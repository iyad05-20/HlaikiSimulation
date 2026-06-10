using UnityEngine;
using JemaaGame.NPC;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static bool IsNewGame = false;
    private static int _globalReputation = 0;
    private static bool _isLoaded = false;
    private static readonly List<string> _fragments = new List<string>();
    private static readonly Dictionary<string, int> _cycleVariants = new Dictionary<string, int>();
    
    // NOUVEAU: Contexte des fragments (pour Contextual Anchoring)
    [System.Serializable]
    public class FragmentContext
    {
        public List<string> presentations = new List<string>();  // Peut avoir plusieurs présentations
    }
    private static readonly Dictionary<string, FragmentContext> _fragmentContexts = 
        new Dictionary<string, FragmentContext>();
    
    [SerializeField] private int halkaMinFragments = 3;

    // Propriété intelligente : si on demande la réputation et qu'elle n'est pas chargée, on la charge.
    public static int GlobalReputation
    {
        get
        {
            if (!_isLoaded && !IsNewGame)
            {
                LoadGlobalData();
            }
            return _globalReputation;
        }
        set
        {
            _globalReputation = value;
            _isLoaded = true;
        }
    }

    private void Start()
    {
        if (IsNewGame)
        {
            ResetCycle();
            Debug.Log("[GameManager] New Game started: Reputation reset to 0");
        }
        else
        {
            LoadGlobalData();
        }
    }

    private static void LoadGlobalData()
    {
        if (SessionManager.Instance != null)
        {
            PlayerData data = SessionManager.Instance.LoadPlayerData();
            _globalReputation = data.globalReputation;
            _isLoaded = true;
            Debug.Log($"[GameManager] Global Data Loaded: Reputation = {_globalReputation}");
        }
        else
        {
            // Si le SessionManager n'est pas encore là, on ne marque pas comme chargé
            // pour retenter au prochain accès.
            Debug.LogWarning("[GameManager] SessionManager not ready yet in LoadGlobalData...");
        }
    }

    public static void AddReputation(int delta)
    {
        // On utilise la propriété pour s'assurer que c'est chargé avant de modifier
        GlobalReputation += delta; 
        Debug.Log($"[GameManager] Global Reputation updated: {GlobalReputation} (delta: {delta})");
        SaveGlobalData();
    }

    public static void AddFragment(string fragmentId)
    {
        if (string.IsNullOrEmpty(fragmentId) || _fragments.Contains(fragmentId))
            return;

        _fragments.Add(fragmentId);
        Debug.Log($"[GameManager] Fragment collected: {fragmentId} ({_fragments.Count} total)");
    }

    // NOUVEAU: Ajouter fragment avec contexte (pour Contextual Anchoring)
    public static void AddFragmentWithContext(string fragmentId, string contextualizedPresentation)
    {
        if (!_fragments.Contains(fragmentId))
        {
            _fragments.Add(fragmentId);
            Debug.Log($"[GameManager] Fragment collected with context: {fragmentId}");
        }
        
        if (!_fragmentContexts.ContainsKey(fragmentId))
        {
            _fragmentContexts[fragmentId] = new FragmentContext();
        }
        
        _fragmentContexts[fragmentId].presentations.Add(contextualizedPresentation);
        Debug.Log($"[GameManager] Fragment context saved: {fragmentId}");
    }

    // NOUVEAU: Récupérer la présentation contextualisée d'un fragment
    public static string GetFragmentContextualPresentation(string fragmentId)
    {
        if (_fragmentContexts.TryGetValue(fragmentId, out var context))
        {
            if (context.presentations.Count > 0)
            {
                // Retourner la dernière présentation (la plus récente)
                return context.presentations[context.presentations.Count - 1];
            }
        }
        
        return "";  // Fallback vide si pas de contexte
    }

    public static bool HasFragment(string fragmentId)
    {
        return _fragments.Contains(fragmentId);
    }

    public static int FragmentCount
    {
        get { return _fragments.Count; }
    }

    public static List<string> GetCollectedFragmentsSnapshot()
    {
        return new List<string>(_fragments);
    }

    public static void SetCycleVariant(string npcId, int variantId)
    {
        _cycleVariants[npcId] = variantId;
    }

    public static int GetCycleVariant(string npcId)
    {
        int variantId;
        if (_cycleVariants.TryGetValue(npcId, out variantId))
            return variantId;
        return -1;
    }

    public static void ResetCycle()
    {
        if (SessionManager.Instance != null)
            SessionManager.Instance.DeleteAllSessions();
        if (EventTracker.Instance != null)
            EventTracker.Instance.ResetCycle();
        if (HalkaOrchestrator.Instance != null)
            HalkaOrchestrator.Instance.ResetCycle();

        _globalReputation = 0;
        _isLoaded = true;
        _fragments.Clear();
        _cycleVariants.Clear();
        SaveGlobalData();
        Debug.Log("[GameManager] Cycle reset.");
    }

    public static string DebugSummary()
    {
        return $"GlobalRep={GlobalReputation} | Fragments=[{string.Join(", ", _fragments)}] | Variants={_cycleVariants.Count}";
    }

    public bool IsHalkaUnlocked()
    {
        if (_fragments.Count < halkaMinFragments)
        {
            Debug.Log($"[GameManager] Halka locked: {_fragments.Count}/{halkaMinFragments} fragments");
            return false;
        }

        return true;
    }

    private static void SaveGlobalData()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.SavePlayerData(new PlayerData { globalReputation = _globalReputation });
        }
    }
}
