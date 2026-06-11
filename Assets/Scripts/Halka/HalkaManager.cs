using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[Serializable]
public class HalkaFragmentEntry
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
public class HalkaCompositionResult
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

public class HalkaManager : MonoBehaviour
{
    public static HalkaManager Instance { get; private set; }

    [Header("Audience tuning")]
    [SerializeField] private int baseAudience = 3;
    [SerializeField] private int reputationAudienceStep = 10;
    [SerializeField] private float moussaAudienceMultiplier = 1.35f;
    [SerializeField] private int youssefAudienceBonus = 3;
    [SerializeField] private int omarAudienceBonus = 5;

    [Header("Optional audience signals")]
    [SerializeField] private bool moussaAllied;
    [SerializeField] private bool youssefBavard;
    [SerializeField] private bool omarPresent;

    private GroqApiClient apiClient;
    private readonly List<HalkaFragmentEntry> _catalog = new List<HalkaFragmentEntry>();
    private readonly Dictionary<string, HalkaFragmentEntry> _fragmentById = new Dictionary<string, HalkaFragmentEntry>();
    private readonly Dictionary<string, int> _pairRules = new Dictionary<string, int>();

    public event Action<HalkaCompositionResult> OnHalkaCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _pairRules[MakePairKey("hamid", "lalla_fatima")] = 24;
        _pairRules[MakePairKey("driss", "si_brahim")] = 14;
        _pairRules[MakePairKey("driss", "zahra")] = 8;
        _pairRules[MakePairKey("hamid", "moussa")] = 6;
    }

    private void OnEnable()
    {
        apiClient = GroqApiClient.Instance;
        if (apiClient == null)
            apiClient = GetComponent<GroqApiClient>();

        LoadFragmentCatalog();
    }

    public void SetMoussaAllied(bool value) { moussaAllied = value; }
    public void SetYoussefBavard(bool value) { youssefBavard = value; }
    public void SetOmarPresent(bool value) { omarPresent = value; }

    public void ResetCycle()
    {
        moussaAllied = false;
        youssefBavard = false;
        omarPresent = false;
    }

    public List<HalkaFragmentEntry> GetAvailableFragments()
    {
        List<string> collected = GameManager.GetCollectedFragmentsSnapshot();
        return _catalog.Where(fragment => collected.Contains(fragment.fragmentId)).ToList();
    }

    public void StartHalka(List<string> selectedFragmentIds)
    {
        StartCoroutine(RunHalka(selectedFragmentIds, null));
    }

    public void StartHalka(List<string> selectedFragmentIds, Action<HalkaCompositionResult> onComplete)
    {
        StartCoroutine(RunHalka(selectedFragmentIds, onComplete));
    }

    private IEnumerator RunHalka(List<string> selectedFragmentIds, Action<HalkaCompositionResult> onComplete)
    {
        if (_catalog.Count == 0)
            LoadFragmentCatalog();

        List<string> available = GameManager.GetCollectedFragmentsSnapshot();
        List<string> orderedSelection = selectedFragmentIds != null && selectedFragmentIds.Count > 0
            ? new List<string>(selectedFragmentIds)
            : new List<string>(available);

        HalkaCompositionResult result = ScoreComposition(orderedSelection);
        string prompt = BuildNarrationPrompt(result);

        if (apiClient == null)
        {
            result.narration = BuildFallbackNarration(result);
            OnHalkaCompleted?.Invoke(result);
            if (onComplete != null) onComplete(result);
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
                done = true;
            },
            error =>
            {
                Debug.LogWarning($"[HalkaManager] Narration LLM failed: {error}");
                narration = BuildFallbackNarration(result);
                done = true;
            }
        ));

        yield return new WaitUntil(() => done);

        result.narration = narration;
        OnHalkaCompleted?.Invoke(result);
        if (onComplete != null) onComplete(result);
    }

    private void LoadFragmentCatalog()
    {
        _catalog.Clear();
        _fragmentById.Clear();

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

            HalkaFragmentEntry fragment = new HalkaFragmentEntry
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

    private HalkaCompositionResult ScoreComposition(List<string> selectedFragmentIds)
    {
        HalkaCompositionResult result = new HalkaCompositionResult
        {
            selectedFragmentIds = new List<string>(selectedFragmentIds)
        };

        List<HalkaFragmentEntry> selected = selectedFragmentIds
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
        audience += Mathf.Clamp(GameManager.GlobalReputation / reputationAudienceStep, -2, 10);

        if (ResolveMoussaAllied())
            audience = Mathf.CeilToInt(audience * moussaAudienceMultiplier);

        if (ResolveYoussefBavard())
            audience += youssefAudienceBonus;

        if (ResolveOmarPresent())
            audience += omarAudienceBonus;

        return Mathf.Max(1, audience);
    }

    private bool ResolveMoussaAllied()
    {
        return moussaAllied || ResolveNpcCondition("moussa", "allied") || ResolveNpcCondition("moussa", "vouch_active");
    }

    private bool ResolveYoussefBavard()
    {
        return youssefBavard || ResolveNpcCondition("youssef", "spread_word") || ResolveNpcCondition("youssef", "bavard");
    }

    private bool ResolveOmarPresent()
    {
        return omarPresent || ResolveNpcCondition("omar", "present") || ResolveNpcCondition("omar", "photo_taken");
    }

    private bool ResolveNpcCondition(string npcId, string conditionKey)
    {
        if (NPCManager.Instance == null)
            return false;

        return NPCManager.Instance.GetCondition(npcId, conditionKey);
    }

    private string BuildNarrationPrompt(HalkaCompositionResult result)
    {
        List<HalkaFragmentEntry> selected = result.selectedFragmentIds
            .Select(id => ResolveFragment(id))
            .Where(fragment => fragment != null)
            .ToList();

        string fragmentsText = selected.Count == 0
            ? "- Aucun fragment disponible. Le narrateur doit improviser une Halka de manque et de silence."
            : string.Join("\n", selected.Select((fragment, index) =>
                $"{index + 1}. [{fragment.npcName} | Tier {fragment.tier}] {fragment.title}\n   {fragment.content}"));

        return
            "Tu dois écrire la narration publique d'une Halka finale.\n" +
            "Le texte doit être vivant, oral, cohérent, et centré sur la réaction de la foule.\n" +
            "Ne montre jamais les nombres, le score, ni la logique interne.\n\n" +
            $"RÉACTION INTERNE: {result.reactionLabel}\n" +
            $"COHÉRENCE: {result.coherenceScore}\n" +
            $"PROFONDEUR: {result.depthScore}\n" +
            $"AUDIENCE: {result.audienceSize}\n" +
            $"EXPOSITION D'UN SECRET: {(result.exposesSensitiveStory ? "oui" : "non")}\n\n" +
            "FRAGMENTS CHOISIS DANS CET ORDRE:\n" +
            fragmentsText + "\n\n" +
            "Contraintes:\n" +
            "- français naturel\n" +
            "- mentionne les spectateurs, les silences, les réactions corporelles, et les personnages présents si pertinent\n" +
            "- si un fragment sensible est exposé, la conséquence doit être ressentie dans la scène\n" +
            "- la narration doit donner l'impression d'une performance orale, pas d'un résumé";
    }

    private string BuildFallbackNarration(HalkaCompositionResult result)
    {
        if (result.selectedFragmentIds.Count == 0)
            return "La Halka s'ouvre dans un silence un peu lourd. Sans fragments, le joueur improvise, et la foule sent l'absence d'histoire autant que la présence du vide.";

        if (result.totalScore >= 80)
            return "La Halka prend. Les fragments s'assemblent comme s'ils avaient toujours appartenu à la même mémoire, et la foule répond d'un seul souffle.";

        if (result.totalScore >= 45)
            return "La Halka avance avec des hauts et des bas, mais la salle reste accrochée. Certaines transitions frappent juste, d'autres laissent un petit vide.";

        return "La Halka hésite. La foule écoute par moments, mais l'histoire se disperse et l'effet collectif reste fragile.";
    }

    private HalkaFragmentEntry ResolveFragment(string fragmentId)
    {
        if (string.IsNullOrEmpty(fragmentId))
            return null;

        HalkaFragmentEntry fragment;
        if (_fragmentById.TryGetValue(fragmentId, out fragment))
            return fragment;

        return _catalog.FirstOrDefault(entry => entry.fragmentId == fragmentId);
    }

    private int ScorePair(HalkaFragmentEntry left, HalkaFragmentEntry right)
    {
        if (left == null || right == null)
            return 0;

        int score = -3;

        if (left.npcId == right.npcId)
            return -8;

        string pairKey = MakePairKey(left.npcId, right.npcId);
        int pairBonus;
        if (_pairRules.TryGetValue(pairKey, out pairBonus))
            return pairBonus;

        if (left.npcId == "driss" && right.npcId == "si_brahim")
            return 16;

        if ((left.npcId == "hamid" && right.npcId == "lalla_fatima") || (left.npcId == "lalla_fatima" && right.npcId == "hamid"))
            return 22;

        if (left.tier == 3 && right.tier == 3)
            score += 4;

        if (Math.Abs(left.tier - right.tier) <= 1)
            score += 2;

        return score;
    }

    private int GetTierWeight(int tier)
    {
        if (tier >= 3) return 4;
        if (tier == 2) return 2;
        return 1;
    }

    private string GetReactionLabel(int totalScore, int audience, int coherence, int depth)
    {
        if (audience >= 12 && totalScore >= 70) return "foule emportée";
        if (totalScore >= 60) return "performance marquante";
        if (totalScore >= 35) return "récit solide";
        if (totalScore >= 15) return "récit fragile";
        return "halka dispersée";
    }

    private string MakePairKey(string first, string second)
    {
        if (string.CompareOrdinal(first, second) <= 0)
            return first + "|" + second;

        return second + "|" + first;
    }
}
