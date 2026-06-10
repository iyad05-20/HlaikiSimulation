using System;
using System.Collections.Generic;
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
    
    // NOUVEAU Phase 3: Contexte social du fragment
    public string fragment_contextual_presentation = "";  // Présentation LLM personnalisée
    public int favorability_at_reveal = 0;  // Favorabilité au moment de la révélation
    public int player_global_reputation_at_reveal = 0;  // Réputation globale à ce moment
}

[Serializable]
public class PlayerData
{
    public int globalReputation;
    // Tu pourras ajouter ici : argent, inventaire, position, etc.
}

[Serializable]
public class CollectedStory
{
    public string id;
    public string title;
    public string content;
    public string dateUnlocked;
}

[Serializable]
public class PlayerStoriesData
{
    public List<CollectedStory> stories = new List<CollectedStory>();
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

    // ─── Player Stories Save/Load ──────────────────────────────────────────────
    public void SaveStories(PlayerStoriesData data)
    {
        string json = JsonUtility.ToJson(data, true);
        string path = Path.Combine(SaveDir, "player_stories.json");
        File.WriteAllText(path, json);
        Debug.Log("[SessionManager] Player stories saved.");
    }

    public PlayerStoriesData LoadStories()
    {
        string path = Path.Combine(SaveDir, "player_stories.json");
        if (!File.Exists(path))
        {
            Debug.Log("[SessionManager] No player stories found. Returning default.");
            return new PlayerStoriesData();
        }

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<PlayerStoriesData>(json);
    }

    public void CaptureStory(string id, string title, string content)
    {
        PlayerStoriesData data = LoadStories();
        
        // Si l'histoire n'existe pas encore
        if (!data.stories.Exists(s => s.id == id))
        {
            data.stories.Add(new CollectedStory {
                id = id,
                title = title,
                content = content,
                dateUnlocked = DateTime.UtcNow.ToString("o")
            });
            
            SaveStories(data);
            
            // Notification automatique si le système UI est présent
            if (JemaaGame.UI.NotificationManager.Instance != null)
            {
                JemaaGame.UI.NotificationManager.Instance.Show(
                    "FRAGMENT OBTENU", 
                    title, 
                    JemaaGame.UI.NotificationType.Fragment
                );
            }
        }
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

    public void DeleteSession(string npcId)
    {
        string path = Path.Combine(SaveDir, $"{npcId}_session.json");
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"[SessionManager] Session deleted for {npcId}.");
        }
    }

    public bool HasSession(string npcId)
    {
        string path = Path.Combine(SaveDir, $"{npcId}_session.json");
        return File.Exists(path);
    }

    // NOUVEAU Phase 1: Récupérer tous les sessions pour contextual anchoring
    public List<NPCSessionData> GetAllSessions()
    {
        List<NPCSessionData> sessions = new List<NPCSessionData>();
        
        if (!Directory.Exists(SaveDir))
            return sessions;
        
        string[] files = Directory.GetFiles(SaveDir, "*_session.json");
        foreach (string file in files)
        {
            if (file.EndsWith("player_save.json"))
                continue;  // Skip player data file
            
            try
            {
                string json = File.ReadAllText(file);
                NPCSessionData data = JsonUtility.FromJson<NPCSessionData>(json);
                if (data != null)
                    sessions.Add(data);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SessionManager] Failed to load session from {file}: {ex.Message}");
            }
        }
        
        return sessions;
    }
}
