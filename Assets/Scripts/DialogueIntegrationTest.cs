/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using JemaaGame.NPC;

namespace JemaaGame.Tests
{
    /// <summary>
    /// C# Integration Test — Dialogue Core Flow
    /// Tests the critical path: User Input → NPC State Update → Emotion/Favorability Change
    /// 
    /// Mirrors Python test flow but in C#, verifying:
    /// 1. DialogueManager receives user input
    /// 2. NPCManager/NPCState updates correctly
    /// 3. Emotion and favorability change as expected
    /// 4. Session persists
    ///
    /// Run via: Unity → Window → General → Test Runner → Run Tests
    /// </summary>
    [TestFixture]
    public class DialogueIntegrationTest
    {
        private NPCManager _npcManager;
        private GameState _gameState;
        private DialogueManager _dialogueManager;
        private NPCState _hamidState;
        
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Ensure singletons exist
            if (NPCManager.Instance == null)
            {
                var npcMgrObj = new GameObject("NPCManager");
                npcMgrObj.AddComponent<NPCManager>();
            }
            
            if (GameState.Instance == null)
            {
                var gameStateObj = new GameObject("GameState");
                gameStateObj.AddComponent<GameState>();
            }
            
            if (DialogueManager.Instance == null)
            {
                var dlgMgrObj = new GameObject("DialogueManager");
                dlgMgrObj.AddComponent<DialogueManager>();
            }
            
            if (SessionManager.Instance == null)
            {
                var sessionMgrObj = new GameObject("SessionManager");
                sessionMgrObj.AddComponent<SessionManager>();
            }
        }
        
        [SetUp]
        public void Setup()
        {
            _npcManager = NPCManager.Instance;
            _gameState = GameState.Instance;
            _dialogueManager = DialogueManager.Instance;
            
            Assert.IsNotNull(_npcManager, "NPCManager singleton not initialized");
            Assert.IsNotNull(_gameState, "GameState singleton not initialized");
            Assert.IsNotNull(_dialogueManager, "DialogueManager singleton not initialized");
            
            // Initialize cycle with fresh state
            _npcManager.InitializeCycle();
            _hamidState = new NPCState("hamid", null);
        }
        
        // ── TEST 1: Initial State ────────────────────────────────────────
        
        [Test]
        public void Test_InitialState_DefaultEmotion()
        {
            // ARRANGE & ACT
            var state = new NPCState("hamid", null);
            
            // ASSERT
            Assert.AreEqual(state.Emotion, EmotionState.Open, 
                "Default emotion should be Open");
            Assert.AreEqual(state.Favorability, 0, 
                "Initial favorability should be 0");
            Assert.AreEqual(state.Reputation, 0, 
                "Initial reputation should be 0");
        }
        
        // ── TEST 2: Favorability Delta Application ───────────────────────
        
        [Test]
        public void Test_ApplyScoring_FavorabilityIncreases()
        {
            // ARRANGE
            int initialFav = _hamidState.Favorability;
            var scoring = new ScoringResult
            {
                favorability_delta = 10,
                emotion_pressure = "positive",
                curiosity_triggered = false,
                emotion_shift = null,
                reason = "positive engagement"
            };
            
            // ACT
            _hamidState.ApplyScoring(scoring, reputationDelta: 0);
            
            // ASSERT
            Assert.AreEqual(_hamidState.Favorability, initialFav + 10,
                "Favorability should increase by delta");
            Assert.IsTrue(_hamidState.Favorability <= 100,
                "Favorability should be clamped at 100");
        }
        
        // ── TEST 3: Emotion Transition (Rules-based) ────────────────────
        
        [Test]
        public void Test_EmotionTransition_ClosedToOccupied()
        {
            // ARRANGE - Start closed (very low favorability)
            var closedState = new NPCState("hamid", new NPCSessionData
            {
                npc_id = "hamid",
                favorability = -15,
                reputation = 0,
                emotion = "closed",
                conditions = new NPCConditions(),
                interaction_summary = ""
            });
            
            Assert.AreEqual(closedState.Emotion, EmotionState.Closed,
                "Setup: emotion should be Closed");
            
            // ACT - Apply positive scoring to raise favorability above threshold
            var scoring = new ScoringResult
            {
                favorability_delta = 15,  // -15 + 15 = 0, above -5 threshold
                emotion_pressure = "positive",
                curiosity_triggered = false,
                emotion_shift = null,  // Let rules compute
                reason = "recovery"
            };
            
            closedState.ApplyScoring(scoring, 0);
            
            // ASSERT
            Assert.AreEqual(closedState.Emotion, EmotionState.Occupied,
                "Emotion should transition from Closed to Occupied when fav >= -5");
        }
        
        // ── TEST 4: LLM Emotion Override ─────────────────────────────────
        
        [Test]
        public void Test_ApplyScoring_LLMEmotionOverride()
        {
            // ARRANGE
            var scoring = new ScoringResult
            {
                favorability_delta = 5,
                emotion_pressure = "positive",
                curiosity_triggered = false,
                emotion_shift = "occupied",  // LLM suggests occupied
                reason = "task-focused"
            };
            
            // ACT
            _hamidState.ApplyScoring(scoring, 0);
            
            // ASSERT
            Assert.AreEqual(_hamidState.Emotion, EmotionState.Occupied,
                "LLM emotion_shift should override rules");
        }
        
        // ── TEST 5: Condition Tracking ───────────────────────────────────
        
        [Test]
        public void Test_Conditions_CuriosityTriggered()
        {
            // ARRANGE
            Assert.IsFalse(_hamidState.GetCondition("curiosity_shown"),
                "Curiosity should not be triggered initially");
            
            var scoring = new ScoringResult
            {
                favorability_delta = 0,
                emotion_pressure = "neutral",
                curiosity_triggered = true,  // Trigger curiosity
                emotion_shift = null,
                reason = "curious engagement"
            };
            
            // ACT
            _hamidState.ApplyScoring(scoring, 0);
            
            // ASSERT
            Assert.IsTrue(_hamidState.GetCondition("curiosity_shown"),
                "Curiosity should be marked as triggered");
        }
        
        // ── TEST 6: Fragment Unlock Logic ────────────────────────────────
        
        [Test]
        public void Test_Fragment_Unlock_Conditions()
        {
            // ARRANGE
            var scoring = new ScoringResult
            {
                favorability_delta = 60,  // Enough to unlock fragment
                emotion_pressure = "positive",
                curiosity_triggered = true,  // Curiosity required
                emotion_shift = null,
                reason = "high engagement"
            };
            
            // ACT
            _hamidState.ApplyScoring(scoring, 0);  // fav = 60, curiosity = true
            
            // ASSERT
            Assert.AreEqual(_hamidState.Favorability, 60, "Favorability should be 60");
            Assert.IsTrue(_hamidState.GetCondition("curiosity_shown"), 
                "Curiosity should be true");
            Assert.IsTrue(_hamidState.GetCondition("fragment_revealed"),
                "Fragment should be unlocked (fav >= 60 AND curiosity_shown)");
        }
        
        // ── TEST 7: Fragment Reveal Guard ────────────────────────────────
        
        [Test]
        public void Test_Fragment_RevealGuard_OnlyOnce()
        {
            // ARRANGE - Set up fragment unlocked
            _hamidState.SetCondition("curiosity_shown", true);
            _hamidState.SetCondition("fragment_revealed", true);
            
            // ACT - First call should return true
            bool firstReveal = _hamidState.FragmentShouldReveal();
            
            // ASSERT
            Assert.IsTrue(firstReveal, "First reveal should return true");
            
            // ACT - Second call should return false (guard prevents repeat)
            bool secondReveal = _hamidState.FragmentShouldReveal();
            
            // ASSERT
            Assert.IsFalse(secondReveal, 
                "Fragment should only reveal once (guard prevents repeat)");
        }
        
        // ── TEST 8: Favorability Clamping ────────────────────────────────
        
        [Test]
        public void Test_Favorability_Clamped()
        {
            // ARRANGE
            var hugePositive = new ScoringResult
            {
                favorability_delta = 500,  // Way beyond 100
                emotion_pressure = "positive",
                curiosity_triggered = false,
                emotion_shift = null,
                reason = "extreme"
            };
            
            // ACT
            _hamidState.ApplyScoring(hugePositive, 0);
            
            // ASSERT
            Assert.AreEqual(_hamidState.Favorability, 100,
                "Favorability should be clamped at max 100");
            
            // ARRANGE - Negative clamp
            var hugeNegative = new ScoringResult
            {
                favorability_delta = -500,
                emotion_pressure = "negative",
                curiosity_triggered = false,
                emotion_shift = null,
                reason = "extreme"
            };
            
            // ACT
            _hamidState.ApplyScoring(hugeNegative, 0);
            
            // ASSERT
            Assert.AreEqual(_hamidState.Favorability, -30,
                "Favorability should be clamped at min -30");
        }
        
        // ── TEST 9: Session Persistence (Serialization) ──────────────────
        
        [Test]
        public void Test_SessionData_Serialization()
        {
            // ARRANGE
            _hamidState.SetCondition("curiosity_shown", true);
            var scoring = new ScoringResult
            {
                favorability_delta = 25,
                emotion_pressure = "positive",
                curiosity_triggered = true,
                emotion_shift = "open",
                reason = "friendly"
            };
            _hamidState.ApplyScoring(scoring, 0);
            
            // ACT - Serialize to session
            var sessionData = _hamidState.ToSessionData("Hamid seemed interested in discussion.");
            
            // ASSERT
            Assert.AreEqual(sessionData.npc_id, "hamid");
            Assert.AreEqual(sessionData.favorability, 25);
            Assert.AreEqual(sessionData.emotion, "open");
            Assert.IsTrue(sessionData.conditions.curiosity_shown);
            Assert.AreEqual(sessionData.interaction_summary, "Hamid seemed interested in discussion.");
            
            // ACT - Deserialize back
            var restoredState = new NPCState("hamid", sessionData);
            
            // ASSERT
            Assert.AreEqual(restoredState.Favorability, 25, 
                "Restored favorability should match serialized value");
            Assert.AreEqual(restoredState.Emotion, EmotionState.Open,
                "Restored emotion should match serialized value");
            Assert.IsTrue(restoredState.GetCondition("curiosity_shown"),
                "Restored conditions should match");
        }
        
        // ── TEST 10: NPCManager State Access ─────────────────────────────
        
        [Test]
        public void Test_NPCManager_StateAccess()
        {
            // ARRANGE
            int hamidFav = _npcManager.GetFavorability("hamid");
            EmotionState hamidEmotion = _npcManager.GetEmotion("hamid");
            
            // ASSERT
            Assert.AreEqual(hamidFav, 0, "Initial favorability from manager should be 0");
            Assert.AreEqual(hamidEmotion, EmotionState.Open, 
                "Initial emotion from manager should be Open");
        }
        
        // ── TEST 11: Full Dialogue Turn Simulation ───────────────────────
        
        [Test]
        public void Test_DialogueFlow_Simulated()
        {
            // ARRANGE - Simulate a dialogue turn manually
            string npcId = "hamid";
            string userInput = "Salut Hamid, ça va?";
            
            var state = new NPCState(npcId, null);
            var initialFav = state.Favorability;
            
            // Simulate LLM response parsing
            var llmScoring = new ScoringResult
            {
                favorability_delta = 8,   // Positive interaction
                emotion_pressure = "positive",
                curiosity_triggered = false,
                emotion_shift = null,
                reason = "friendly greeting"
            };
            
            // Simulate reputation delta calculation (rules-based, no LLM)
            int repDelta = 1;  // Neutral keywords
            
            // ACT - Apply changes
            state.ApplyScoring(llmScoring, repDelta);
            
            // ASSERT - Verify state changed
            Assert.AreNotEqual(state.Favorability, initialFav,
                "Favorability should change after interaction");
            Assert.AreEqual(state.Favorability, initialFav + 8,
                "Favorability should match scoring delta");
            Assert.AreEqual(state.Emotion, EmotionState.Open,
                "Emotion should be determined by rules (likely Open)");
            
            // Verify serializable
            var sessionData = state.ToSessionData("Hamid reacted positively.");
            Assert.IsNotNull(sessionData, "Should serialize to session data");
        }
        
        [TearDown]
        public void TearDown()
        {
            // Reset for next test
            if (_npcManager != null)
                _npcManager.ResetCycle();
        }
    }
}
*/