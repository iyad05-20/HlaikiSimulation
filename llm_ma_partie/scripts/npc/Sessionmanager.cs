using System;
using System.IO;
using UnityEngine;

namespace JemaaGame.NPC
{
    /// <summary>
    /// Reads and writes NPC session files.
    /// Format is identical to Python npc_sessions/{id}_session.json —
    /// the same files can be read by both the Python test scripts and Unity.
    /// </summary>
    public class SessionManager : MonoBehaviour
    {
        public static SessionManager Instance { get; private set; }

        // ── Config ─────────────────────────────────────────────────────────

        // In Unity: StreamingAssets/npc_sessions/
        // In Python tests: game/npc_sessions/
        // Both point to the same folder when running from project root.
        [SerializeField]
        private string sessionFolder = "npc_sessions";

        private string SessionDir => Path.Combine(Application.streamingAssetsPath, sessionFolder);

        // ── Unity lifecycle ────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureSessionDir();
        }

        private void EnsureSessionDir()
        {
            if (!Directory.Exists(SessionDir))
                Directory.CreateDirectory(SessionDir);
        }

        // ── Load ───────────────────────────────────────────────────────────

        /// <summary>
        /// Load previous session for an NPC.
        /// Returns null if no session exists (first encounter this cycle).
        /// </summary>
        public NPCSessionData LoadSession(string npcId)
        {
            string path = SessionPath(npcId);
            if (!File.Exists(path))
            {
                Debug.Log($"[SessionManager] No session found for {npcId} — starting fresh.");
                return null;
            }

            try
            {
                string json    = File.ReadAllText(path);
                var    session = JsonUtility.FromJson<NPCSessionData>(json);
                Debug.Log($"[SessionManager] Loaded session for {npcId}: fav={session.favorability} rep={session.reputation}");
                return session;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SessionManager] Failed to load session for {npcId}: {e.Message}");
                return null;
            }
        }

        // ── Save ───────────────────────────────────────────────────────────

        /// <summary>
        /// Save session at end of conversation.
        /// Called by DialogueManager after generating summary.
        /// </summary>
        public void SaveSession(NPCSessionData session)
        {
            try
            {
                string path = SessionPath(session.npc_id);
                string json = JsonUtility.ToJson(session, prettyPrint: true);
                File.WriteAllText(path, json);
                Debug.Log($"[SessionManager] Session saved for {session.npc_id}.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SessionManager] Failed to save session for {session.npc_id}: {e.Message}");
            }
        }

        // ── Delete ─────────────────────────────────────────────────────────

        /// <summary>
        /// Delete session — used on cycle reset.
        /// </summary>
        public void DeleteSession(string npcId)
        {
            string path = SessionPath(npcId);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[SessionManager] Session deleted for {npcId}.");
            }
        }

        public void DeleteAllSessions()
        {
            foreach (string f in Directory.GetFiles(SessionDir, "*_session.json"))
                File.Delete(f);
            Debug.Log("[SessionManager] All sessions deleted.");
        }

        // ── Utility ───────────────────────────────────────────────────────

        private string SessionPath(string npcId)
            => Path.Combine(SessionDir, $"{npcId}_session.json");

        public bool HasSession(string npcId)
            => File.Exists(SessionPath(npcId));
    }
}