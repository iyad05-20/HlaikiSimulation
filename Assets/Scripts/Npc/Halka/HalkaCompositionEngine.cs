using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace JemaaGame.NPC
{
    /// <summary>
    /// Pure scoring + narration logic for Halka composition.
    /// Responsibility: compute coherence, depth, audience; generate LLM narration.
    /// No UI coupling. No singleton pattern (designed to be called by HalkaOrchestrator).
    /// </summary>
    public class HalkaCompositionEngine : MonoBehaviour
    {
        [Serializable]
        public class FragmentEntry
        {
            public string npcId;
            public string npcName;
            public string fragmentId;
            public string title;
            public string content;
            public int tier;
            public bool isSensitive;
        }

        [Serializable]
        public class CompositionResult
        {
            public List<string> selectedFragmentIds = new List<string>();
            public int coherenceScore;
            public int depthScore;
            public int audienceSize;
            public int totalScore;
            public string reactionLabel;
            public bool exposesSensitiveStory;
            public string narration;
        }

        [Header("Audience tuning")]
        [SerializeField] private int baseAudience = 3;
        [SerializeField] private int reputationAudienceStep = 10;
        [SerializeField] private float moussaAudienceMultiplier = 1.35f;
        [SerializeField] private int youssefAudienceBonus = 3;
        [SerializeField] private int omarAudienceBonus = 5;

        private GroqApiClient apiClient;
        private readonly List<FragmentEntry> _catalog = new List<FragmentEntry>();
        private readonly Dictionary<string, FragmentEntry> _fragmentById = new Dictionary<string, FragmentEntry>();
        private readonly Dictionary<string, int> _pairRules = new Dictionary<string, int>();

        // Audience signals (set externally by HalkaOrchestrator)
        public bool MoussaAllied { get; set; }
        public bool YoussefBavard { get; set; }
        public bool OmarPresent { get; set; }

        private void OnEnable()
        {
            apiClient = GroqApiClient.Instance;
            if (apiClient == null)
                apiClient = GetComponent<GroqApiClient>();

            LoadFragmentCatalog();
        }

        // ─── Setup ──────────────────────────────────────────────────────────────
        private void InitializePairRules()
        {
            _pairRules.Clear();
            _pairRules[MakePairKey("hamid", "lalla_fatima")] = 24;
            _pairRules[MakePairKey("driss", "si_brahim")] = 14;
            _pairRules[MakePairKey("driss", "zahra")] = 8;
            _pairRules[MakePairKey("hamid", "moussa")] = 6;
        }

        private string MakePairKey(string id1, string id2)
        {
            return id1.CompareTo(id2) < 0 ? $"{id1}|{id2}" : $"{id2}|{id1}";
        }

        // ─── Catalog Loading ────────────────────────────────────────────────────
        private void LoadFragmentCatalog()
        {
            _catalog.Clear();
            _fragmentById.Clear();
            InitializePairRules();

            string personasDir = Path.Combine(Application.streamingAssetsPath, "personas");
            if (!Directory.Exists(personasDir))
                return;

            string[] files = Directory.GetFiles(personasDir, "*.json");
            foreach (string file in files)
            {
                string raw = File.ReadAllText(file);
                raw = raw.Replace("\"base\":", "\"base_text\":");

                NpcPersonaData persona = JsonUtility.FromJson<NpcPersonaData>(raw);
                if (persona == null || persona.story_fragment == null || string.IsNullOrEmpty(persona.story_fragment.id))
                    continue;

                FragmentEntry fragment = new FragmentEntry
                {
                    npcId = persona.id,
                    npcName = persona.name,
                    fragmentId = persona.story_fragment.id,
                    title = persona.story_fragment.title,
                    content = persona.story_fragment.content,
                    tier = persona.tier,
                    isSensitive = persona.tier >= 3 || persona.id == "zahra" || persona.id == "driss" || persona.id == "si_brahim"
                };

                if (_fragmentById.ContainsKey(fragment.fragmentId))
                    continue;

                _catalog.Add(fragment);
                _fragmentById[fragment.fragmentId] = fragment;
            }
        }

        // ─── Query Available Fragments ──────────────────────────────────────────
        public List<FragmentEntry> GetAvailableFragments()
        {
            List<string> collected = GameManager.GetCollectedFragmentsSnapshot();
            return _catalog.Where(fragment => collected.Contains(fragment.fragmentId)).ToList();
        }

        // ─── Composition & Scoring ──────────────────────────────────────────────
        public CompositionResult ScoreComposition(List<string> selectedFragmentIds)
        {
            CompositionResult result = new CompositionResult
            {
                selectedFragmentIds = new List<string>(selectedFragmentIds)
            };

            List<FragmentEntry> selected = selectedFragmentIds
                .Select(id => ResolveFragment(id))
                .Where(fragment => fragment != null)
                .ToList();

            int coherence = 0;
            int depth = 0;
            bool exposesSensitive = false;

            for (int i = 0; i < selected.Count; i++)
            {
                depth += GetTierWeight(selected[i].tier);
                if (selected[i].isSensitive)
                    exposesSensitive = true;

                if (i == 0)
                    continue;

                coherence += ScorePair(selected[i - 1], selected[i]);
            }

            if (selected.Count > 1 && coherence == 0)
                coherence -= (selected.Count - 1) * 4;

            int audience = ComputeAudienceSize();
            if (selected.Count == 0)
                coherence -= 10;

            result.coherenceScore = coherence;
            result.depthScore = depth;
            result.audienceSize = audience;
            result.totalScore = coherence + (depth * 4) + audience;
            result.exposesSensitiveStory = exposesSensitive && audience >= 10;
            result.reactionLabel = GetReactionLabel(result.totalScore, audience, coherence, depth);
            return result;
        }

        private int ComputeAudienceSize()
        {
            int audience = baseAudience;

            int globalRep = GameManager.GlobalReputation;
            audience += globalRep / reputationAudienceStep;

            if (MoussaAllied)
                audience = Mathf.RoundToInt(audience * moussaAudienceMultiplier);
            if (YoussefBavard)
                audience += youssefAudienceBonus;
            if (OmarPresent)
                audience += omarAudienceBonus;

            return Mathf.Max(1, audience);
        }

        private int ScorePair(FragmentEntry prev, FragmentEntry curr)
        {
            string key = MakePairKey(prev.npcId, curr.npcId);
            if (_pairRules.TryGetValue(key, out int bonus))
                return bonus;

            if (prev.tier == curr.tier)
                return 2;

            return 0;
        }

        private int GetTierWeight(int tier)
        {
            return tier switch
            {
                1 => 2,
                2 => 5,
                3 => 10,
                _ => 1
            };
        }

        private FragmentEntry ResolveFragment(string fragmentId)
        {
            if (_fragmentById.TryGetValue(fragmentId, out FragmentEntry fragment))
                return fragment;
            return null;
        }

        private string GetReactionLabel(int totalScore, int audience, int coherence, int depth)
        {
            if (totalScore >= 100)
                return "Ovation monumentale — le cercle entier est transporté.";
            else if (totalScore >= 60)
                return "Réaction forte — des applaudissements sincères.";
            else if (totalScore >= 30)
                return "Appréciation modérée — quelques sourires.";
            else
                return "Réaction mitigée — l'audience reste prudente.";
        }

        // ─── LLM Narration ──────────────────────────────────────────────────────
        public IEnumerator GenerateNarration(CompositionResult composition, System.Action<string> onComplete, System.Action<string> onError)
        {
            string prompt = BuildNarrationPrompt(composition);

            if (apiClient == null)
            {
                apiClient = GroqApiClient.Instance;
            }

            if (apiClient == null)
            {
                string fallback = BuildFallbackNarration(composition);
                onComplete?.Invoke(fallback);
                yield break;
            }

            string narration = null;
            bool done = false;

            List<GroqMessage> messages = new List<GroqMessage>
            {
                new GroqMessage
                {
                    role = "system",
                    content = "Tu es un narrateur de performance orale. Tu rédiges une Halka vivante, publique et cohérente. Ne mentionne jamais de score, de mécanique, ni d'audience numérique."
                },
                new GroqMessage
                {
                    role = "user",
                    content = prompt
                }
            };

            StartCoroutine(apiClient.SendChatRequest(messages,
                resultText =>
                {
                    narration = resultText != null ? resultText.Trim() : "";
                    Debug.Log($"[HalkaCompositionEngine] LLM Narration Result:\n{narration}");
                    done = true;
                },
                error =>
                {
                    Debug.LogWarning($"[HalkaCompositionEngine] Narration LLM failed: {error}");
                    done = true;
                }
            ));

            yield return new WaitUntil(() => done);

            if (string.IsNullOrEmpty(narration))
            {
                narration = BuildFallbackNarration(composition);
                onError?.Invoke("Narration LLM failed, using fallback");
            }

            onComplete?.Invoke(narration);
        }

        private string BuildNarrationPrompt(CompositionResult composition)
        {
            List<FragmentEntry> selected = composition.selectedFragmentIds
                .Select(id => ResolveFragment(id))
                .Where(f => f != null)
                .ToList();

            string fragmentsText = string.Join("\n\n", selected.Select(f =>
                $"[{f.npcName}] — {f.title}\n{f.content}"));

            bool exposesSensitive = composition.exposesSensitiveStory;
            string sensitivityNote = exposesSensitive
                ? "\n⚠️ Tu dois être conscient que l'audience remarquera la révélation d'une histoire intime. Ajoute de la tension narrative autour de ce moment — des réactions murmurées, de la gêne, ou une réflexion sur la confiance brisée."
                : "";

            // NOUVEAU Phase 2: Injecter le contexte social du joueur
            string socialContext = BuildPlayerSocialContext();

            return $@"Les fragments suivants ont été rassemblés pour une performance Halka (cercle narratif traditionnel) :

{fragmentsText}

CONTEXTE SOCIAL DU JOUEUR:
{socialContext}

Audience estimée : {composition.audienceSize} personnes.
Cohérence globale : {composition.coherenceScore} points.

Compose une narration vivante, ORALE et PUBLIQUE de ces fragments, comme si le joueur les racontait maintenant devant le cercle. La narration doit :
1. Être fluide et naturelle (comme parlée, non écrite)
2. Créer une continuité entre les fragments
3. Capturer l'atmosphère des histoires
4. Rester authentique aux voix et cultures représentées
5. Refléter la position sociale du joueur dans le réseau social (ses alliances, ses tensions, ce qu'il a appris)
6. Si le joueur connaît certains des NPCs dans l'audience, leur réaction silencieuse à ce qui est dit peut augmenter la tension{sensitivityNote}

Commence directement par la narration, sans introduction. Utilise des transitions orales (« Et puis... », « C'est là que... », « Voilà ce que... »).";
        }

        // Phase 2: Build player social context from game state
        private string BuildPlayerSocialContext()
        {
            List<string> npcNames = new List<string>();
            
            // Get NPC names from collected fragments
            List<string> fragmentIds = GameManager.GetCollectedFragmentsSnapshot();
            foreach (string fragId in fragmentIds)
            {
                var frag = ResolveFragment(fragId);
                if (frag != null && !npcNames.Contains(frag.npcName))
                {
                    npcNames.Add(frag.npcName);
                }
            }

            string npcMention = npcNames.Count > 0
                ? $"Ces NPCs sont dans ton réseau social: {string.Join(", ", npcNames)}"
                : "Tu ne raconte l'histoire de personnes que tu ne connaissais pas intimement.";

            string audienceNpcNote = fragmentIds.Count > 0
                ? "\nCertains des NPCs dont tu raconte l'histoire peuvent être dans l'audience et reconnaître leur propre histoire."
                : "";

            return $@"Réputation globale du joueur: {GameManager.GlobalReputation}/100
{npcMention}{audienceNpcNote}

Personnalise la narration pour montrer:
- Le point de vue du joueur sur ces histoires (pas juste les révéler, mais montrer qu'il les a comprises)
- Les tensions sociales s'il y en a (raconter une histoire intime peut changer une relation)
- La croissance sociale du joueur (il a collecté ces fragments, donc il a su créer de la confiance)";
        }

        private string BuildFallbackNarration(CompositionResult composition)
        {
            if (composition.selectedFragmentIds.Count == 0)
                return "Le silence retombe. Le cercle attend une histoire qui ne vient pas... mais la nuit est jeune.";

            List<FragmentEntry> selected = composition.selectedFragmentIds
                .Select(id => ResolveFragment(id))
                .Where(f => f != null)
                .ToList();

            if (selected.Count == 0)
                return "Les histoires se dispersent dans l'air du soir. Le cercle reste silencieux.";

            string names = string.Join(", ", selected.Select(f => f.npcName));
            string baseText = $"Le joueur raconte des histoires entrecroisées : celles de {names}.";

            return composition.coherenceScore > 10
                ? baseText + " L'audience suit, captivée par les connexions."
                : baseText + " L'audience reste attentive, bien que les histoires ne se lient pas toujours.";
        }

        public void Reset()
        {
            MoussaAllied = false;
            YoussefBavard = false;
            OmarPresent = false;
        }
    }
}
