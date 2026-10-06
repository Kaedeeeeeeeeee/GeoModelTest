using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class QuestNpcPhysicsTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private Keyboard _keyboard;
    private readonly HashSet<GameObject> _persistentRootsBefore = new HashSet<GameObject>();
    private InputSettings.UpdateMode _inputMode;
    private InputSettings.BackgroundBehavior _background;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode _editorInput;
#endif
    private UnityEngine.Object _backend;
    private bool _backendEnabled;
    private object _director;
    private object _quests;
    private float _previousTimeScale;
    private CursorLockMode _cursorLock;
    private bool _cursorVisible;

    private static Type T(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Get(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
    private static object Prop(object target, string name) => (target as Type ?? target.GetType()).GetProperty(name, Flags).GetValue(target is Type ? null : target);
    private static object Call(object target, string name, params object[] args) =>
        (target as Type ?? target.GetType()).GetMethod(name, Flags).Invoke(target is Type ? null : target, args);
    private static Component Find(string name) => (Component)UnityEngine.Object.FindFirstObjectByType(T(name));

    [UnityTest]
    public IEnumerator Kaede_ShouldOfferConversationAtTalkingDistance_BlockBody_AndAllowLaboratoryPassage()
    {
        _persistentRootsBefore.Clear();
        var persistentProbe = new GameObject("KaedePersistentSceneProbe");
        UnityEngine.Object.DontDestroyOnLoad(persistentProbe);
        foreach (var root in persistentProbe.scene.GetRootGameObjects())
            if (root != persistentProbe) _persistentRootsBefore.Add(root);
        UnityEngine.Object.DestroyImmediate(persistentProbe);
        _previousTimeScale = Time.timeScale;
        _cursorLock = Cursor.lockState;
        _cursorVisible = Cursor.visible;
        _backend = Resources.Load("BackendSettings");
        _backendEnabled = (bool)Get(_backend, "enableBackend");
        _backend.GetType().GetField("enableBackend", Flags).SetValue(_backend, false);
        _inputMode = InputSystem.settings.updateMode;
        _background = InputSystem.settings.backgroundBehavior;
#if UNITY_EDITOR
        _editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
#endif
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        _keyboard = InputSystem.AddDevice<Keyboard>();

        Call(T("ProgressResetService"), "ResetAll");
        PlayerPrefs.SetString("StoryFlags", "story.main.rescue");
        PlayerPrefs.SetInt("MainScene.ClassRoom.Hidden", 1);
        yield return SceneManager.LoadSceneAsync("MainScene");
        yield return new WaitForSecondsRealtime(4f);
        _director = Prop(T("StorySystem.StoryDirector"), "Instance");
        _quests = Prop(T("QuestSystem.QuestManager"), "Instance");
        StopDialogue();
        Call(_director, "ReloadFlags");
        Call(T("StorySystem.InvestigationProgress"), "Reset");

        Call(Prop(T("GameSceneManager"), "Instance"), "SwitchToScene", "Laboratory Scene");
        float deadline = Time.realtimeSinceStartup + 40f;
        while ((SceneManager.GetActiveScene().name != "Laboratory Scene" || (bool)Prop(T("GameSceneManager"), "IsLoadingScene")) && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.AreEqual("Laboratory Scene", SceneManager.GetActiveScene().name);
        yield return new WaitForSecondsRealtime(1f);
        int quizAnswers = 0;
        deadline = Time.realtimeSinceStartup + 90f;
        while ((bool)Prop(T("StorySystem.StoryDirector"), "IsStoryPlaybackActive") && Time.realtimeSinceStartup < deadline)
        {
            var choice = GameObject.Find("SubtitleCanvas/ChoicePanel/Choice0");
            if (choice != null && choice.GetComponent<Button>().interactable)
            {
                choice.GetComponent<Button>().onClick.Invoke();
                quizAnswers++;
            }
            var next = GameObject.Find("SubtitleCanvas/BG");
            if (next != null && next.GetComponent<Button>().interactable) next.GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.12f);
        }
        Assert.AreEqual(1, quizAnswers, "The real laboratory introduction must answer its first quiz correctly.");
        var attempts = (IList)Prop(Prop(T("StorySystem.QuizScoreManager"), "Instance"), "Attempts");
        Assert.AreEqual(1, attempts.Count);
        Assert.AreEqual("q.weathering_order", Get(attempts[0], "questionId"));
        Assert.AreEqual("q.weathering_order.correct_sequence", Get(attempts[0], "choiceId"));
        Assert.IsTrue((bool)Get(attempts[0], "isCorrect"));
        yield return null;
        // Complete the actual four-page control guide after the introduction.
        deadline = Time.realtimeSinceStartup + 5f;
        while (GameObject.Find("FirstControlGuide") == null && Time.realtimeSinceStartup < deadline) yield return null;
        var guide = GameObject.Find("FirstControlGuide");
        Assert.NotNull(guide);
        for (int page = 0; page < 4; page++)
        {
            guide.transform.Find("GuideCard/Begin").GetComponent<Button>().onClick.Invoke();
            yield return null;
        }
        for (int i = 0; i < 5; i++) yield return null;

        var player = Find("FirstPersonController");
        var controller = player.GetComponent<CharacterController>();
        var npc = GameObject.Find("DrKaedeQuestCube").GetComponent(T("QuestSystem.QuestNpcInteraction"));
        Assert.IsTrue(((Behaviour)player).enabled && controller.enabled);
        Assert.IsTrue((bool)Prop(npc, "HasNewConversation"));
        string[] stageIds = ((Array)Get(npc, "stages")).Cast<object>().Select(stage => (string)Get(stage, "questId")).ToArray();
        CollectionAssert.IsSubsetOf(new[] { "q.lab.drkaede", "q.chapter4.kaede", "q.chapter5.kaede", "q.chapter6.kaede" }, stageIds,
            "All Kaede stages must share this scene object's interaction and solid colliders.");
        Debug.LogWarning("[KaedePhysics] sharedStages=" + string.Join(",", stageIds));

        // Only set the initial position. Every approach, collision and exit below uses the
        // real CharacterController in the real scene, with bounded steps independent of deltaTime.
        Vector3 front = npc.transform.forward;
        front.y = 0f;
        front.Normalize();
        Vector3 start = npc.transform.position + front * 3f;
        start.y = player.transform.position.y;
        controller.enabled = false;
        player.transform.SetPositionAndRotation(start, Quaternion.LookRotation(-front));
        controller.enabled = true;
        Physics.SyncTransforms();
        yield return new WaitForFixedUpdate();
        yield return null;
        Assert.That(HorizontalDistance(player, npc), Is.EqualTo(3f).Within(0.02f));
        Assert.IsFalse((bool)Get(npc, "playerInRange"), "At 3 m the real player must be outside Kaede's interaction range.");
        Debug.LogWarning($"[KaedePhysics] start={player.transform.position:F4}; radius={controller.radius:F3}; scale={player.transform.lossyScale:F3}; height={controller.height:F3}; center={controller.center:F3}; npc={npc.transform.position:F4}; stage={Get(npc, "currentStageIndex")}");

        for (int step = 0; step < 60 && HorizontalDistance(player, npc) > 1.4f; step++)
        {
            controller.Move(-front * Mathf.Min(0.04f, HorizontalDistance(player, npc) - 1.4f));
            yield return new WaitForFixedUpdate();
            yield return null;
        }
        Debug.LogWarning($"[KaedePhysics] controllerEnabled={controller.enabled}; detectCollisions={controller.detectCollisions}; overlapRecovery={controller.enableOverlapRecovery}; playerLayer={player.gameObject.layer}; npcLayer={npc.gameObject.layer}; ignoredLayers={Physics.GetIgnoreLayerCollision(player.gameObject.layer, npc.gameObject.layer)}; physicsMode={Physics.simulationMode}; fixedDt={Time.fixedDeltaTime}; timeScale={Time.timeScale}; fpcEnabled={((Behaviour)player).enabled}; blocked={Prop(T("Core.GameInputState"), "GameplayBlocked")}; inPlay={Application.isPlaying}");
        foreach (var collider in npc.GetComponents<Collider>())
        {
            string shape = collider is CapsuleCollider capsule ? $"radius={capsule.radius:F3}; height={capsule.height:F3}; center={capsule.center:F3}" : "";
            Debug.LogWarning($"[KaedePhysics] collider={collider.GetType().Name}; enabled={collider.enabled}; trigger={collider.isTrigger}; bounds={collider.bounds}; rb={collider.attachedRigidbody}; {shape}");
        }
        Vector3 playerCenter = controller.transform.TransformPoint(controller.center);
        float playerRadius = controller.radius;
        float playerHalfAxis = controller.height * 0.5f - playerRadius;
        var overlapping = Physics.OverlapCapsule(playerCenter - Vector3.up * playerHalfAxis,
            playerCenter + Vector3.up * playerHalfAxis, playerRadius, ~0, QueryTriggerInteraction.Collide);
        Debug.LogWarning("[KaedePhysics] overlapping=" + string.Join(",", overlapping.Select(collider => collider.name + "/" + collider.GetType().Name + "/trigger=" + collider.isTrigger)));
        var shared = (Component)Get(npc, "sharedPrompt");
        var panel = (RectTransform)Prop(shared, "Panel");
        Debug.LogWarning($"[KaedePhysics] talkingDistance={HorizontalDistance(player, npc):F4}; player={player.transform.position:F4}; range={Get(npc, "playerInRange")}; sharedPrompt={panel.gameObject.activeInHierarchy}");
        Assert.That(HorizontalDistance(player, npc), Is.InRange(1.3f, 1.5f), "The real controller must reach a normal talking distance.");
        Assert.IsTrue((bool)Get(npc, "playerInRange"), "Kaede must become interactable at 1.3-1.5 m without entering her body.");
        Assert.IsTrue(shared.gameObject.activeInHierarchy && panel.gameObject.activeInHierarchy, "The shared E prompt must be visible at a normal talking distance.");
        Bounds interactionBounds = new Bounds(npc.transform.position + Vector3.up * 1.5f, new Vector3(2f, 3f, 2f));
        Assert.LessOrEqual(interactionBounds.min.y, controller.bounds.min.y, "The interaction volume must cover the player's full capsule height.");
        Assert.GreaterOrEqual(interactionBounds.max.y, controller.bounds.max.y);
        Debug.LogWarning($"[KaedePhysics] interactionBounds={interactionBounds}; playerBounds={controller.bounds}; npcMarkerHeight={Get(npc, "markerHeight")}");

        // Use the player's actual view at 1.4 m. A target beside Kaede crosses the
        // old wide trigger but misses her narrow solid body, like ordinary tool rays.
        var playerCamera = player.GetComponentInChildren<Camera>();
        Assert.NotNull(playerCamera);
        Assert.IsTrue(Physics.queriesHitTriggers, "This regression must exercise the default trigger-inclusive raycast settings.");
        Vector3 rayOrigin = playerCamera.transform.position;
        Vector3 rayTarget = npc.transform.position - npc.transform.right * 0.8f;
        rayTarget.y = rayOrigin.y;
        Vector3 rayOffset = rayTarget - rayOrigin;
        bool rayHit = Physics.Raycast(rayOrigin, rayOffset.normalized, out RaycastHit viewHit, rayOffset.magnitude);
        bool hitKaedeTrigger = rayHit && viewHit.collider.gameObject == npc.gameObject && viewHit.collider.isTrigger;
        Debug.LogWarning($"[KaedePhysics] defaultViewRayOrigin={rayOrigin:F4}; target={rayTarget:F4}; queriesHitTriggers={Physics.queriesHitTriggers}; hit={(rayHit ? viewHit.collider.name + "/" + viewHit.collider.GetType().Name : "none")}; hitKaedeTrigger={hitKaedeTrigger}");
        Assert.IsFalse(hitKaedeTrigger, "Default Physics.Raycast from the player's view must not be intercepted by a wide Kaede trigger.");
        var allViewHits = Physics.RaycastAll(rayOrigin, rayOffset.normalized, rayOffset.magnitude);
        Assert.IsFalse(allViewHits.Any(hit => hit.collider.gameObject == npc.gameObject && hit.collider.isTrigger),
            "Other geometry must not hide a Kaede trigger interception along the same default ray.");
        Assert.Zero(npc.GetComponents<Collider>().Count(collider => collider.enabled && collider.isTrigger),
            "Kaede's interaction range must not install a trigger that ordinary gameplay raycasts can hit.");
        var solidBody = npc.GetComponents<CapsuleCollider>().Single(collider => collider.enabled && !collider.isTrigger);
        Assert.That(solidBody.radius * Mathf.Max(Mathf.Abs(npc.transform.lossyScale.x), Mathf.Abs(npc.transform.lossyScale.z)),
            Is.EqualTo(0.3f).Within(0.001f), "The narrow solid Kaede body must remain installed.");

        bool hitBody = false;
        for (int step = 0; step < 40; step++)
        {
            hitBody |= (controller.Move(-front * 0.04f) & CollisionFlags.Sides) != 0;
            yield return new WaitForFixedUpdate();
            yield return null;
        }
        Debug.LogWarning($"[KaedePhysics] bodyStopDistance={HorizontalDistance(player, npc):F4}; sideCollision={hitBody}; player={player.transform.position:F4}");
        Assert.IsTrue(hitBody, "Walking towards Kaede must hit a real solid collider.");
        Assert.That(HorizontalDistance(player, npc), Is.InRange(0.4f, 1f), "The solid body must keep the player's center at least 0.4 m away.");
        Assert.IsTrue(panel.gameObject.activeInHierarchy);

        // Dynamic pickups may be dropped beside Kaede. The same priority-3 shared
        // prompt used by SampleCollector must suppress both her hint and her E action.
        var pickupPrompt = (Component)Call(T("UISystem.InteractionPrompt"), "Create",
            "KaedePriorityPickupProbe", "PickupPanel", 170,
            Enum.Parse(T("UISystem.MobileControlHint+Control"), "Interact"));
        Call(pickupPrompt, "SetVisible", true, 3, 0.1f);
        yield return null;
        Assert.IsFalse(panel.gameObject.activeInHierarchy, "The priority-3 pickup prompt must win over the priority-2 NPC prompt.");
        Assert.IsTrue(((RectTransform)Prop(pickupPrompt, "Panel")).gameObject.activeInHierarchy);
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.E));
        yield return null;
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
        yield return null;
        Debug.LogWarning($"[KaedePhysics] priority3SuppressesNpc={!panel.gameObject.activeInHierarchy}; npcConversation={Prop(T("StorySystem.StoryDirector"), "IsStoryPlaybackActive")}");
        Assert.IsFalse((bool)Prop(T("StorySystem.StoryDirector"), "IsStoryPlaybackActive"), "E must not start the hidden NPC action when a pickup owns the shared prompt.");
        Assert.IsFalse((bool)Get(npc, "isInteracting"));
        UnityEngine.Object.Destroy(pickupPrompt.gameObject);
        yield return null;
        yield return null;
        Assert.IsTrue(panel.gameObject.activeInHierarchy, "Kaede's prompt must return after the higher-priority pickup prompt disappears.");

        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.E));
        yield return null;
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
        yield return null;
        Debug.LogWarning($"[KaedePhysics] keyEConversation={Prop(T("StorySystem.StoryDirector"), "IsStoryPlaybackActive")}; interacting={Get(npc, "isInteracting")}");
        Assert.IsTrue((bool)Prop(T("StorySystem.StoryDirector"), "IsStoryPlaybackActive"), "A real Input System E key must start Kaede's conversation.");
        Assert.IsTrue((bool)Get(npc, "isInteracting"));

        // Finish the actual dialogue so exit assertions exercise the normal interaction state.
        deadline = Time.realtimeSinceStartup + 40f;
        while ((bool)Prop(T("StorySystem.StoryDirector"), "IsStoryPlaybackActive") && Time.realtimeSinceStartup < deadline)
        {
            var next = GameObject.Find("SubtitleCanvas/BG");
            if (next != null && next.GetComponent<Button>().interactable) next.GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.08f);
        }
        Assert.IsFalse((bool)Prop(T("StorySystem.StoryDirector"), "IsStoryPlaybackActive"));
        Assert.IsFalse((bool)Get(npc, "isInteracting"));
        for (int step = 0; step < 70 && HorizontalDistance(player, npc) < 2.2f; step++)
        {
            controller.Move(front * 0.04f);
            yield return new WaitForFixedUpdate();
            yield return null;
        }
        Debug.LogWarning($"[KaedePhysics] exitDistance={HorizontalDistance(player, npc):F4}; range={Get(npc, "playerInRange")}; sharedPrompt={panel.gameObject.activeInHierarchy}");
        Assert.Greater(HorizontalDistance(player, npc), 2f);
        Assert.IsFalse((bool)Get(npc, "playerInRange"), "Moving beyond 2 m must clear the real interaction range.");
        Assert.IsFalse(panel.gameObject.activeInHierarchy, "The prompt must disappear beyond 2 m.");

        var cutting = Find("SampleCuttingSystem.CuttingStationInteraction");
        Assert.NotNull(cutting, "The actual laboratory cutting station must remain available.");
        float stationDistance = HorizontalDistance(cutting, npc);
        float cuttingRange = (float)Get(cutting, "interactionRange");
        var pickups = UnityEngine.Object.FindObjectsByType(T("SampleCollector"), FindObjectsSortMode.None).Cast<Component>().ToArray();
        Debug.LogWarning($"[KaedePhysics] cutting={cutting.transform.position:F4}; centerSeparation={stationDistance:F4}; cuttingRange={cuttingRange:F3}; staticPickups={pickups.Length}; promptPriorities=pickup3,npc2");
        Assert.Greater(stationDistance, cuttingRange + 2f,
            "The enlarged Kaede range must remain separate from the real cutting station's interaction range.");
        foreach (var pickup in pickups)
        {
            float pickupRange = (float)Get(pickup, "interactionRange");
            float separation = HorizontalDistance(pickup, npc);
            Debug.LogWarning($"[KaedePhysics] nearbyPickup={pickup.name}; separation={separation:F3}; range={pickupRange:F3}");
            Assert.Greater(separation, pickupRange + 2f, "A permanent laboratory pickup must not overlap Kaede's expanded prompt.");
        }

        // The left side already contains shelves. Record that original obstruction,
        // then use the existing right-hand aisle to reach the station behind her.
        Vector3 leftProbe = npc.transform.position + front * 1.9f + npc.transform.right * 1.5f;
        leftProbe.y = player.transform.position.y;
        Vector3 probeCenter = leftProbe + Vector3.up * controller.center.y;
        float probeAxis = controller.height * 0.5f - controller.radius;
        var leftObstacles = Physics.OverlapCapsule(probeCenter - Vector3.up * probeAxis,
            probeCenter + Vector3.up * probeAxis, controller.radius, ~0, QueryTriggerInteraction.Ignore);
        foreach (var obstacle in leftObstacles)
        {
            if (obstacle == controller || obstacle.bounds.max.y <= controller.bounds.min.y + 0.05f) continue;
            Debug.LogWarning($"[KaedePhysics] originalLeftObstruction={obstacle.name}/{obstacle.GetType().Name}; bounds={obstacle.bounds}; addedKaedeBody={obstacle.transform == npc.transform}");
        }
        // These waypoints are tested against every real scene collider, rather than a probe room.
        Vector3 side = -npc.transform.right;
        side.y = 0f;
        side.Normalize();
        Vector3[] passage =
        {
            npc.transform.position + front * 2.2f + side * 1.2f,
            npc.transform.position - front * 1.7f + side * 1.2f,
            cutting.transform.position + front * 2.2f
        };
        foreach (Vector3 waypoint in passage)
        {
            for (int step = 0; step < 240; step++)
            {
                Vector3 offset = waypoint - player.transform.position;
                offset.y = 0f;
                if (offset.magnitude <= 0.12f) break;
                controller.Move(Vector3.ClampMagnitude(offset, 0.04f));
                yield return new WaitForFixedUpdate();
                yield return null;
            }
            Vector3 remaining = waypoint - player.transform.position;
            remaining.y = 0f;
            Debug.LogWarning($"[KaedePhysics] passageWaypoint={waypoint:F4}; reached={player.transform.position:F4}; remaining={remaining.magnitude:F4}; kaedeDistance={HorizontalDistance(player, npc):F4}");
            Assert.LessOrEqual(remaining.magnitude, 0.15f, "The player must be able to walk around Kaede and reach the station behind her.");
        }
        Assert.IsTrue((bool)Get(cutting, "playerInRange"), "The cutting station must be reachable through real CharacterController movement.");
        Assert.IsFalse((bool)Get(npc, "playerInRange"), "At the cutting station Kaede must not compete for interaction.");
    }

    private static float HorizontalDistance(Component player, Component npc) => Vector2.Distance(
        new Vector2(player.transform.position.x, player.transform.position.z),
        new Vector2(npc.transform.position.x, npc.transform.position.z));

    private void StopDialogue()
    {
        Call(_director, "CancelPlayback");
        Call(_quests, "CancelPendingPlayback");
        Call(T("UISystem.FirstControlGuide"), "CloseCurrent");
        Call(T("Core.GameInputState"), "ReleaseAll");
        Time.timeScale = 1f;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        try
        {
            if (_director != null && _quests != null) StopDialogue();
            // Remove only roots created by this fixture, while its test keyboard
            // is still available to UI modules and OnDestroy callbacks.
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) UnityEngine.Object.Destroy(root);
            var persistentProbe = new GameObject("KaedePersistentSceneProbe");
            UnityEngine.Object.DontDestroyOnLoad(persistentProbe);
            foreach (var root in persistentProbe.scene.GetRootGameObjects())
                if (root != persistentProbe && !_persistentRootsBefore.Contains(root)) UnityEngine.Object.Destroy(root);
            UnityEngine.Object.Destroy(persistentProbe);
            yield return null;
        }
        finally
        {
            if (_keyboard != null)
            {
                InputSystem.RemoveDevice(_keyboard);
                InputSystem.settings.updateMode = _inputMode;
                InputSystem.settings.backgroundBehavior = _background;
#if UNITY_EDITOR
                InputSystem.settings.editorInputBehaviorInPlayMode = _editorInput;
#endif
            }
            if (_backend != null) _backend.GetType().GetField("enableBackend", Flags).SetValue(_backend, _backendEnabled);
            Time.timeScale = _previousTimeScale;
            Cursor.lockState = _cursorLock;
            Cursor.visible = _cursorVisible;
        }
    }
}
