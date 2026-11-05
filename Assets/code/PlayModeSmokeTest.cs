using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Automated play-mode acceptance checks. It only runs when the editor is
/// started with the "-paralysisSmoke" command-line argument, so normal play
/// sessions are unaffected.
///
/// Run from a terminal:
///   Unity.exe -batchmode -nographics -projectPath &lt;project&gt;
///     -executeMethod SmokeTestRunner.Run -paralysisSmoke
///
/// Results are written to Logs/smoke_results.txt.
/// </summary>
[NoAutoStaticsCleanup]
public class PlayModeSmokeTest : MonoBehaviour
{
    private const string SmokeArgument = "-paralysisSmoke";
    private const string ResultFile = "smoke_results.txt";

    private static PlayModeSmokeTest runner;

    private readonly StringBuilder report = new StringBuilder();
    private int passed;
    private int failed;

    /// <summary>
    /// Domain reload is disabled in this project, so statics survive between
    /// Play Mode sessions. Clear the old runner explicitly so a second run
    /// from the same editor still boots.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        runner = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (runner != null)
            return;

        if (!HasArgument(SmokeArgument) && !ConsumeRequestFile())
            return;

        GameObject go = new GameObject("PlayModeSmokeTest");
        DontDestroyOnLoad(go);
        runner = go.AddComponent<PlayModeSmokeTest>();
    }

    private const string RequestFileName = "paralysis_smoke.request";

    /// <summary>
    /// The editor can start the test by dropping a request file into the
    /// project Temp folder, which makes it possible to run the checks in an
    /// editor that is already open.
    /// </summary>
    private static bool ConsumeRequestFile()
    {
        string path = Path.Combine(
            Path.GetDirectoryName(Application.dataPath) ?? ".",
            "Temp",
            RequestFileName
        );

        if (!File.Exists(path))
            return false;
        try
        {
            File.Delete(path);
        }
        catch (Exception)
        {
            // Ignore, the test still runs.
        }

        return true;
    }

    private static bool HasArgument(string value)
    {
        string[] args = Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], value, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private IEnumerator Start()
    {
        // The smoke test must keep running when the editor is not focused;
        // otherwise Play Mode pauses and the real-time deadlines below expire
        // while no frames are being simulated.
        Application.runInBackground = true;

        yield return new WaitForSecondsRealtime(0.5f);

        Line("=== Paralysis play-mode smoke test ===");
        Line("Unity " + Application.unityVersion);

        yield return HomeTest();
        yield return StoreTest();
        yield return DrivingTest();
        yield return MenuTest();
        yield return MusicTest();
        yield return EndTest();

        Finish();
    }

    #region Tests

    private IEnumerator HomeTest()
    {
        ResetStory();

        SceneManager.LoadScene("Home");
        yield return new WaitForSecondsRealtime(2.5f);

        Section("HOME");

        Check(
            "active scene is Home",
            SceneManager.GetActiveScene().name == "Home"
        );
        Check("player exists", InteractionUtil.FindPlayer() != null);
        Check("camera exists", InteractionUtil.FindCamera() != null);
        Check("StoryDirector exists", StoryDirector.Instance != null);
        CheckNoPinkMaterials("Home");

        DialogueSystem dialogue = DialogueSystem.Instance;

        Check("DialogueSystem exists", dialogue != null);

        // The opening starts after the scene has finished loading and its
        // intro delay has elapsed, which can take a few seconds in batch mode.
        yield return WaitForDialogue(10f);

        Check(
            "opening dialogue is showing",
            DialogueSystem.Instance != null &&
            DialogueSystem.Instance.IsVisible
        );

        yield return ClickThroughDialogue(25f);

        // The objective is activated a short delay after the intro dialogue.
        yield return new WaitForSecondsRealtime(1f);

        Check(
            "opening dialogue finished",
            DialogueSystem.Instance == null || !DialogueSystem.Instance.IsBusy
        );

        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsBusy)
        {
            Line(
                "dialogue still busy: " +
                DialogueSystem.Instance.CurrentConversationId
            );
        }

        ObjectiveManager objectives = ObjectiveManager.Instance;

        Check("ObjectiveManager exists", objectives != null);
        Check(
            "turn_off_lights objective active",
            objectives != null && objectives.HasActive("turn_off_lights")
        );
        Check(
            "LightsObjective exists",
            FindAnyObjectByType<LightsObjective>() != null
        );
        Check(
            "light switches exist",
            FindObjectsByType<LightSwitch>().Length > 0
        );

        InteractableObject[] inspectables =
            FindObjectsByType<InteractableObject>();

        Check("inspectable objects exist", inspectables.Length > 0);

        if (inspectables.Length == 0)
            yield break;

        InteractableObject target = inspectables[0];

        Check("player unlocked before inspection", !PlayerControl.IsLocked);

        target.Interact();
        yield return null;

        Check("inspection started", target.IsInspecting);
        Check(
            "player locked while inspecting",
            PlayerControl.IsLocked
        );

        target.StopInspecting();

        // Closing runs a short camera transition before the modal and the
        // player lock are released.
        yield return new WaitForSecondsRealtime(1f);

        Check("inspection ended", !target.IsInspecting);
        Check(
            "player unlocked after inspection",
            !PlayerControl.IsLocked
        );
    }

    private IEnumerator StoreTest()
    {
        ResetStory();

        SceneManager.LoadScene("Store");
        yield return new WaitForSecondsRealtime(2.5f);

        Section("STORE");

        Check(
            "active scene is Store",
            SceneManager.GetActiveScene().name == "Store"
        );
        Check("player exists", InteractionUtil.FindPlayer() != null);
        Check("camera exists", InteractionUtil.FindCamera() != null);
        Check("StoryDirector exists", StoryDirector.Instance != null);
        Check(
            "StoryDirector store scene is Store",
            StoryDirector.Instance != null &&
            StoryDirector.Instance.StoreScene == "Store"
        );
        Check(
            "single audio listener",
            FindObjectsByType<AudioListener>().Length == 1
        );
        CheckNoPinkMaterials("Store");

        Check(
            "objective HUD uses authored canvas",
            ObjectiveManager.Instance != null &&
            ObjectiveManager.Instance.GetComponentInChildren<ObjectiveView>(
                true
            ) != null
        );
        Check(
            "instruction toast uses authored canvas",
            ToastUI.Instance != null &&
            ToastUI.Instance.GetComponentInChildren<ToastView>(true) != null
        );
        Check(
            "quest log uses authored canvas",
            QuestLogUI.Instance != null &&
            QuestLogUI.Instance.GetComponentInChildren<QuestLogView>(true) !=
            null
        );

        DialogueDatabase dialogueDatabase = DialogueDatabase.Load();
        int dialogueLineCount = 0;
        int dialogueVoiceCount = 0;

        if (dialogueDatabase != null && dialogueDatabase.sequences != null)
        {
            for (int s = 0; s < dialogueDatabase.sequences.Count; s++)
            {
                DialogueSequence sequence = dialogueDatabase.sequences[s];

                if (sequence == null || sequence.lines == null)
                    continue;

                for (int i = 0; i < sequence.lines.Length; i++)
                {
                    DialogueLine line = sequence.lines[i];

                    if (line == null)
                        continue;

                    dialogueLineCount++;

                    if (line.voiceClip != null)
                        dialogueVoiceCount++;
                }
            }
        }

        Check(
            "dialogue lines have voice tracks",
            dialogueLineCount > 0 &&
            dialogueVoiceCount == dialogueLineCount
        );

        if (dialogueVoiceCount != dialogueLineCount)
        {
            Line(
                "  voice tracks: " + dialogueVoiceCount + " / " +
                dialogueLineCount
            );
        }

        UnityEngine.UI.Text[] uiTexts =
            FindObjectsByType<UnityEngine.UI.Text>(
                FindObjectsInactive.Include
            );

        int wrongFont = 0;

        for (int i = 0; i < uiTexts.Length; i++)
        {
            if (uiTexts[i] != null && uiTexts[i].font != GameFont.Default)
                wrongFont++;
        }

        Check(
            "all UI text uses the electronic font",
            uiTexts.Length > 0 && wrongFont == 0
        );

        ShoppingCart cart = ShoppingCart.Instance;

        Check("ShoppingCart exists", cart != null);
        Check("cart requires bread", cart != null && cart.IsRequired("bread"));
        Check("cart requires milk", cart != null && cart.IsRequired("milk"));
        Check("cart requires eggs", cart != null && cart.IsRequired("eggs"));

        GroceryItem[] groceries =
            FindObjectsByType<GroceryItem>();

        StringBuilder groceryList = new StringBuilder();
        int listedGroceries = 0;

        for (int i = 0; i < groceries.Length; i++)
        {
            if (groceries[i] == null)
                continue;

            bool onList = cart != null && cart.IsRequired(groceries[i].ItemId);

            if (onList)
                listedGroceries++;

            if (groceryList.Length > 0)
                groceryList.Append(", ");

            groceryList.Append(groceries[i].ItemId);
        }

        Line(
            "  active grocery items: " + groceries.Length + " (" +
            groceryList + ")"
        );

        // The store also dresses the shelves with produce that is not on the
        // shopping list, so the check counts the items the list needs.
        Check("three grocery items exist", listedGroceries == 3);

        // The store intro dialogue runs before StoryDirector activates the
        // shopping objective. Wait for the objective instead of assuming the
        // dialogue has already started when this test resumes.
        yield return WaitForObjective("collect_groceries", 20f);

        ObjectiveManager objectives = ObjectiveManager.Instance;

        Check(
            "collect_groceries objective active",
            objectives != null && objectives.HasActive("collect_groceries")
        );

        // The incomplete path is unchanged by the transformation work: the
        // short failure conversation runs, no camera/monster sequence starts,
        // and the list reopens so the player can keep shopping.
        CashierInteraction prematureCashier =
            FindAnyObjectByType<CashierInteraction>();

        Check("cashier exists before shopping", prematureCashier != null);

        if (prematureCashier != null)
        {
            prematureCashier.Interact();

            float incompleteDeadline = Time.realtimeSinceStartup + 20f;

            while ((QuestLogUI.Instance == null ||
                    !QuestLogUI.Instance.IsOpen) &&
                   Time.realtimeSinceStartup < incompleteDeadline)
            {
                DialogueSystem active = DialogueSystem.Instance;

                if (active != null && active.IsBusy)
                    active.Click();

                yield return new WaitForSecondsRealtime(0.15f);
            }

            Check(
                "incomplete checkout opens the list",
                QuestLogUI.Instance != null && QuestLogUI.Instance.IsOpen
            );
            Check(
                "incomplete checkout does not complete",
                !prematureCashier.CheckoutCompleted
            );

            if (QuestLogUI.Instance != null && QuestLogUI.Instance.IsOpen)
                QuestLogUI.Instance.Close();

            yield return new WaitForSecondsRealtime(0.4f);

            Check(
                "incomplete checkout releases input",
                !UIState.BlocksGameplay && !PlayerControl.IsLocked
            );
        }

        for (int i = 0; i < groceries.Length; i++)
        {
            // Only the shopping list is picked up. The produce that dresses
            // the shelves is not on the list and would answer with its own
            // "I don't think I need that" line.
            if (groceries[i] == null || !groceries[i].CanInteract)
                continue;

            if (cart == null || !cart.IsRequired(groceries[i].ItemId))
                continue;

            groceries[i].Interact();
        }

        yield return null;

        Check("cart is complete", cart != null && cart.IsComplete);

        // The all-collected dialogue starts a fraction of a second after the
        // last pickup and the pay objective appears when it ends.
        yield return WaitForObjective("pay_groceries", 20f);

        Check(
            "collect_groceries completed",
            objectives != null && objectives.IsDone("collect_groceries")
        );
        Check(
            "pay_groceries objective active",
            objectives != null && objectives.HasActive("pay_groceries")
        );

        CashierInteraction cashier =
            FindAnyObjectByType<CashierInteraction>();

        Check("cashier exists", cashier != null);

        if (cashier == null)
            yield break;

        // The ending replaces the scene, but this section verifies the
        // post-checkout state, so keep the demo in the store here.
        cashier.EndsDemo = false;

        StoreLightingController lighting =
            FindAnyObjectByType<StoreLightingController>();
        MonsterApproachController approach =
            FindAnyObjectByType<MonsterApproachController>();
        MonsterAnimationDriver[] monsterDrivers = FindObjectsByType<
            MonsterAnimationDriver>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        MonsterAnimationDriver monster = monsterDrivers.Length > 0
            ? monsterDrivers[0]
            : null;
        Camera cam = InteractionUtil.FindCamera();
        Vector3 cameraHomeLocal = cam != null
            ? cam.transform.localPosition
            : Vector3.zero;
        Quaternion cameraHomeRotation = cam != null
            ? cam.transform.localRotation
            : Quaternion.identity;

        Light[] storeLights = FindObjectsByType<Light>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        Color[] lightColors = new Color[storeLights.Length];
        float[] lightIntensities = new float[storeLights.Length];
        float[] lightRanges = new float[storeLights.Length];
        float[] lightSpotAngles = new float[storeLights.Length];
        bool[] lightEnabled = new bool[storeLights.Length];

        for (int i = 0; i < storeLights.Length; i++)
        {
            Light light = storeLights[i];
            lightColors[i] = light != null ? light.color : Color.clear;
            lightIntensities[i] = light != null ? light.intensity : 0f;
            lightRanges[i] = light != null ? light.range : 0f;
            lightSpotAngles[i] = light != null ? light.spotAngle : 0f;
            lightEnabled[i] = light != null && light.enabled;
        }

        Transform monsterRoot = approach != null ? approach.MonsterRoot : null;
        Vector3 monsterHome = monsterRoot != null
            ? monsterRoot.position
            : Vector3.zero;
        bool sawReach = false;
        bool sawMonsterClose = false;

        Check("cashier lighting controller exists", lighting != null);
        Check("cashier monster approach exists", approach != null);

        cashier.Interact();

        float deadline = Time.realtimeSinceStartup + 55f;

        while (!cashier.CheckoutCompleted &&
               Time.realtimeSinceStartup < deadline)
        {
            DialogueSystem active = DialogueSystem.Instance;

            if (active != null && active.IsBusy)
                active.Click();

            MonsterAnimationDriver liveMonster =
                FindAnyObjectByType<MonsterAnimationDriver>();

            if (liveMonster != null)
            {
                if (liveMonster.IsReaching)
                    sawReach = true;

                if (cam != null && Vector3.Distance(
                        cam.transform.position,
                        liveMonster.FocusPoint) < 2.5f)
                {
                    sawMonsterClose = true;
                }
            }

            yield return new WaitForSecondsRealtime(0.2f);
        }

        Check("cashier checkout completed", cashier.CheckoutCompleted);
        Check("cashier monster reached the camera", sawReach);
        Check("cashier monster came close to the player", sawMonsterClose);
        Check(
            "pay_groceries completed",
            objectives != null && objectives.IsDone("pay_groceries")
        );

        yield return new WaitForSecondsRealtime(1.8f);

        Check(
            "cashier sequence unlocks the player",
            !PlayerControl.IsLocked
        );
        Check(
            "cashier sequence releases its modal",
            !UIState.BlocksGameplay
        );
        Check(
            "cashier lighting is restored",
            lighting == null || !lighting.IsRed
        );

        int changedLights = 0;

        for (int i = 0; i < storeLights.Length; i++)
        {
            Light light = storeLights[i];

            if (light == null)
                continue;

            if (light.enabled != lightEnabled[i] ||
                Mathf.Abs(light.intensity - lightIntensities[i]) > 0.001f ||
                Mathf.Abs(light.range - lightRanges[i]) > 0.001f ||
                Mathf.Abs(light.spotAngle - lightSpotAngles[i]) > 0.001f ||
                (light.color - lightColors[i]).maxColorComponent > 0.001f)
            {
                changedLights++;
            }
        }

        Check("cashier restores every light property", changedLights == 0);

        if (changedLights > 0)
            Line("  restored light mismatches: " + changedLights);

        Check(
            "cashier restores the monster transform",
            monsterRoot != null &&
            Vector3.Distance(monsterRoot.position, monsterHome) < 0.05f
        );
        Check(
            "cashier hides the transformed model",
            monster == null || !monster.gameObject.activeInHierarchy
        );

        cashier.Interact();
        yield return new WaitForSecondsRealtime(0.5f);

        Check(
            "cashier checkout cannot run twice",
            cashier.CheckoutCompleted &&
            !UIState.BlocksGameplay &&
            !PlayerControl.IsLocked
        );

        // The pause menu used to open after the cashier but ignore the mouse.
        // Reproduce the real post-checkout state: no modal may be left behind,
        // and a pointer raycast must reach the authored CONTINUE hitbox.
        yield return new WaitForSecondsRealtime(0.25f);

        Check(
            "no modal blocks pause after checkout",
            !UIState.BlocksGameplay
        );

        if (UIState.BlocksGameplay)
        {
            object top = UIState.Top;

            Line(
                "  blocking modal: " +
                (top == null ? "null" : top.GetType().Name)
            );
        }

        PauseManager pauseAfterCheckout = PauseManager.Ensure();
        bool openedAfterCheckout = pauseAfterCheckout.Open();

        Check(
            "pause opens after checkout",
            openedAfterCheckout && pauseAfterCheckout.IsOpen
        );

        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        Check(
            "single event system after checkout",
            eventSystems.Length == 1
        );
        Check(
            "event system has an input module",
            EventSystem.current != null &&
            EventSystem.current.currentInputModule != null
        );

        // Let the opening slide finish so the button is at its final position.
        yield return new WaitForSecondsRealtime(1.2f);

        Transform pauseButtonsAfter = FindTransform("PauseButtons");
        MenuButton continueAfter = FindMenuButton(
            pauseButtonsAfter,
            "CONTINUE"
        );

        bool pointerHit = false;
        bool pointerClicked = false;

        if (continueAfter != null &&
            continueAfter.Hitbox != null &&
            EventSystem.current != null)
        {
            RectTransform hitRect = continueAfter.Hitbox.rectTransform;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                null,
                hitRect.TransformPoint(hitRect.rect.center)
            );

            PointerEventData pointer = new PointerEventData(EventSystem.current)
            {
                position = screenPoint,
                button = PointerEventData.InputButton.Left
            };

            List<RaycastResult> hits = new List<RaycastResult>();

            EventSystem.current.RaycastAll(pointer, hits);

            if (hits.Count > 0)
            {
                GameObject hit = hits[0].gameObject;

                pointerHit =
                    hit == continueAfter.gameObject ||
                    hit.transform.IsChildOf(continueAfter.transform);

                Line("  pause raycast top: " + hit.name);

                ExecuteEvents.ExecuteHierarchy(
                    hit,
                    pointer,
                    ExecuteEvents.pointerClickHandler
                );

                pointerClicked = true;
            }
            else
            {
                Line("  pause raycast: no hits");
            }
        }

        Check("pause CONTINUE is hittable after checkout", pointerHit);

        yield return new WaitForSecondsRealtime(1.2f);

        Check(
            "pause CONTINUE click resumes after checkout",
            pointerClicked && !pauseAfterCheckout.IsOpen
        );

        if (pauseAfterCheckout.IsOpen)
        {
            pauseAfterCheckout.Resume();
            yield return new WaitForSecondsRealtime(1.2f);
        }

        // Replay cleanup: ResetCheckout must restore every temporary state so
        // the sequence can run again without stacking controllers or lights.
        cashier.ResetCheckout();
        yield return null;

        Check("cashier reset clears completion", !cashier.CheckoutCompleted);
        Check(
            "cashier reset unlocks the player",
            !PlayerControl.IsLocked && !UIState.BlocksGameplay
        );
        Check(
            "cashier reset restores lighting",
            lighting == null || !lighting.IsRed
        );
        Check(
            "cashier reset restores the monster",
            monsterRoot != null &&
            Vector3.Distance(monsterRoot.position, monsterHome) < 0.05f
        );

        // Interruption: if the cashier leaves the scene while the cinematic
        // is running, the camera, the lights and the monster all have to be
        // handed back. Otherwise the player stays locked in a cutscene.
        cashier.Interact();
        yield return new WaitForSecondsRealtime(1.5f);

        Check(
            "cashier sequence locks the player",
            PlayerControl.IsLocked
        );

        if (DialogueSystem.Instance != null)
            DialogueSystem.Instance.StopImmediately();

        yield return new WaitForSecondsRealtime(0.2f);

        cashier.enabled = false;
        yield return new WaitForSecondsRealtime(0.4f);

        Check(
            "interrupted cashier unlocks the player",
            !PlayerControl.IsLocked && !UIState.BlocksGameplay
        );
        Check(
            "interrupted cashier restores the camera",
            cam != null &&
            Vector3.Distance(cam.transform.localPosition, cameraHomeLocal) <
                0.01f &&
            Quaternion.Angle(cam.transform.localRotation, cameraHomeRotation) <
                0.5f
        );
        Check(
            "interrupted cashier restores lighting",
            lighting == null || !lighting.IsRed
        );
        Check(
            "interrupted cashier restores the monster",
            monsterRoot != null &&
            Vector3.Distance(monsterRoot.position, monsterHome) < 0.05f
        );
        Check(
            "interrupted cashier hides the transformed model",
            monster == null || !monster.gameObject.activeInHierarchy
        );

        CashierAnimationDriver normalCashier =
            FindAnyObjectByType<CashierAnimationDriver>();

        Check(
            "interrupted cashier brings the normal cashier back",
            normalCashier == null || normalCashier.gameObject.activeInHierarchy
        );

        cashier.enabled = true;
    }

    private IEnumerator DrivingTest()
    {
        ResetStory();

        SceneManager.LoadScene("CarDriving");
        yield return new WaitForSecondsRealtime(2.5f);

        // Loading the driving scene can stall on shader compilation for longer
        // than the wait above. When that happens the check runs on the very
        // frame CarDriveScene.Start creates the camera, before its first
        // LateUpdate. Wait for the camera to actually tick before reading it.
        CarCamera carCamera = FindAnyObjectByType<CarCamera>();
        float cameraDeadline = Time.realtimeSinceStartup + 5f;

        while ((carCamera == null || carCamera.LastUpdateFrame == 0) &&
               Time.realtimeSinceStartup < cameraDeadline)
        {
            yield return null;
            carCamera = FindAnyObjectByType<CarCamera>();
        }

        Section("DRIVING");

        Check(
            "active scene is CarDriving",
            SceneManager.GetActiveScene().name == "CarDriving"
        );
        Check("driving session active", CarDriveSession.Active);

        CarDriveController controller =
            FindAnyObjectByType<CarDriveController>();

        Check("car controller exists", controller != null);
        Check("car camera exists", carCamera != null);
        Check(
            "single audio listener",
            FindObjectsByType<AudioListener>().Length == 1
        );
        Check(
            "walking interaction suspended",
            PlayerInteractor.Instance != null &&
            PlayerInteractor.Instance.IsSuspended
        );
        CheckNoPinkMaterials("CarDriving");

        CarDriveDestination destination =
            FindAnyObjectByType<CarDriveDestination>();

        Check("store destination exists", destination != null);

        if (destination == null || controller == null)
            yield break;

        Transform car = CarDriveSession.FindDriveRoot(controller.transform);

        Check("car root found", car != null);

        if (car == null)
            yield break;

        if (carCamera != null)
        {
            Check(
                "driving uses the third-person camera",
                carCamera.ViewMode == CarCameraViewMode.ThirdPerson
            );
            Check(
                "driving uses the shared mouse sensitivity",
                carCamera.UsesPlayerMouseSensitivity
            );

            Renderer[] carRenderers =
                car.GetComponentsInChildren<Renderer>(true);

            bool bodyVisible = false;

            for (int i = 0; i < carRenderers.Length; i++)
            {
                if (carRenderers[i] != null && carRenderers[i].enabled)
                    bodyVisible = true;
            }

            Check("third person shows the car body", bodyVisible);

            // The orbit camera eases into place, so let it settle before
            // reading where it ended up.
            float settleDeadline = Time.realtimeSinceStartup + 2f;

            while (Time.realtimeSinceStartup < settleDeadline)
            {
                Vector3 offset = carCamera.transform.position - car.position;

                if (offset.y > 1f &&
                    Vector3.Dot(offset, car.forward) < 0f)
                {
                    break;
                }

                yield return null;
            }

            Vector3 cameraOffset = carCamera.transform.position - car.position;

            Check(
                "third person camera sits behind the car",
                cameraOffset.y > 1f &&
                Vector3.Dot(cameraOffset, car.forward) < 0f
            );

            carCamera.SetViewMode(CarCameraViewMode.FirstPerson);
            yield return null;

            bool bodyHidden = carRenderers.Length > 0;

            for (int i = 0; i < carRenderers.Length; i++)
            {
                if (carRenderers[i] != null && carRenderers[i].enabled)
                    bodyHidden = false;
            }

            Check("first person hides the car body", bodyHidden);

            Vector3 eyeOffset = carCamera.transform.position - car.position;

            Check(
                "first person camera sits at the driver",
                eyeOffset.magnitude < 4f && eyeOffset.y > 0.5f
            );

            carCamera.SetViewMode(CarCameraViewMode.ThirdPerson);
            yield return null;

            bodyVisible = false;

            for (int i = 0; i < carRenderers.Length; i++)
            {
                if (carRenderers[i] != null && carRenderers[i].enabled)
                    bodyVisible = true;
            }

            Check("third person restores the car body", bodyVisible);
        }

        Rigidbody body = car.GetComponent<Rigidbody>();
        Vector3 target = destination.DestinationPosition;

        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = target;
        }

        car.position = target;

        // Moving the car to the destination teleports the editor camera to a
        // part of the city whose shaders may not be compiled yet. The first
        // frame after that can take several seconds in the editor, so allow a
        // generous deadline even though the in-game load delay is under 1s.
        float deadline = Time.realtimeSinceStartup + 30f;

        while (SceneManager.GetActiveScene().name != "Store" &&
               Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        Check(
            "destination loads Store",
            SceneManager.GetActiveScene().name == "Store"
        );

        if (SceneManager.GetActiveScene().name != "Store")
        {
            Line(
                "  destination stage: " + destination.LastStage +
                " | watched car: " +
                (destination.WatchedCar == null
                    ? "none"
                    : destination.WatchedCar.position.ToString("F2")) +
                " | car: " + car.position.ToString("F2") +
                " | target: " + target.ToString("F2")
            );
        }

        yield return new WaitForSecondsRealtime(1.5f);

        Check("driving session cleared", !CarDriveSession.Active);
        Check(
            "walking interaction resumed",
            PlayerInteractor.Instance != null &&
            !PlayerInteractor.Instance.IsSuspended
        );
        Check(
            "drive_to_store completed",
            ObjectiveManager.Instance != null &&
            ObjectiveManager.Instance.IsDone("drive_to_store")
        );

        // --- Exit path: E returns to the original scene beside the car. ---
        ResetStory();

        SceneManager.LoadScene("CarDriving");
        yield return new WaitForSecondsRealtime(2.5f);

        controller = FindAnyObjectByType<CarDriveController>();

        Check(
            "second driving session active",
            CarDriveSession.Active && controller != null
        );

        if (controller == null)
            yield break;

        Check("exit request accepted", controller.ExitVehicle());

        deadline = Time.realtimeSinceStartup + 8f;

        while (SceneManager.GetActiveScene().name != "City" &&
               Time.realtimeSinceStartup < deadline)
        {
            yield return new WaitForSecondsRealtime(0.1f);
        }

        Check(
            "exit returns to City",
            SceneManager.GetActiveScene().name == "City"
        );

        yield return new WaitForSecondsRealtime(1.5f);

        Transform player = InteractionUtil.FindPlayer();
        GameObject cityCar = GameObject.Find("Free Racing Car");

        Check("player exists after exit", player != null);
        Check("city car exists after exit", cityCar != null);
        Check(
            "walking interaction resumed after exit",
            PlayerInteractor.Instance != null &&
            !PlayerInteractor.Instance.IsSuspended
        );
        CheckNoPinkMaterials("City");

        if (player != null && cityCar != null)
        {
            Vector3 offset = player.position - cityCar.transform.position;
            offset.y = 0f;

            Check(
                "player returned beside the car",
                offset.magnitude < 8f
            );
        }
    }

    /// <summary>
    /// Exercises the title screen, chapter list, shared settings panel, pause
    /// menu, loading screen and chapter introduction. It temporarily moves the
    /// user's save/settings files aside and restores them at the end.
    /// </summary>
    private IEnumerator MenuTest()
    {
        SavedFile settingsBackup = new SavedFile(SaveIO.SettingsPath);
        SavedFile progressBackup = new SavedFile(SaveIO.ProgressPath);

        GameSession.CleanupForMenu();
        GameProgress.Clear();
        GameProgress.Reload();

        SceneManager.LoadScene("MainMenu");
        yield return new WaitForSecondsRealtime(1.5f);

        Section("MAIN MENU");

        Check(
            "active scene is MainMenu",
            SceneManager.GetActiveScene().name == "MainMenu"
        );

        MainMenuController menu = FindAnyObjectByType<MainMenuController>();

        Check("main menu controller exists", menu != null);

        if (menu == null)
        {
            settingsBackup.Restore();
            progressBackup.Restore();
            GameProgress.Reload();
            GameSettings.Reload();
            yield break;
        }

        // The Canvas is authored by hand, so the title is optional: the menu
        // is allowed to ship without one. When a title is assigned it must
        // still show the expected text and keep the protected placeholder
        // font.
        bool titleAssigned = menu.TitleText != null;

        Check(
            "main menu shows the title",
            !titleAssigned || menu.TitleText.text == "PARALYSIS"
        );

        Font placeholder = Resources.Load<Font>("Fonts/PlaceholderTitle");

        Check(
            "menu title uses the placeholder font",
            !titleAssigned ||
            (placeholder != null && menu.TitleText.font == placeholder)
        );
        Check(
            "menu title is protected from the electronic font",
            !titleAssigned ||
            menu.TitleText.GetComponent<GameFontIgnore>() != null
        );

        Transform menuContent = FindTransform("MenuContent");
        MenuButton[] menuButtons = menuContent != null
            ? menuContent.GetComponentsInChildren<MenuButton>(true)
            : new MenuButton[0];

        Check("main menu has five buttons", menuButtons.Length == 5);

        string[] expectedLabels =
        {
            "NEW GAME",
            "CONTINUE",
            "CHAPTERS",
            "SETTINGS",
            "QUIT GAME"
        };

        bool labelsOk = true;
        bool menuFontsOk = true;
        bool hitboxesOk = true;

        for (int i = 0; i < expectedLabels.Length; i++)
        {
            MenuButton button = FindMenuButton(menuContent, expectedLabels[i]);

            if (button == null || button.Label == null)
            {
                labelsOk = false;
                continue;
            }

            if (button.Label.font != GameFont.Default)
                menuFontsOk = false;

            if (button.Hitbox == null || button.Hitbox.color.a > 0.001f)
                hitboxesOk = false;
        }

        Check("main menu button labels are correct", labelsOk);
        Check("main menu buttons use the electronic font", menuFontsOk);
        Check("main menu buttons have invisible hitboxes", hitboxesOk);

        Check(
            "continue is disabled without progress",
            menu.ContinueButton != null &&
            !menu.ContinueButton.Interactable
        );
        Check(
            "continue explains the missing save",
            menu.ContinueButton != null &&
            menu.ContinueButton.SecondaryLabel != null &&
            menu.ContinueButton.SecondaryLabel.gameObject.activeSelf &&
            menu.ContinueButton.SecondaryLabel.text == "NO SAVE FOUND"
        );

        MenuButton newGame = FindMenuButton(menuContent, "NEW GAME");

        Check("new game button exists", newGame != null);

        if (newGame != null && newGame.Label != null)
        {
            int normalSize = newGame.Label.fontSize;

            Check(
                "main menu keeps the authored label size",
                newGame.AuthoredFontSize > 0 &&
                normalSize == newGame.AuthoredFontSize
            );

            newGame.OnPointerEnter(null);
            yield return new WaitForSecondsRealtime(0.45f);

            Check(
                "menu hover enlarges, brightens and bolds",
                newGame.Label.fontSize > normalSize &&
                newGame.Label.color.r > 0.9f &&
                newGame.Label.fontStyle == FontStyle.Bold
            );

            newGame.OnPointerExit(null);
            yield return new WaitForSecondsRealtime(0.45f);

            Check(
                "menu hover returns smoothly",
                Mathf.Abs(newGame.Label.fontSize - normalSize) <= 1
            );
        }

        menu.ChaptersButton.Activate();
        yield return null;

        ChapterSelectionController chapters = menu.ChapterSelection;

        Check("chapter screen opens", chapters != null && chapters.IsOpen);

        ChapterDatabase database = ChapterDatabase.Load();
        int expectedChapters = database != null
            ? database.GetOrderedChapters().Count
            : 0;

        Check(
            "chapter screen lists configured chapters",
            chapters != null &&
            expectedChapters >= 3 &&
            chapters.ChapterCount == expectedChapters
        );

        bool chapterFontsOk = true;
        bool chapterAvailable = true;
        bool chapterLabelsOk = true;
        bool chapterSizesOk = true;

        if (chapters != null && database != null)
        {
            List<ChapterDefinition> ordered =
                database.GetOrderedChapters();

            for (int i = 0; i < chapters.ChapterButtons.Count; i++)
            {
                MenuButton button = chapters.ChapterButtons[i];

                if (button == null || button.Label == null)
                {
                    chapterFontsOk = false;
                    chapterAvailable = false;
                    continue;
                }

                if (button.Label.font != GameFont.Default)
                    chapterFontsOk = false;

                if (button.AuthoredFontSize <= 0 ||
                    button.Label.fontSize != button.AuthoredFontSize)
                {
                    chapterSizesOk = false;
                }

                if (!button.Interactable)
                    chapterAvailable = false;

                if (i < ordered.Count &&
                    button.Label.text.IndexOf(
                        ordered[i].chapterTitle,
                        StringComparison.OrdinalIgnoreCase) < 0)
                {
                    chapterLabelsOk = false;
                }
            }
        }

        Check("chapter list uses the electronic font", chapterFontsOk);
        Check("chapter buttons keep the authored label size", chapterSizesOk);
        Check(
            "development mode unlocks every chapter",
            chapterAvailable
        );
        Check("chapter labels use configured titles", chapterLabelsOk);

        Transform chapterHost = FindTransform("ChapterSelectionHost");
        MenuButton chapterBack = FindMenuButton(chapterHost, "BACK");

        Check("chapter back button exists", chapterBack != null);

        if (chapterBack != null)
            chapterBack.Activate();

        yield return null;

        Check(
            "chapter back returns to the main menu",
            chapters != null && !chapters.IsOpen
        );

        menu.SettingsButton.Activate();
        yield return null;

        SettingsPanel settings = menu.Settings;
        Slider slider = settings != null
            ? settings.SensitivitySlider
            : null;
        SettingsConfig settingsConfig = GameSettings.Config;

        Check(
            "settings screen opens",
            settings != null && settings.IsOpen
        );
        Check(
            "settings reuses the shared panel",
            settings != null &&
            settings.IsBuilt &&
            slider != null
        );
        Check(
            "sensitivity slider uses the configured range",
            slider != null &&
            settingsConfig != null &&
            Mathf.Approximately(
                slider.minValue,
                Mathf.Min(
                    settingsConfig.minMouseSensitivity,
                    settingsConfig.maxMouseSensitivity)) &&
            Mathf.Approximately(
                slider.maxValue,
                Mathf.Max(
                    settingsConfig.minMouseSensitivity,
                    settingsConfig.maxMouseSensitivity))
        );
        Check(
            "sensitivity slider starts at the saved value",
            slider != null &&
            Mathf.Approximately(slider.value, GameSettings.MouseSensitivity)
        );

        Transform settingsHost = FindTransform("SettingsHost");
        MenuButton fps60 = FindMenuButton(settingsHost, "60 FPS");
        MenuButton fps120 = FindMenuButton(settingsHost, "120 FPS");
        MenuButton fps240 = FindMenuButton(settingsHost, "240 FPS");

        Check(
            "frame rate options are 60/120/240",
            fps60 != null && fps120 != null && fps240 != null
        );
        Check(
            "current frame rate is selected",
            (GameSettings.FrameRate == 60 && fps60 != null && fps60.Selected) ||
            (GameSettings.FrameRate == 120 && fps120 != null && fps120.Selected) ||
            (GameSettings.FrameRate == 240 && fps240 != null && fps240.Selected)
        );

        float testSensitivity = GameSettings.MouseSensitivity;

        if (slider != null)
        {
            testSensitivity = Mathf.Approximately(
                GameSettings.MouseSensitivity,
                7.5f)
                ? 4.5f
                : 7.5f;

            slider.value = Mathf.Clamp(
                testSensitivity,
                slider.minValue,
                slider.maxValue
            );

            testSensitivity = slider.value;

            yield return null;

            Check(
                "sensitivity applies immediately",
                Mathf.Approximately(
                    GameSettings.MouseSensitivity,
                    testSensitivity)
            );
        }

        if (fps120 != null)
        {
            fps120.Activate();
            yield return null;

            Check(
                "frame rate applies immediately",
                GameSettings.FrameRate == 120 &&
                Application.targetFrameRate == 120
            );
            Check("frame rate selection is visible", fps120.Selected);
        }

        MenuButton settingsBack = FindMenuButton(settingsHost, "BACK");

        Check("settings back button exists", settingsBack != null);

        if (settingsBack != null)
            settingsBack.Activate();

        yield return null;

        Check(
            "settings back returns to the main menu",
            settings != null && !settings.IsOpen
        );

        GameSettings.Reload();

        Check(
            "settings are persisted separately",
            slider != null &&
            fps120 != null &&
            Mathf.Approximately(
                GameSettings.MouseSensitivity,
                testSensitivity) &&
            GameSettings.FrameRate == 120
        );

        ChapterDefinition firstChapter = database != null
            ? database.FirstChapter
            : null;

        Check(
            "chapter database has a first chapter",
            firstChapter != null
        );

        if (firstChapter != null)
        {
            GameProgress.BeginNewGame(firstChapter);
            menu.ShowMenu();
            yield return null;

            Check(
                "continue becomes available with progress",
                menu.ContinueButton.Interactable
            );
            Check(
                "continue hides the no-save hint",
                menu.ContinueButton.SecondaryLabel == null ||
                !menu.ContinueButton.SecondaryLabel.gameObject.activeSelf
            );
        }

        GameProgress.Clear();
        menu.ShowMenu();
        yield return null;

        Check(
            "continue is disabled after progress is cleared",
            !menu.ContinueButton.Interactable
        );

        // Put the player's real files back before the pause/transition tests.
        progressBackup.Restore();
        settingsBackup.Restore();
        GameProgress.Reload();
        GameSettings.Reload();

        Section("PAUSE");

        SceneManager.LoadScene("Home");
        yield return new WaitForSecondsRealtime(2f);

        yield return WaitForDialogue(8f);
        yield return ClickThroughDialogue(20f);

        PlayerControl.ForceUnlockAll();
        UIState.ClearModals();

        PauseManager pause = PauseManager.Ensure();

        Check("pause manager exists", pause != null);

        // Batch mode does not honour Cursor.lockState, so remember whatever
        // state gameplay actually had and verify that pause restores it. In a
        // normal GUI session this is Locked + hidden.
        CursorLockMode expectedCursorLock = Cursor.lockState;
        bool expectedCursorVisible = Cursor.visible;

        bool opened = pause != null && pause.Open();

        Check("pause opens", opened && pause.IsOpen);

        GameObject pauseCanvas = GameObject.Find("PauseCanvas");
        Image leftImage = pauseCanvas != null
            ? FindImage(pauseCanvas.transform, "PauseLeftPanel")
            : null;
        Image rightImage = pauseCanvas != null
            ? FindImage(pauseCanvas.transform, "PauseRightDarkening")
            : null;

        Check(
            "pause left panel is black",
            leftImage != null &&
            leftImage.color.r < 0.01f &&
            leftImage.color.g < 0.01f &&
            leftImage.color.b < 0.01f
        );
        Check(
            "pause animates while time is frozen",
            Mathf.Approximately(Time.timeScale, 0f) &&
            leftImage != null &&
            leftImage.color.a < 0.1f
        );

        yield return new WaitForSecondsRealtime(0.6f);

        Check(
            "pause opening animation completes",
            leftImage != null && leftImage.color.a > 0.99f
        );
        Check(
            "pause right side is darkened",
            rightImage != null &&
            rightImage.color.a > 0.3f &&
            rightImage.color.a < 0.9f
        );
        Check(
            "pause freezes time",
            Mathf.Approximately(Time.timeScale, 0f)
        );
        Check(
            "pause unlocks the cursor",
            Cursor.lockState == CursorLockMode.None && Cursor.visible
        );
        Check(
            "pause blocks gameplay input",
            UIState.BlocksGameplay && PlayerControl.IsLocked
        );

        FirstPersonController fpc = InteractionUtil.FindController();

        Check(
            "pause locks walking and look",
            fpc != null && !fpc.playerCanMove && !fpc.cameraCanMove
        );
        Check(
            "pause does not duplicate managers",
            FindObjectsByType<PauseManager>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None).Length == 1
        );

        Transform pauseButtons = FindTransform("PauseButtons");
        MenuButton pauseContinue = FindMenuButton(pauseButtons, "CONTINUE");
        MenuButton pauseSettings = FindMenuButton(pauseButtons, "SETTINGS");
        MenuButton pauseReplay = FindMenuButton(
            pauseButtons,
            "REPLAY CHAPTER"
        );
        MenuButton pauseMainMenu = FindMenuButton(
            pauseButtons,
            "MAIN MENU"
        );

        Check(
            "pause shows its four actions",
            pauseContinue != null &&
            pauseSettings != null &&
            pauseReplay != null &&
            pauseMainMenu != null
        );

        bool pauseHitboxesOk = pauseButtons != null;
        MenuButton[] pauseButtonList = pauseButtons != null
            ? pauseButtons.GetComponentsInChildren<MenuButton>(true)
            : new MenuButton[0];

        for (int i = 0; i < pauseButtonList.Length; i++)
        {
            if (pauseButtonList[i].Hitbox == null ||
                pauseButtonList[i].Hitbox.color.a > 0.001f)
            {
                pauseHitboxesOk = false;
            }
        }

        Check("pause buttons have no rectangles", pauseHitboxesOk);

        pause.OpenSettings();
        yield return null;

        Check("pause settings opens", pause.IsSettingsOpen);

        Transform pauseContent = FindTransform("PauseContent");
        Transform pauseSettingsHost = FindTransform("PauseSettings");
        SettingsPanel pausePanel = pauseSettingsHost != null
            ? pauseSettingsHost.GetComponent<SettingsPanel>()
            : null;

        Check(
            "pause buttons hide in settings",
            pauseContent != null && !pauseContent.gameObject.activeSelf
        );
        Check(
            "pause settings reuses the shared panel",
            pausePanel != null &&
            pausePanel.IsBuilt &&
            pausePanel.IsOpen &&
            pausePanel.SensitivitySlider != null
        );
        Check(
            "pause stays frozen in settings",
            Mathf.Approximately(Time.timeScale, 0f) && pause.IsOpen
        );

        pause.HideSettings();
        yield return null;

        Check(
            "leaving settings does not resume",
            pause.IsOpen && Mathf.Approximately(Time.timeScale, 0f)
        );
        Check(
            "pause actions return after settings",
            pauseContent != null && pauseContent.gameObject.activeSelf
        );

        pause.Resume();

        float resumeDeadline = Time.realtimeSinceStartup + 5f;

        while (pause.IsOpen &&
               Time.realtimeSinceStartup < resumeDeadline)
        {
            yield return null;
        }

        Check("continue resumes gameplay", !pause.IsOpen);
        Check(
            "continue restores time scale",
            Mathf.Approximately(Time.timeScale, 1f)
        );
        Check(
            "continue restores the cursor",
            Cursor.lockState == expectedCursorLock &&
            Cursor.visible == expectedCursorVisible
        );
        Check(
            "continue unlocks walking and look",
            !PlayerControl.IsLocked &&
            fpc != null &&
            fpc.playerCanMove &&
            fpc.cameraCanMove
        );
        Check(
            "pause canvas is hidden after continue",
            GameObject.Find("PauseCanvas") == null
        );

        Section("TRANSITION");

        bool reopened = pause.Open();
        yield return new WaitForSecondsRealtime(0.5f);

        Check("pause reopens for the transition", reopened && pause.IsOpen);

        bool menuStarted = SceneFlow.GoToMainMenu();

        Check(
            "main menu transition starts",
            menuStarted && SceneFlow.IsTransitioning
        );
        Check("pause closes for the transition", !pause.IsOpen);

        SceneTransitionManager transition = SceneTransitionManager.Instance;
        GameObject transitionCanvas = GameObject.Find(
            "SceneTransitionCanvas"
        );
        Text loadingText = transitionCanvas != null
            ? FindText(transitionCanvas.transform, "LoadingText")
            : null;

        Check(
            "loading screen is visible",
            transition != null &&
            transition.IsBusy &&
            loadingText != null &&
            loadingText.gameObject.activeInHierarchy &&
            loadingText.text.StartsWith("LOADING")
        );

        yield return new WaitForSecondsRealtime(0.2f);

        Check(
            "loading label animates dots",
            loadingText != null &&
            loadingText.text.Length > "LOADING".Length
        );

        float menuDeadline = Time.realtimeSinceStartup + 20f;

        while (SceneFlow.IsTransitioning &&
               Time.realtimeSinceStartup < menuDeadline)
        {
            yield return null;
        }

        Check("main menu transition completes", !SceneFlow.IsTransitioning);
        Check(
            "main menu is active after transition",
            SceneManager.GetActiveScene().name == "MainMenu"
        );
        Check(
            "transition manager is idle",
            SceneTransitionManager.Instance != null &&
            !SceneTransitionManager.Instance.IsBusy
        );
        Check(
            "transition overlay is hidden",
            GameObject.Find("TransitionBlack") == null
        );
        Check(
            "time scale is restored after transition",
            Mathf.Approximately(Time.timeScale, 1f)
        );
        Check(
            "menu cursor is visible",
            Cursor.lockState == CursorLockMode.None && Cursor.visible
        );

        yield return null;

        Check(
            "story managers are cleaned for the menu",
            StoryDirector.Instance == null &&
            ObjectiveManager.Instance == null &&
            ShoppingCart.Instance == null
        );
        Check(
            "pause manager is not duplicated",
            FindObjectsByType<PauseManager>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None).Length == 1
        );
        Check(
            "transition manager is not duplicated",
            FindObjectsByType<SceneTransitionManager>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None).Length == 1
        );

        Section("CHAPTER INTRO");

        Canvas introCanvas = UIHelper.CreateCanvas(
            "IntroTestCanvas",
            700,
            false
        );

        GameObject introHost = new GameObject("ChapterIntroTestHost");
        introHost.transform.SetParent(introCanvas.transform, false);

        ChapterIntroScreen intro =
            introHost.AddComponent<ChapterIntroScreen>();
        intro.Build(introCanvas.transform);

        ChapterDefinition testChapter = new ChapterDefinition
        {
            chapterId = "smoke_test",
            chapterNumber = "7",
            chapterTitle = "TEST CHAPTER",
            subtitle = "A TEST SUBTITLE",
            sceneName = "Home"
        };

        ChapterIntroStyle testStyle = new ChapterIntroStyle
        {
            fadeInDuration = 0.05f,
            displayDuration = 0.05f,
            fadeOutDuration = 0.05f,
            useUnscaledTime = true
        };

        StartCoroutine(intro.Play(testChapter, testStyle));

        Check("chapter intro starts", intro.IsPlaying);

        Text introNumber = FindText(
            introCanvas.transform,
            "ChapterNumber"
        );
        Text introTitle = FindText(introCanvas.transform, "ChapterTitle");
        Text introSubtitle = FindText(
            introCanvas.transform,
            "ChapterSubtitle"
        );
        Image introBackground = FindImage(
            introCanvas.transform,
            "ChapterIntro"
        );

        Check(
            "chapter intro shows the configured number",
            introNumber != null && introNumber.text == "CHAPTER 7"
        );
        Check(
            "chapter intro shows the configured title",
            introTitle != null && introTitle.text == "TEST CHAPTER"
        );
        Check(
            "chapter intro shows the optional subtitle",
            introSubtitle != null &&
            introSubtitle.text == "A TEST SUBTITLE" &&
            introSubtitle.gameObject.activeSelf
        );
        Check(
            "chapter intro background is black",
            introBackground != null &&
            introBackground.color.r < 0.01f &&
            introBackground.color.g < 0.01f &&
            introBackground.color.b < 0.01f &&
            introBackground.color.a > 0.99f
        );
        Check(
            "chapter intro uses the shared font",
            introTitle != null && introTitle.font == GameFont.Default
        );

        float introDeadline = Time.realtimeSinceStartup + 5f;

        while (intro.IsPlaying &&
               Time.realtimeSinceStartup < introDeadline)
        {
            yield return null;
        }

        Check("chapter intro finishes", !intro.IsPlaying);

        Destroy(introCanvas.gameObject);
        yield return null;

        Section("LOADING INDICATOR");

        Canvas loadingCanvas = UIHelper.CreateCanvas(
            "LoadingTestCanvas",
            710,
            false
        );

        RectTransform loadingRect = UIHelper.CreateRect(
            "LoadingTestLabel",
            loadingCanvas.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(800f, 120f)
        );

        Text loadingLabel = UIHelper.AddText(
            loadingRect,
            new TextStyle
            {
                fontSize = 40,
                alignment = TextAnchor.MiddleCenter
            },
            "LOADING"
        );

        LoadingIndicator indicator =
            loadingLabel.gameObject.AddComponent<LoadingIndicator>();

        indicator.Configure(loadingLabel, "LOADING", true, 0.05f);

        string firstLoading = loadingLabel.text;

        yield return new WaitForSecondsRealtime(0.25f);

        Check(
            "loading indicator animates dots",
            loadingLabel.text != firstLoading &&
            loadingLabel.text.StartsWith("LOADING")
        );

        Destroy(loadingCanvas.gameObject);
        yield return null;

        Section("NEW GAME TRANSITION");

        ChapterDatabase newGameDatabase = ChapterDatabase.Load();
        ChapterDefinition first = newGameDatabase != null
            ? newGameDatabase.FirstChapter
            : null;

        Check(
            "first chapter is configured",
            first != null && !string.IsNullOrEmpty(first.sceneName)
        );

        if (first != null)
        {
            bool startedNewGame = SceneFlow.NewGame(first, true);

            Check(
                "new game transition starts",
                startedNewGame && SceneFlow.IsTransitioning
            );

            GameObject newGameCanvas = GameObject.Find(
                "SceneTransitionCanvas"
            );
            Text initialLoading = newGameCanvas != null
                ? FindText(newGameCanvas.transform, "LoadingText")
                : null;

            bool sawLoading =
                initialLoading != null &&
                initialLoading.gameObject.activeInHierarchy &&
                initialLoading.text.StartsWith("LOADING");
            bool sawIntro = false;
            float newGameDeadline = Time.realtimeSinceStartup + 30f;

            while (SceneFlow.IsTransitioning &&
                   Time.realtimeSinceStartup < newGameDeadline)
            {
                if (!sawLoading)
                {
                    GameObject canvasObject = GameObject.Find(
                        "SceneTransitionCanvas"
                    );
                    Text label = canvasObject != null
                        ? FindText(canvasObject.transform, "LoadingText")
                        : null;

                    if (label != null &&
                        label.gameObject.activeInHierarchy &&
                        label.text.StartsWith("LOADING"))
                    {
                        sawLoading = true;
                    }
                }

                if (!sawIntro)
                {
                    ChapterIntroScreen introScreen =
                        FindAnyObjectByType<ChapterIntroScreen>(
                            FindObjectsInactive.Include
                        );

                    if (introScreen != null && introScreen.IsPlaying)
                        sawIntro = true;
                }

                yield return null;
            }

            Check("loading screen is used for a new game", sawLoading);
            Check("chapter intro is used for a new game", sawIntro);
            Check(
                "new game transition completes",
                !SceneFlow.IsTransitioning
            );
            Check(
                "new game opens the first chapter",
                SceneManager.GetActiveScene().name == first.sceneName
            );
            Check(
                "transition overlay is hidden after new game",
                GameObject.Find("TransitionBlack") == null
            );
            Check(
                "time scale is restored after new game",
                Mathf.Approximately(Time.timeScale, 1f)
            );
        }

        progressBackup.Restore();
        settingsBackup.Restore();
        GameProgress.Reload();
        GameSettings.Reload();
        GameSession.CleanupForMenu();

        yield return null;
    }

    private IEnumerator MusicTest()
    {
        Section("MUSIC");

        MusicManager manager = MusicManager.Instance;

        Check("music manager auto-bootstraps", manager != null);

        if (manager == null)
            yield break;

        MusicDatabase database = manager.Database;

        Check("music database loads", database != null);

        if (database == null)
            yield break;

        bool allCues = true;

        for (int i = 0; i < MusicCueIds.All.Length; i++)
        {
            if (!database.HasCue(MusicCueIds.All[i]))
                allCues = false;
        }

        Check("music database has every story cue", allCues);

        MusicCue menuCueData = database.FindCue(MusicCueIds.Menu);
        MusicCue storeCueData = database.FindCue(MusicCueIds.Store);
        MusicCue arrivalCueData = database.FindCue(MusicCueIds.CarArrival);
        MusicCue endCueData = database.FindCue(MusicCueIds.End);

        Check(
            "background themes loop until another cue",
            menuCueData != null && menuCueData.loop
        );
        Check(
            "grocery store theme is a timed one-shot",
            storeCueData != null && storeCueData.IsOneShot
        );
        Check(
            "car arrival theme is a timed one-shot",
            arrivalCueData != null && arrivalCueData.IsOneShot
        );
        Check(
            "ending theme is a timed one-shot",
            endCueData != null && endCueData.IsOneShot
        );

        Check(
            "menu scene has a valid music binding",
            HasValidBinding(database, "MainMenu")
        );
        Check(
            "home scene has a valid music binding",
            HasValidBinding(database, "Home")
        );
        Check(
            "city scene has a valid music binding",
            HasValidBinding(database, "City")
        );
        Check(
            "driving scene has a valid music binding",
            HasValidBinding(database, "CarDriving")
        );
        Check(
            "store scene has a valid music binding",
            HasValidBinding(database, "Store")
        );
        Check(
            "end scene has a valid music binding",
            HasValidBinding(database, "End")
        );

        List<string> issues = database.Validate();
        bool onlyMissingClips = true;

        for (int i = 0; i < issues.Count; i++)
        {
            if (issues[i].IndexOf(
                    "has no clip assigned yet",
                    StringComparison.Ordinal) < 0)
            {
                onlyMissingClips = false;
            }
        }

        Check("music database has no structural errors", onlyMissingClips);

        Check(
            "music manager is not duplicated",
            FindObjectsByType<MusicManager>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None).Length == 1
        );

        Check(
            "unknown music cue is ignored",
            !MusicManager.PlayCue("smoke_missing_cue")
        );

        MusicManager.SetVolume(0.5f);

        Check(
            "music volume can be changed",
            Mathf.Approximately(
                manager.MasterVolume,
                database.masterVolume * 0.5f)
        );

        MusicManager.SetMuted(true);

        Check("music can be muted", manager.IsMuted);

        MusicManager.SetMuted(false);
        MusicManager.SetVolume(1f);

        Check(
            "music volume restores",
            Mathf.Approximately(manager.MasterVolume, database.masterVolume)
        );

        string homeCueId = database.FindCueIdForScene("Home");
        MusicCue homeCue = database.FindCue(homeCueId);

        if (homeCue != null && homeCue.clip != null)
        {
            Check(
                "home plays its scene cue",
                manager.CurrentCueId == homeCueId &&
                manager.IsPlaying
            );
        }
        else
        {
            Check(
                "home waits for a start clip",
                string.IsNullOrEmpty(manager.CurrentCueId) &&
                !manager.IsPlaying
            );
        }

        SceneManager.LoadScene("MainMenu");
        yield return new WaitForSecondsRealtime(1.5f);

        Check(
            "music manager survives a scene load",
            MusicManager.Instance != null &&
            MusicManager.Instance == manager
        );
        Check(
            "music manager is not duplicated after a load",
            FindObjectsByType<MusicManager>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None).Length == 1
        );
        Check(
            "menu binding is still active after the load",
            HasValidBinding(database, "MainMenu")
        );

        string menuCueId = database.FindCueIdForScene("MainMenu");
        MusicCue menuCue = database.FindCue(menuCueId);

        if (menuCue != null && menuCue.clip != null)
        {
            Check(
                "menu plays its scene cue",
                MusicManager.Instance.CurrentCueId == menuCueId &&
                MusicManager.Instance.IsPlaying
            );
        }
        else
        {
            Check(
                "menu waits for a menu clip",
                string.IsNullOrEmpty(MusicManager.Instance.CurrentCueId)
            );
        }

        MusicManager.StopMusic(0f);

        Check(
            "music stops cleanly",
            MusicManager.Instance != null &&
            !MusicManager.Instance.IsPlaying
        );

        // Exercise the actual AudioSource path with a generated tone. The
        // database asset is only changed in memory and is restored below.
        MusicCue testCue = database.FindCue(MusicCueIds.Menu);

        if (testCue != null)
        {
            AudioClip previousClip = testCue.clip;
            AudioClip testClip = CreateTestTone();

            testCue.clip = testClip;

            bool started = MusicManager.PlayCue(MusicCueIds.Menu);

            yield return new WaitForSecondsRealtime(0.2f);

            Check(
                "music cue starts a clip",
                started &&
                MusicManager.Instance.CurrentCueId == MusicCueIds.Menu &&
                MusicManager.Instance.CurrentClip == testClip
            );
            Check(
                "music clip is playing",
                MusicManager.Instance.IsPlaying
            );
            Check(
                "music volume follows the cue",
                MusicManager.Instance.CurrentVolume > 0f
            );
            Check(
                "music manager reports a looping cue",
                MusicManager.Instance.CurrentCueLoops &&
                !MusicManager.Instance.CurrentCueIsOneShot
            );

            MusicManager.StopMusic(0f);
            yield return null;

            Check(
                "music clip stops cleanly",
                !MusicManager.Instance.IsPlaying
            );

            // A one-shot cue must play once and then clear itself instead of
            // repeating for the rest of the scene.
            bool previousLoop = testCue.loop;
            testCue.loop = false;

            bool oneShotStarted = MusicManager.PlayCue(MusicCueIds.Menu);

            Check(
                "timed cue starts",
                oneShotStarted &&
                MusicManager.Instance.CurrentCueIsOneShot &&
                !MusicManager.Instance.CurrentCueLoops
            );

            yield return new WaitForSecondsRealtime(1.6f);

            Check(
                "timed cue stops on its own",
                !MusicManager.Instance.IsPlaying &&
                string.IsNullOrEmpty(MusicManager.Instance.CurrentCueId)
            );

            testCue.loop = previousLoop;
            testCue.clip = previousClip;
            Destroy(testClip);
        }
    }

    private IEnumerator EndTest()
    {
        ResetStory();

        SceneManager.LoadScene("End");
        yield return new WaitForSecondsRealtime(1.2f);

        Section("END");

        Check(
            "active scene is End",
            SceneManager.GetActiveScene().name == "End"
        );
        Check(
            "end screen unlocks the cursor",
            Cursor.lockState == CursorLockMode.None && Cursor.visible
        );

        EndScreenController controller =
            FindAnyObjectByType<EndScreenController>();

        Check("end screen controller exists", controller != null);

        if (controller == null)
            yield break;

        EndScreenView view = controller.View;

        Check("end screen has an authored view", view != null);

        if (view == null)
            yield break;

        Check("end screen uses its own canvas", view.canvas != null);
        Check(
            "end screen has a title",
            view.titleText != null &&
            !string.IsNullOrEmpty(view.titleText.text)
        );
        Check(
            "end screen has fly-in lines",
            view.lines != null && view.lines.Length >= 3
        );

        bool linesOk = view.lines != null && view.lines.Length > 0;

        if (view.lines != null)
        {
            for (int i = 0; i < view.lines.Length; i++)
            {
                if (view.lines[i] == null ||
                    string.IsNullOrEmpty(view.lines[i].text))
                {
                    linesOk = false;
                }
            }
        }

        Check("end screen lines are authored", linesOk);
        Check(
            "end screen has a back button",
            view.backButton != null &&
            view.backButton.Label != null &&
            view.backButton.Hitbox != null
        );
        Check(
            "end back button says back to main menu",
            view.backButton != null &&
            view.backButton.Label != null &&
            string.Equals(
                view.backButton.Label.text.Trim(),
                "BACK TO MAIN MENU",
                StringComparison.Ordinal
            )
        );
        Check(
            "end back button keeps the menu label size",
            view.backButton != null &&
            view.backButton.AuthoredFontSize > 0
        );

        float deadline = Time.realtimeSinceStartup + 10f;

        while (!controller.AnimationComplete &&
               Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        Check("end screen animation completes", controller.AnimationComplete);

        CanvasGroup buttonGroup = view.backButton != null
            ? view.backButton.GetComponent<CanvasGroup>()
            : null;

        Check(
            "end back button fades in",
            buttonGroup != null && buttonGroup.alpha >= 0.99f
        );

        if (view.backButton != null)
            view.backButton.Activate();

        deadline = Time.realtimeSinceStartup + 15f;

        while (SceneManager.GetActiveScene().name != "MainMenu" &&
               Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        Check(
            "end back button returns to the main menu",
            SceneManager.GetActiveScene().name == "MainMenu"
        );

        yield return new WaitForSecondsRealtime(0.5f);
    }

    private static AudioClip CreateTestTone()
    {
        const int sampleRate = 44100;
        int samples = sampleRate;

        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            data[i] = Mathf.Sin(
                2f * Mathf.PI * 440f * i / sampleRate
            ) * 0.05f;
        }

        AudioClip clip = AudioClip.Create(
            "MusicSmokeTestTone",
            samples,
            1,
            sampleRate,
            false
        );

        clip.SetData(data, 0);
        return clip;
    }

    #endregion

    #region Helpers

    private static bool HasValidBinding(
        MusicDatabase database,
        string sceneName)
    {
        if (database == null)
            return false;

        string cueId = database.FindCueIdForScene(sceneName);

        return !string.IsNullOrEmpty(cueId) && database.HasCue(cueId);
    }

    /// <summary>
    /// Fails when a renderer uses a built-in shader, because those render
    /// pink under the Universal Render Pipeline.
    /// </summary>
    private void CheckNoPinkMaterials(string sceneLabel)
    {
        List<string> pink = new List<string>();
        Renderer[] renderers = FindObjectsByType<Renderer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            Material[] materials = renderers[i].sharedMaterials;

            for (int m = 0; m < materials.Length; m++)
            {
                Material material = materials[m];

                if (material == null || material.shader == null)
                    continue;

                if (!IsBuiltInShader(material.shader.name))
                    continue;

                pink.Add(
                    renderers[i].name + " -> " + material.shader.name
                );
            }
        }

        Check(
            sceneLabel + " has no pink materials",
            pink.Count == 0
        );

        for (int i = 0; i < pink.Count && i < 12; i++)
            Line("  pink: " + pink[i]);
    }

    /// <summary>
    /// True for the built-in shaders that do not work under URP.
    /// </summary>
    private static bool IsBuiltInShader(string shaderName)
    {
        if (string.IsNullOrEmpty(shaderName))
            return false;

        if (shaderName.StartsWith("Standard"))
            return true;

        if (shaderName.StartsWith("Legacy Shaders/"))
            return true;

        if (shaderName == "GUI/Text Shader")
            return true;

        if (shaderName == "Diffuse")
            return true;

        if (shaderName.StartsWith("Unlit/Color") ||
            shaderName.StartsWith("Unlit/Texture"))
        {
            return true;
        }

        if (shaderName.StartsWith("Particles/"))
            return true;

        return false;
    }

    private void ResetStory()
    {
        // Start every section from a clean session, exactly like returning to
        // the main menu and choosing a chapter. This destroys the persistent
        // story managers from the previous section so the next scene builds
        // its own LightsObjective/ShoppingCart instead of keeping destroyed
        // references from a different scene.
        GameSession.CleanupForMenu();

        if (DialogueSystem.Instance != null)
            DialogueSystem.Instance.StopImmediately();

        ToastUI.HideCurrent();
        UIState.ClearModals();
        PlayerControl.ForceUnlockAll();
    }

    /// <summary>
    /// Waits until an objective exists and is not complete. Any dialogue that
    /// appears in the meantime is clicked through, because story routines
    /// often activate the next objective after a conversation.
    /// </summary>
    private IEnumerator WaitForObjective(string id, float timeout)
    {
        float deadline = Time.realtimeSinceStartup + timeout;

        while (Time.realtimeSinceStartup < deadline)
        {
            ObjectiveManager objectives = ObjectiveManager.Instance;

            if (objectives != null && objectives.HasActive(id))
                yield break;

            DialogueSystem dialogue = DialogueSystem.Instance;

            if (dialogue != null && dialogue.IsBusy)
                dialogue.Click();

            yield return new WaitForSecondsRealtime(0.1f);
        }
    }

    private IEnumerator WaitForDialogue(float timeout)
    {
        float deadline = Time.realtimeSinceStartup + timeout;

        while (Time.realtimeSinceStartup < deadline)
        {
            DialogueSystem dialogue = DialogueSystem.Instance;

            if (dialogue != null && dialogue.IsBusy)
                yield break;

            yield return new WaitForSecondsRealtime(0.1f);
        }
    }

    private IEnumerator ClickThroughDialogue(float timeout)
    {
        float deadline = Time.realtimeSinceStartup + timeout;

        while (Time.realtimeSinceStartup < deadline)
        {
            DialogueSystem dialogue = DialogueSystem.Instance;

            if (dialogue == null || !dialogue.IsBusy)
                yield break;

            dialogue.Click();
            yield return new WaitForSecondsRealtime(0.12f);
        }

        Line("WARNING: dialogue did not finish within " + timeout + "s");
    }

    private void Section(string name)
    {
        Line(string.Empty);
        Line("--- " + name + " ---");
    }

    private void Check(string label, bool condition)
    {
        if (condition)
        {
            passed++;
            Line("PASS  " + label);
            return;
        }

        failed++;
        Line("FAIL  " + label);
    }

    private void Line(string text)
    {
        report.AppendLine(text);
        Debug.Log("SMOKE: " + text);
    }

    private static Transform FindTransform(string name)
    {
        Transform[] transforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name == name)
                return transforms[i];
        }

        return null;
    }

    private static MenuButton FindMenuButton(Transform root, string label)
    {
        MenuButton[] buttons = root != null
            ? root.GetComponentsInChildren<MenuButton>(true)
            : FindObjectsByType<MenuButton>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < buttons.Length; i++)
        {
            MenuButton button = buttons[i];

            if (button == null || button.Label == null)
                continue;

            if (string.Equals(
                    button.Label.text.Trim(),
                    label,
                    StringComparison.OrdinalIgnoreCase))
            {
                return button;
            }
        }

        return null;
    }

    private static Text FindText(Transform root, string name)
    {
        if (root == null)
            return null;

        Text[] texts = root.GetComponentsInChildren<Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].name == name)
                return texts[i];
        }

        return null;
    }

    private static Image FindImage(Transform root, string name)
    {
        if (root == null)
            return null;

        Image[] images = root.GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null && images[i].name == name)
                return images[i];
        }

        return null;
    }

    /// <summary>
    /// Copies a save/settings file and its .bak sibling aside for the duration
    /// of the menu test, then puts the originals back. The smoke test changes
    /// real persisted values, so this keeps the user's actual save intact.
    /// </summary>
    private sealed class SavedFile
    {
        private readonly string path;
        private readonly string backupPath;
        private readonly bool existed;
        private readonly bool backupExisted;
        private readonly byte[] contents;
        private readonly byte[] backupContents;

        public SavedFile(string value)
        {
            path = value;
            backupPath = value + ".bak";
            existed = File.Exists(path);
            backupExisted = File.Exists(backupPath);
            contents = existed ? File.ReadAllBytes(path) : null;
            backupContents = backupExisted
                ? File.ReadAllBytes(backupPath)
                : null;
        }

        public void Restore()
        {
            try
            {
                RestoreOne(path, existed, contents);
                RestoreOne(backupPath, backupExisted, backupContents);
            }
            catch (Exception error)
            {
                Debug.LogWarning(
                    "SMOKE: could not restore '" + path + "': " + error
                );
            }
        }

        private static void RestoreOne(
            string target,
            bool hadFile,
            byte[] bytes)
        {
            string directory = Path.GetDirectoryName(target);

            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            if (hadFile)
            {
                File.WriteAllBytes(target, bytes);
                return;
            }

            if (File.Exists(target))
                File.Delete(target);
        }
    }

    private void Finish()
    {
        Line(string.Empty);
        Line("RESULT: " + passed + " passed, " + failed + " failed");

        string path = Path.Combine(
            Path.GetDirectoryName(Application.dataPath) ?? ".",
            "Logs",
            ResultFile
        );

        try
        {
            File.WriteAllText(path, report.ToString());
            Debug.Log("SMOKE: results written to " + path);
        }
        catch (Exception error)
        {
            Debug.LogError("SMOKE: could not write results: " + error);
        }

#if UNITY_EDITOR
        if (Application.isBatchMode)
            UnityEditor.EditorApplication.Exit(failed == 0 ? 0 : 1);
        else
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(failed == 0 ? 0 : 1);
#endif
    }

    #endregion
}
