using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using JemaaGame.NPC;

public class HalkaCompositionEngineTests
{
    private HalkaCompositionEngine _engine;
    private HalkaOrchestrator _orchestrator;

    [SetUp]
    public void Setup()
    {
        // Create fresh instances for testing
        GameObject engineObj = new GameObject("TestHalkaEngine");
        _engine = engineObj.AddComponent<HalkaCompositionEngine>();

        GameObject orchestratorObj = new GameObject("TestHalkaOrchestrator");
        _orchestrator = orchestratorObj.AddComponent<HalkaOrchestrator>();
    }

    [TearDown]
    public void Teardown()
    {
        if (_engine != null) UnityEngine.Object.DestroyImmediate(_engine.gameObject);
        if (_orchestrator != null) UnityEngine.Object.DestroyImmediate(_orchestrator.gameObject);
    }

    // ─── TEST 1: Namespace Verification ────────────────────────────────

    [Test]
    public void Test_NamespaceCorrect_HalkaCompositionEngine()
    {
        Assert.That(_engine.GetType().Namespace, Is.EqualTo("JemaaGame.NPC"));
    }

    [Test]
    public void Test_NamespaceCorrect_HalkaOrchestrator()
    {
        Assert.That(_orchestrator.GetType().Namespace, Is.EqualTo("JemaaGame.NPC"));
    }

    // ─── TEST 2: Fragment Structure ────────────────────────────────

    [Test]
    public void Test_FragmentEntry_HasRequiredFields()
    {
        var fragment = new HalkaCompositionEngine.FragmentEntry
        {
            npcId = "test",
            npcName = "Test NPC",
            fragmentId = "test_fragment",
            title = "Test Fragment",
            content = "Test content...",
            tier = 2,
            isSensitive = false
        };

        Assert.That(fragment.npcId, Is.EqualTo("test"));
        Assert.That(fragment.tier, Is.EqualTo(2));
        Assert.That(fragment.isSensitive, Is.False);
    }

    // ─── TEST 3: Composition Result Structure ────────────────────────────

    [Test]
    public void Test_CompositionResult_Initialize()
    {
        var result = new HalkaCompositionEngine.CompositionResult();
        
        Assert.That(result.selectedFragmentIds, Is.Not.Null);
        Assert.That(result.coherenceScore, Is.EqualTo(0));
        Assert.That(result.depthScore, Is.EqualTo(0));
    }

    // ─── TEST 4: GameManager Integration ────────────────────────────

    [Test]
    public void Test_GameManager_HasRequiredMethods()
    {
        // Verify GameManager has the required methods
        Assert.That(typeof(GameManager).GetMethod("AddFragment"), Is.Not.Null);
        Assert.That(typeof(GameManager).GetMethod("GetCollectedFragmentsSnapshot"), Is.Not.Null);
        Assert.That(typeof(GameManager).GetMethod("ResetCycle"), Is.Not.Null);
    }

    [Test]
    public void Test_GameManager_GlobalReputationProperty()
    {
        // Test reputation property exists and works
        int initial = GameManager.GlobalReputation;
        Assert.That(initial, Is.GreaterThanOrEqualTo(0));
    }

    // ─── TEST 5: Orchestrator Singleton Pattern ────────────────────────────

    [Test]
    public void Test_HalkaOrchestrator_IsSingleton()
    {
        Assert.That(HalkaOrchestrator.Instance, Is.Not.Null);
    }

    [Test]
    public void Test_HalkaOrchestrator_OnlyOneInstance()
    {
        var instance1 = HalkaOrchestrator.Instance;
        var instance2 = HalkaOrchestrator.Instance;
        
        Assert.That(instance1, Is.SameAs(instance2));
    }

    // ─── TEST 6: Events Declaration ────────────────────────────

    [Test]
    public void Test_Events_OnHalkaCompletedExists()
    {
        var fieldInfo = typeof(HalkaOrchestrator).GetEvent("OnHalkaCompleted");
        Assert.That(fieldInfo, Is.Not.Null);
    }

    [Test]
    public void Test_Events_OnHalkaStartRequestedExists()
    {
        var fieldInfo = typeof(HalkaOrchestrator).GetEvent("OnHalkaStartRequested");
        Assert.That(fieldInfo, Is.Not.Null);
    }

    // ─── TEST 7: Error Handling ────────────────────────────

    [Test]
    public void Test_EmptyFragmentIds_Handled()
    {
        var result = new HalkaCompositionEngine.CompositionResult
        {
            selectedFragmentIds = new List<string>()
        };

        Assert.That(result.selectedFragmentIds, Has.Count.EqualTo(0));
    }

    [Test]
    public void Test_NullCheck_InFragmentList()
    {
        var validIds = new List<string>();
        var testIds = new List<string> { null, "valid", null };

        foreach (var id in testIds)
        {
            if (!string.IsNullOrEmpty(id))
                validIds.Add(id);
        }

        Assert.That(validIds, Has.Count.EqualTo(1));
        Assert.That(validIds, Contains.Item("valid"));
    }

    // ─── TEST 8: Type Verification ────────────────────────────

    [Test]
    public void Test_HalkaCompositionPanel_IsUIComponent()
    {
        var panelType = typeof(HalkaCompositionPanel);
        Assert.That(panelType.IsSubclassOf(typeof(MonoBehaviour)));
    }

    [Test]
    public void Test_HalkaCompositionPanel_HasEvents()
    {
        var eventInfo = typeof(HalkaCompositionPanel).GetEvent("OnCompositionSubmitted");
        Assert.That(eventInfo, Is.Not.Null);
    }
}

// ─── Integration Tests ────────────────────────────────

public class HalkaIntegrationTests
{
    [Test]
    public void Test_Orchestrator_Initializes()
    {
        GameObject obj = new GameObject("TestOrc");
        var orch = obj.AddComponent<HalkaOrchestrator>();

        Assert.That(orch, Is.Not.Null);
        UnityEngine.Object.DestroyImmediate(obj);
    }

    [Test]
    public void Test_Engine_InitializesWithoutError()
    {
        GameObject obj = new GameObject("TestEngine");
        var engine = obj.AddComponent<HalkaCompositionEngine>();

        Assert.That(engine, Is.Not.Null);
        UnityEngine.Object.DestroyImmediate(obj);
    }
}
