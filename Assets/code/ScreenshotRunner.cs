using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Takes screenshots so the UI and scenes can be checked from a terminal.
/// The editor starts it with a request file in Temp:
///
///   Temp/paralysis_shot.request
///   Store|3|store.png
///   City|3|city.png
///
/// Special jobs:
///   MainMenu|2|main_menu.png
///   Pause|3|pause_menu.png
///   ChapterIntro|2|chapter_intro.png
///   Dialogue|4|dialogue.png
///   HudToast|3|hud_toast.png
///   QuestLog|3|quest_log.png
///
/// The screenshots end up in the Logs folder.
/// </summary>
[NoAutoStaticsCleanup]
public class ScreenshotRunner : MonoBehaviour
{
    private const string RequestFileName = "paralysis_shot.request";

    private static ScreenshotRunner runner;

    private readonly List<string[]> jobs = new List<string[]>();

    /// <summary>
    /// Domain reload is disabled in this project, so statics survive between
    /// Play Mode sessions. Clear the old runner explicitly; otherwise a stale
    /// reference can stop the next screenshot request from booting.
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

        // The editor is often unfocused while this runs from a terminal.
        Application.runInBackground = true;

        string path = Path.Combine(
            Path.GetDirectoryName(Application.dataPath) ?? ".",
            "Temp",
            RequestFileName
        );

        if (!File.Exists(path))
            return;

        string[] lines;

        try
        {
            lines = File.ReadAllLines(path);
            File.Delete(path);
        }
        catch (Exception)
        {
            return;
        }

        GameObject go = new GameObject("ScreenshotRunner");
        DontDestroyOnLoad(go);
        runner = go.AddComponent<ScreenshotRunner>();

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (line.Length == 0 || line.StartsWith("#"))
                continue;

            string[] parts = line.Split('|');

            if (parts.Length < 3)
                continue;

            runner.jobs.Add(parts);
        }

        Debug.Log("ScreenshotRunner: queued " + runner.jobs.Count + " jobs.");
    }

    private IEnumerator Start()
    {
        for (int i = 0; i < jobs.Count; i++)
            yield return RunJob(jobs[i]);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator RunJob(string[] job)
    {
        string kind = job[0];
        float wait = 3f;
        float.TryParse(job[1], out wait);

        if (string.Equals(kind, "Pause", StringComparison.OrdinalIgnoreCase))
        {
            SceneManager.LoadScene("Home");
            yield return new WaitForSecondsRealtime(wait);

            PauseManager pause = PauseManager.Ensure();

            if (pause != null && pause.Open())
                yield return new WaitForSecondsRealtime(1.2f);

            Capture(job[2]);
            yield return new WaitForSecondsRealtime(1f);

            if (pause != null && pause.IsOpen)
            {
                pause.Resume();
                yield return new WaitForSecondsRealtime(0.8f);
            }

            yield break;
        }

        if (string.Equals(
                kind,
                "ChapterIntro",
                StringComparison.OrdinalIgnoreCase))
        {
            SceneManager.LoadScene("MainMenu");
            yield return new WaitForSecondsRealtime(wait);
            yield return CaptureChapterIntro(job[2]);
            yield break;
        }

        if (string.Equals(kind, "Dialogue", StringComparison.OrdinalIgnoreCase))
        {
            SceneManager.LoadScene("Home");
            yield return new WaitForSecondsRealtime(wait);
            yield return WaitForVisibleDialogue(10f);
            yield return new WaitForSecondsRealtime(0.4f);
            Capture(job[2]);
            yield return new WaitForSecondsRealtime(1f);
            yield break;
        }

        if (string.Equals(
                kind,
                "HudToast",
                StringComparison.OrdinalIgnoreCase))
        {
            SceneManager.LoadScene("Home");
            yield return new WaitForSecondsRealtime(wait);
            yield return ClickThroughDialogue(10f);

            ObjectiveManager objectives = ObjectiveManager.Ensure();
            objectives.SetObjective(
                "screenshot_objective",
                "Collect the groceries from the shelves"
            );

            ToastUI.Show("Press M to view your shopping list", 0f);
            yield return new WaitForSecondsRealtime(0.4f);

            Capture(job[2]);
            yield return new WaitForSecondsRealtime(1f);

            ToastUI.HideCurrent();
            yield break;
        }

        if (string.Equals(
                kind,
                "QuestLog",
                StringComparison.OrdinalIgnoreCase))
        {
            SceneManager.LoadScene("Store");
            yield return new WaitForSecondsRealtime(wait);
            yield return ClickThroughDialogue(10f);

            QuestLogUI log = QuestLogUI.Ensure();
            log.Open();
            yield return new WaitForSecondsRealtime(0.5f);

            Capture(job[2]);
            yield return new WaitForSecondsRealtime(1f);

            log.Close();
            yield break;
        }

        if (string.Equals(
                kind,
                "Cashier",
                StringComparison.OrdinalIgnoreCase))
        {
            yield return RunCashierJob(job[2]);
            yield break;
        }

        // A scene name. MainMenu is included here on purpose.
        SceneManager.LoadScene(kind);
        yield return new WaitForSecondsRealtime(wait);

        Capture(job[2]);
        yield return new WaitForSecondsRealtime(2f);
    }

    private IEnumerator RunCashierJob(string fileName)
    {
        _ = fileName;

        SceneManager.LoadScene("Store");
        yield return new WaitForSecondsRealtime(3f);
        yield return ClickThroughDialogue(15f);

        ShoppingCart cart = ShoppingCart.Ensure();
        cart.TryCollect("bread", out _);
        cart.TryCollect("milk", out _);
        cart.TryCollect("eggs", out _);

        CashierInteraction cashier =
            FindAnyObjectByType<CashierInteraction>();

        if (cashier == null)
        {
            Debug.LogWarning("SCREENSHOT: no cashier in Store.");
            yield break;
        }

        cashier.EndsDemo = false;
        cashier.Interact();

        yield return WaitForVisibleDialogue(6f);
        yield return new WaitForSecondsRealtime(1.2f);
        Capture("cashier_dialogue.png");
        WriteCashierProbe("dialogue");
        yield return new WaitForSecondsRealtime(0.6f);

        float deadline = Time.realtimeSinceStartup + 45f;

        while (Time.realtimeSinceStartup < deadline)
        {
            DialogueSystem active = DialogueSystem.Instance;

            if (active == null || !active.IsBusy)
                break;

            active.Click();
            yield return new WaitForSecondsRealtime(0.12f);
        }

        yield return new WaitForSecondsRealtime(3.5f);
        Capture("cashier_transform.png");
        WriteCashierProbe("transform");

        yield return new WaitForSecondsRealtime(7f);
        Capture("cashier_approach.png");
        WriteCashierProbe("approach");

        deadline = Time.realtimeSinceStartup + 16f;

        while (Time.realtimeSinceStartup < deadline)
        {
            MonsterAnimationDriver monster =
                FindAnyObjectByType<MonsterAnimationDriver>();

            if (monster != null &&
                monster.IsReaching &&
                monster.ReachProgress > 0.72f)
            {
                break;
            }

            yield return new WaitForSecondsRealtime(0.2f);
        }

        yield return new WaitForSecondsRealtime(0.2f);
        Capture("cashier_reach.png");
        WriteCashierProbe("reach");

        deadline = Time.realtimeSinceStartup + 12f;

        while (cashier != null &&
               !cashier.CheckoutCompleted &&
               Time.realtimeSinceStartup < deadline)
        {
            yield return new WaitForSecondsRealtime(0.2f);
        }

        yield return new WaitForSecondsRealtime(2.5f);
        Capture("cashier_after.png");
        WriteCashierProbe("after");
        yield return new WaitForSecondsRealtime(1f);
    }

    private static void WriteCashierProbe(string stage)
    {
        Camera cam = InteractionUtil.FindCamera();
        MonsterApproachController approach =
            FindAnyObjectByType<MonsterApproachController>();
        MonsterAnimationDriver monster =
            FindAnyObjectByType<MonsterAnimationDriver>();
        CashierInteraction cashier =
            FindAnyObjectByType<CashierInteraction>();
        StoreLightingController lighting =
            FindAnyObjectByType<StoreLightingController>();

        System.Text.StringBuilder report = new System.Text.StringBuilder();
        report.AppendLine("stage=" + stage);
        report.AppendLine("time=" + Time.realtimeSinceStartup);
        report.AppendLine(
            "camera=" + (cam != null
                ? cam.transform.position + " rot=" + cam.transform.eulerAngles +
                  " near=" + cam.nearClipPlane
                : "null")
        );

        report.AppendLine(
            "monster=" + (monster != null
                ? monster.transform.position + " reaching=" + monster.IsReaching +
                  " progress=" + monster.ReachProgress
                : "null")
        );

        if (monster != null)
        {
            report.AppendLine(
                "bones shoulderR=" +
                monster.GetBonePosition(HumanBodyBones.RightUpperArm) +
                " elbowR=" +
                monster.GetBonePosition(HumanBodyBones.RightLowerArm) +
                " handR=" +
                monster.GetBonePosition(HumanBodyBones.RightHand)
            );
            report.AppendLine(
                "bones shoulderL=" +
                monster.GetBonePosition(HumanBodyBones.LeftUpperArm) +
                " elbowL=" +
                monster.GetBonePosition(HumanBodyBones.LeftLowerArm) +
                " handL=" +
                monster.GetBonePosition(HumanBodyBones.LeftHand)
            );
            report.AppendLine(
                "focus=" + monster.FocusPoint +
                " camForward=" + (cam != null
                    ? cam.transform.forward.ToString()
                    : "null")
            );
        }

        report.AppendLine(
            "approach=" + (approach != null
                ? approach.MonsterRoot != null
                    ? approach.MonsterRoot.position.ToString()
                    : "noRoot"
                : "null")
        );

        report.AppendLine(
            "modes normal=" + (cashier != null ? cashier.name : "null") +
            " locked=" + PlayerControl.IsLocked +
            " modal=" + UIState.BlocksGameplay +
            " checkout=" + (cashier != null && cashier.CheckoutCompleted)
        );

        report.AppendLine(
            "lightingRed=" + (lighting != null && lighting.IsRed) +
            " cursor=" + Cursor.lockState + "/" + Cursor.visible
        );

        string path = Path.Combine(
            Path.GetDirectoryName(Application.dataPath) ?? ".",
            "Logs",
            "cashier_probe.txt"
        );

        File.AppendAllText(path, report.ToString() + "---\n");
    }

    private IEnumerator WaitForVisibleDialogue(float timeout)
    {
        float deadline = Time.realtimeSinceStartup + timeout;

        while (Time.realtimeSinceStartup < deadline)
        {
            DialogueSystem dialogue = DialogueSystem.Instance;

            if (dialogue != null && dialogue.IsVisible)
                yield break;

            yield return new WaitForSecondsRealtime(0.2f);
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
            yield return new WaitForSecondsRealtime(0.2f);
        }
    }

    private IEnumerator CaptureChapterIntro(string fileName)
    {
        ChapterDatabase database = ChapterDatabase.Load();
        ChapterDefinition chapter = database != null
            ? database.FirstChapter
            : null;
        ChapterIntroStyle style = database != null
            ? database.defaultIntroStyle
            : null;

        if (chapter == null || style == null)
        {
            Debug.LogWarning(
                "SCREENSHOT: no chapter intro is configured in the " +
                "ChapterDatabase."
            );

            yield break;
        }

        Canvas canvas = UIHelper.CreateCanvas(
            "ScreenshotChapterIntroCanvas",
            600,
            false
        );
        GameObject introObject = new GameObject("ScreenshotChapterIntro");
        introObject.transform.SetParent(canvas.transform, false);

        ChapterIntroScreen intro =
            introObject.AddComponent<ChapterIntroScreen>();
        intro.Build(canvas.transform);

        Coroutine playback = StartCoroutine(intro.Play(chapter, style));

        float fadeIn = Mathf.Max(0f, style.fadeInDuration);
        float display = Mathf.Max(0f, style.displayDuration);
        float captureAt = fadeIn + Mathf.Min(display * 0.5f, 0.8f);

        yield return new WaitForSecondsRealtime(
            Mathf.Max(0.2f, captureAt)
        );

        Capture(fileName);
        yield return new WaitForSecondsRealtime(1f);

        intro.HideImmediate();

        if (playback != null)
            StopCoroutine(playback);

        Destroy(canvas.gameObject);
    }

    private static void Capture(string fileName)
    {
        string output = Path.Combine(
            Path.GetDirectoryName(Application.dataPath) ?? ".",
            "Logs",
            fileName
        );

        Debug.Log("SCREENSHOT: " + output);
        ScreenCapture.CaptureScreenshot(output);
    }
}
