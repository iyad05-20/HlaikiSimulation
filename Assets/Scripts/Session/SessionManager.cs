using System;
using System.IO;
using UnityEngine;

// ─── Session Data ─────────────────────────────────────────────────────────────
[Serializable]
public class NPCSessionData
{
    public string npc_id;
    public int favorability;
    public string current_emotion;
    public string interaction_summary;
    public bool fragment_revealed;
    public string last_updated;
}

[Serializable]
public class PlayerData
{
    public int globalReputation;
    // Tu pourras ajouter ici : argent, inventaire, position, etc.
}

// ─── SessionManager ───────────────────────────────────────────────────────────
public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    private string SaveDir => Path.Combine(Application.persistentDataPath, "sessions");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Directory.CreateDirectory(SaveDir);
    }

    // ─── NPC Save/Load ────────────────────────────────────────────────────────
    public void SaveSession(NPCSessionData data)
    {
        data.last_updated = DateTime.UtcNow.ToString("o");
        string json = JsonUtility.ToJson(data, true);
        string path = Path.Combine(SaveDir, $"{data.npc_id}_session.json");
        File.WriteAllText(path, json);
        Debug.Log($"[SessionManager] Session saved for {data.npc_id} → {path}");
    }

    public NPCSessionData LoadSession(string npcId)
    {
        string path = Path.Combine(SaveDir, $"{npcId}_session.json");
        if (!File.Exists(path))
        {
            Debug.Log($"[SessionManager] No saved session for {npcId}.");
            return null;
        }

        string json = File.ReadAllText(path);
        NPCSessionData data = JsonUtility.FromJson<NPCSessionData>(json);
        Debug.Log($"[SessionManager] Session loaded for {npcId}. Fav: {data.favorability}");
        return data;
    }

    // ─── Player Save/Load ─────────────────────────────────────────────────────
    public void SavePlayerData(PlayerData data)
    {
        string json = JsonUtility.ToJson(data, true);
        string path = Path.Combine(SaveDir, "player_save.json");
        File.WriteAllText(path, json);
        Debug.Log("[SessionManager] Player data saved.");
    }

    public PlayerData LoadPlayerData()
    {
        string path = Path.Combine(SaveDir, "player_save.json");
        if (!File.Exists(path))
        {
            Debug.Log("[SessionManager] No player save found. Returning default.");
            return new PlayerData { globalReputation = 0 };
        }

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<PlayerData>(json);
    }

    // ─── Delete ───────────────────────────────────────────────────────────────
    public void DeleteAllSessions()
    {
        if (Directory.Exists(SaveDir))
        {
            // On supprime TOUS les fichiers .json (NPC + Joueur)
            string[] files = Directory.GetFiles(SaveDir, "*.json");
            foreach (string file in files)
            {
                File.Delete(file);
            }
            Debug.Log("[SessionManager] All save data deleted (NPCs and Player).");
        }
    }
}
