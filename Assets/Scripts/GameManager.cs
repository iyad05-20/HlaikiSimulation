using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static bool IsNewGame = false;
    private static int _globalReputation = 0;
    private static bool _isLoaded = false;

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
            _globalReputation = 0;
            _isLoaded = true;
            SaveGlobalData();
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

    private static void SaveGlobalData()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.SavePlayerData(new PlayerData { globalReputation = _globalReputation });
        }
    }
}
