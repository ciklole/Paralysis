using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One speaker name and the colour used for that name.</summary>
[Serializable]
public class DialogueSpeakerColor
{
    public string speaker;
    public Color color = Color.white;
}

/// <summary>
/// The single reusable dialogue box. Created at runtime, anchored to the
/// bottom of the screen, advanced with left click.
///
/// While it is open it owns left click and locks player movement, so nothing
/// else can react to the same input. Conversations are queued instead of
/// starting on top of each other.
/// </summary>
public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance;

    [Header("Layout")]
    [Tooltip("Panel width as a fraction of the reference width.")]
    [Range(0.4f, 1f)]
    [SerializeField] private float widthPercent = 1f;

    [SerializeField] private float maxWidth = 1920f;
    [SerializeField] private float panelHeight = 210f;
    [SerializeField] private float bottomMargin = 0f;
    [SerializeField] private float padding = 26f;
    [SerializeField] private Color panelColor = new Color(0.02f, 0.02f, 0.02f, 0.92f);

    [Header("Text")]
    [SerializeField] private TextStyle speakerStyle = new TextStyle
    {
        fontSize = 24,
        color = new Color(1f, 0.84f, 0.42f),
        alignment = TextAnchor.UpperLeft
    };

    [SerializeField] private TextStyle bodyStyle = new TextStyle
    {
        fontSize = 28,
        bold = false,
        color = new Color(0.95f, 0.95f, 0.95f),
        alignment = TextAnchor.UpperLeft
    };

    [SerializeField] private float speakerHeight = 30f;

    [Header("Speaker Colours")]
    [Tooltip("Used when the speaker is not in the list below.")]
    [SerializeField] private Color defaultSpeakerColor =
        new Color(1f, 0.84f, 0.42f);

    [Tooltip(
        "Speaker name colours. Matching is case-insensitive. A line can " +
        "override this with its own speakerColor."
    )]
    [SerializeField]
    private List<DialogueSpeakerColor> speakerColors =
        new List<DialogueSpeakerColor>
        {
            new DialogueSpeakerColor
            {
                speaker = "You",
                color = new Color(1f, 0.84f, 0.42f)
            },
            new DialogueSpeakerColor
            {
                speaker = "Player",
                color = new Color(1f, 0.84f, 0.42f)
            },
            new DialogueSpeakerColor
            {
                speaker = "Cashier",
                color = new Color(0.35f, 1f, 0.45f)
            },
            new DialogueSpeakerColor
            {
                speaker = "It",
                color = new Color(0.85f, 0.05f, 0.05f)
            },
            new DialogueSpeakerColor
            {
                speaker = "???",
                color = new Color(0.85f, 0.05f, 0.05f)
            },
            new DialogueSpeakerColor
            {
                speaker = "Thing",
                color = new Color(0.85f, 0.05f, 0.05f)
            },
            new DialogueSpeakerColor
            {
                speaker = "Monster",
                color = new Color(0.85f, 0.05f, 0.05f)
            }
        };

    [Header("Typewriter")]
    [SerializeField] private bool typewriter = true;
    [SerializeField] private float charactersPerSecond = 35f;

    [Header("Input")]
    [SerializeField] private bool leftClickAdvances = true;

    [Tooltip("Optional extra advance key. Leave as None for click-only.")]
    [SerializeField] private KeyCode advanceKey = KeyCode.None;

    [Header("Behaviour")]
    [Tooltip("Lock player movement for conversations that do not override it.")]
    [SerializeField] private bool defaultLockMovement = true;

    [Tooltip("Queue a new conversation instead of dropping it while one is open.")]
    [SerializeField] private bool queueWhenBusy = true;

    [Header("Sound")]
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private AudioSource sfxSource;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    [Header("Authored Canvas")]
    [Tooltip(
        "Optional config that points at the DialogueCanvas prefab. When " +
        "empty, the Resources asset is loaded. Without a prefab the original " +
        "runtime-built dialogue box is used.")]
    [SerializeField] private DialogueSystemConfig uiConfig;

    [Tooltip("Assigned automatically when the authored canvas is created.")]
    [SerializeField] private DialogueView view;

    private struct QueuedConversation
    {
        public DialogueLine[] lines;
        public string id;
        public bool lockMovement;
        public Action onComplete;
        public Action<int> onLineFinished;
        public Action<int> onLineStarted;
    }

    private readonly Queue<QueuedConversation> queue =
        new Queue<QueuedConversation>();

    private RectTransform panel;
    private RectTransform bodyRect;
    private Text speakerText;
    private Text bodyText;
    private float panelWidth;

    private DialogueLine[] lines;
    private string conversationId = string.Empty;
    private bool conversationLocksMovement;
    private Action onComplete;
    private Action<int> onLineFinishedCallback;
    private Action<int> onLineStartedCallback;

    private int index;
    private bool visible;

    private string fullText = string.Empty;
    private float revealed;
    private bool typing;
    private bool lineFinished;
    private float lineFinishedAt;

    private bool waitingForNextLine;
    private float nextLineAt;
    private int nextIndex;

    public bool IsVisible => visible;
    public bool IsBusy => visible || queue.Count > 0;
    public string CurrentConversationId => conversationId;
    public DialogueView View => view;

    /// <summary>Raised with the line index when a line has been fully shown.</summary>
    public event Action<int> LineFinished;

    /// <summary>
    /// Raised with the line index as soon as a line appears, before typing
    /// finishes. Scene actors use this to start their gestures in sync.
    /// </summary>
    public event Action<int> LineStarted;

    /// <summary>Raised with the conversation id when a conversation ends.</summary>
    public event Action<string> ConversationFinished;

    public static DialogueSystem Ensure()
    {
        if (Instance != null)
            return Instance;

        GameObject go = new GameObject("DialogueSystem");
        return go.AddComponent<DialogueSystem>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Destroy is deferred to the end of the frame. Deactivate first so
            // the duplicate cannot create UI, subscribe, or leave a modal/lock
            // behind during that frame.
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        UpgradeLegacyLayout();

        if (defaultSpeakerColor.a <= 0f)
            defaultSpeakerColor = new Color(1f, 0.84f, 0.42f);

        if (speakerColors == null)
            speakerColors = new List<DialogueSpeakerColor>();

        if (voiceSource == null)
            voiceSource = CreateAudioSource("DialogueVoice");

        if (sfxSource == null)
            sfxSource = CreateAudioSource("DialogueSfx");

        CreateUI();
    }

    /// <summary>
    /// Scene managers saved before the bottom-bar change still have the old
    /// 0.8-width panel values. Move those exact defaults to the new layout
    /// without touching values that have been customized.
    /// </summary>
    private void UpgradeLegacyLayout()
    {
        bool oldWidth = Mathf.Abs(widthPercent - 0.8f) < 0.001f;
        bool oldMaxWidth = Mathf.Abs(maxWidth - 1200f) < 0.1f;
        bool oldHeight = Mathf.Abs(panelHeight - 190f) < 0.1f;
        bool oldMargin = Mathf.Abs(bottomMargin - 45f) < 0.1f;

        if (!oldWidth || !oldMaxWidth || !oldHeight || !oldMargin)
            return;

        widthPercent = 1f;
        maxWidth = 1920f;
        panelHeight = 210f;
        bottomMargin = 0f;
        padding = 26f;
        panelColor = new Color(0.02f, 0.02f, 0.02f, 0.92f);
    }

    private AudioSource CreateAudioSource(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);

        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.loop = false;

        return source;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (visible)
        {
            UIState.PopModal(this);
            PlayerControl.Unlock(this);
        }
    }

    private void CreateUI()
    {
        if (TryCreateAuthoredUI())
            return;

        Canvas canvas = UIHelper.CreateCanvas("DialogueCanvas", 300, false);
        canvas.transform.SetParent(transform, false);

        panelWidth = Mathf.Min(
            maxWidth,
            UIHelper.ReferenceResolution.x * widthPercent
        );

        panel = UIHelper.CreateRect(
            "DialoguePanel",
            canvas.transform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, bottomMargin),
            new Vector2(panelWidth, panelHeight)
        );

        UIHelper.AddPanel(panel, panelColor);
        UIHelper.AddMask(panel);

        RectTransform speakerRect = UIHelper.CreateRect(
            "Speaker",
            panel,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(padding, -padding),
            new Vector2(panelWidth - padding * 2f, speakerHeight)
        );

        speakerText = UIHelper.AddText(speakerRect, speakerStyle, string.Empty);
        speakerText.alignment = TextAnchor.UpperLeft;

        bodyRect = UIHelper.CreateRect(
            "Body",
            panel,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(padding, -(padding + speakerHeight)),
            new Vector2(
                panelWidth - padding * 2f,
                panelHeight - padding * 2f - speakerHeight
            )
        );

        bodyText = UIHelper.AddText(bodyRect, bodyStyle, string.Empty);
        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.verticalOverflow = VerticalWrapMode.Truncate;

        panel.gameObject.SetActive(false);
    }

    /// <summary>
    /// Uses the editable DialogueCanvas prefab when one is configured. The
    /// prefab is a child of this persistent object, so it is cleaned up with
    /// the dialogue system.
    /// </summary>
    private bool TryCreateAuthoredUI()
    {
        UIHelper.EnsureEventSystem();

        if (view == null)
        {
            DialogueSystemConfig config = uiConfig != null
                ? uiConfig
                : DialogueSystemConfig.Load();

            if (config == null ||
                !config.useAuthoredCanvas ||
                config.canvasPrefab == null)
            {
                return false;
            }

            GameObject instance = Instantiate(
                config.canvasPrefab,
                transform,
                false
            );

            instance.name = "DialogueCanvas";
            view = instance.GetComponentInChildren<DialogueView>(true);

            if (view == null)
            {
                Destroy(instance);
                return false;
            }
        }

        if (view.panel == null ||
            view.speakerText == null ||
            view.bodyText == null ||
            view.bodyRect == null)
        {
            Debug.LogWarning(
                "DialogueSystem: the authored DialogueCanvas is missing its " +
                "panel, speaker or body references. Falling back to the " +
                "runtime dialogue box.",
                view
            );

            view = null;
            return false;
        }

        panel = view.panel;
        bodyRect = view.bodyRect;
        speakerText = view.speakerText;
        bodyText = view.bodyText;
        panelWidth = panel.rect.width;

        if (view.voiceSource != null)
            voiceSource = view.voiceSource;

        if (view.sfxSource != null)
            sfxSource = view.sfxSource;

        if (voiceSource == null)
            voiceSource = CreateAudioSource("DialogueVoice");

        if (sfxSource == null)
            sfxSource = CreateAudioSource("DialogueSfx");

        if (view.portraitImage != null)
            view.portraitImage.gameObject.SetActive(false);

        if (view.advancePrompt != null)
            view.advancePrompt.SetActive(false);

        panel.gameObject.SetActive(false);

        return true;
    }

    #region Public playback

    public bool Play(
        DialogueSource source,
        Action complete = null,
        Action<int> onLineFinished = null,
        Action<int> onLineStarted = null)
    {
        if (source == null)
        {
            complete?.Invoke();
            return false;
        }

        return Play(
            source.ResolveLines(),
            source.ResolveId(),
            source.ResolveLock(),
            complete,
            onLineFinished,
            onLineStarted
        );
    }

    public bool Play(
        DialogueLine[] dialogueLines,
        string id = "",
        bool lockMovement = true,
        Action complete = null,
        Action<int> onLineFinished = null,
        Action<int> onLineStarted = null)
    {
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            complete?.Invoke();
            return false;
        }

        if (visible)
        {
            if (!queueWhenBusy)
            {
                complete?.Invoke();
                return false;
            }

            queue.Enqueue(new QueuedConversation
            {
                lines = dialogueLines,
                id = id,
                lockMovement = lockMovement,
                onComplete = complete,
                onLineFinished = onLineFinished,
                onLineStarted = onLineStarted
            });

            return false;
        }

        StartConversation(
            dialogueLines,
            id,
            lockMovement,
            complete,
            onLineFinished,
            onLineStarted
        );

        return true;
    }

    /// <summary>Kept for existing calls. Same as Play.</summary>
    public void Show(DialogueLine[] dialogueLines)
    {
        Play(dialogueLines, string.Empty, defaultLockMovement);
    }

    /// <summary>Convenience overload: one speaker, several lines.</summary>
    public void Show(string speaker, params string[] texts)
    {
        DialogueLine[] built = new DialogueLine[texts.Length];

        for (int i = 0; i < texts.Length; i++)
        {
            built[i] = new DialogueLine
            {
                speaker = speaker,
                text = texts[i]
            };
        }

        Play(built, string.Empty, defaultLockMovement);
    }

    public void Hide()
    {
        StopImmediately();
    }

    /// <summary>Force-closes the conversation and drops anything queued.</summary>
    public void StopImmediately()
    {
        queue.Clear();

        if (!visible)
        {
            if (panel != null)
                panel.gameObject.SetActive(false);

            return;
        }

        onLineFinishedCallback = null;
        onLineStartedCallback = null;
        onComplete = null;
        ClosePanel();
    }

    /// <summary>Colour used for a speaker name, case-insensitive.</summary>
    public Color GetSpeakerColor(string speaker)
    {
        if (speakerColors != null)
        {
            for (int i = 0; i < speakerColors.Count; i++)
            {
                DialogueSpeakerColor entry = speakerColors[i];

                if (entry != null &&
                    string.Equals(
                        entry.speaker,
                        speaker,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return entry.color;
                }
            }
        }

        // Existing scene managers have no serialized palette yet. These
        // built-in colours make sure the player, cashier and transformed
        // speaker still read correctly before the list is customized.
        Color builtIn = GetBuiltInSpeakerColor(speaker);

        if (builtIn.a > 0f)
            return builtIn;

        return defaultSpeakerColor;
    }

    private static Color GetBuiltInSpeakerColor(string speaker)
    {
        if (string.IsNullOrEmpty(speaker))
            return Color.clear;

        if (string.Equals(speaker, "You", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(speaker, "Player", StringComparison.OrdinalIgnoreCase))
        {
            return new Color(1f, 0.84f, 0.42f);
        }

        if (string.Equals(speaker, "Cashier", StringComparison.OrdinalIgnoreCase))
            return new Color(0.35f, 1f, 0.45f);

        if (string.Equals(speaker, "It", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(speaker, "???", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(speaker, "Thing", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(speaker, "Monster", StringComparison.OrdinalIgnoreCase))
        {
            return new Color(0.85f, 0.05f, 0.05f);
        }

        return Color.clear;
    }

    /// <summary>
    /// Per-line override first, then the speaker palette, then the default.
    /// </summary>
    public Color ResolveSpeakerColor(DialogueLine line)
    {
        if (line != null && line.speakerColor.a > 0f)
            return line.speakerColor;

        return GetSpeakerColor(line != null ? line.speaker : string.Empty);
    }

    /// <summary>
    /// Changes a speaker's colour at runtime. Used when a character changes
    /// from normal to something else (e.g. the cashier after transforming).
    /// </summary>
    public void SetSpeakerColor(string speaker, Color color)
    {
        if (string.IsNullOrEmpty(speaker))
            return;

        if (speakerColors == null)
            speakerColors = new List<DialogueSpeakerColor>();

        for (int i = 0; i < speakerColors.Count; i++)
        {
            DialogueSpeakerColor entry = speakerColors[i];

            if (entry != null &&
                string.Equals(
                    entry.speaker,
                    speaker,
                    StringComparison.OrdinalIgnoreCase))
            {
                entry.color = color;
                return;
            }
        }

        speakerColors.Add(new DialogueSpeakerColor
        {
            speaker = speaker,
            color = color
        });
    }

    #endregion

    private void StartConversation(
        DialogueLine[] dialogueLines,
        string id,
        bool lockMovement,
        Action complete,
        Action<int> onLineFinished = null,
        Action<int> onLineStarted = null)
    {
        lines = dialogueLines;
        conversationId = id ?? string.Empty;
        conversationLocksMovement = lockMovement;
        onComplete = complete;
        onLineFinishedCallback = onLineFinished;
        onLineStartedCallback = onLineStarted;

        index = 0;
        visible = true;
        waitingForNextLine = false;

        // The mission log sits above the dialogue canvas. Close it so a
        // dialogue triggered by a scene script cannot play hidden behind it.
        if (QuestLogUI.Instance != null)
            QuestLogUI.Instance.Close();

        panel.gameObject.SetActive(true);
        UIState.PushModal(this);

        if (conversationLocksMovement)
            PlayerControl.Lock(this);

        ShowLine(0);
    }

    private void ShowLine(int lineIndex)
    {
        index = lineIndex;
        waitingForNextLine = false;

        DialogueLine line = lines[lineIndex];

        if (line == null)
        {
            Advance();
            return;
        }

        bool hasSpeaker = !string.IsNullOrEmpty(line.speaker);
        bool authored = view != null;

        if (!authored || view.applyStyles)
        {
            speakerStyle.Apply(speakerText);
            bodyStyle.Apply(bodyText);
            bodyText.verticalOverflow = VerticalWrapMode.Truncate;
        }

        speakerText.gameObject.SetActive(hasSpeaker);
        speakerText.text = hasSpeaker ? line.speaker : string.Empty;

        if (!authored || view.applyStyles || view.applySpeakerColors)
        {
            speakerText.color = ResolveSpeakerColor(line);
        }
        else if (line.speakerColor.a > 0f)
        {
            // A per-line colour is content, not chrome: it still applies when
            // the canvas owns the fonts and base colours.
            speakerText.color = line.speakerColor;
        }

        if (!authored || view.applyRuntimeLayout)
        {
            float width = panel != null && panel.rect.width > 0f
                ? panel.rect.width
                : panelWidth;
            float top = padding + (hasSpeaker ? speakerHeight : 0f);

            bodyRect.anchoredPosition = new Vector2(padding, -top);
            bodyRect.sizeDelta = new Vector2(
                width - padding * 2f,
                panelHeight - top - padding
            );
        }

        if (authored)
        {
            ApplyPortrait(line);
            SetAdvancePrompt(false);
        }

        fullText = line.text ?? string.Empty;
        revealed = 0f;
        lineFinished = false;
        lineFinishedAt = 0f;

        bool useTypewriter =
            line.UsesTypewriter(typewriter) &&
            charactersPerSecond > 0f &&
            fullText.Length > 0;

        if (useTypewriter)
        {
            typing = true;
            bodyText.text = string.Empty;
        }
        else
        {
            typing = false;
            FinishLine();
        }

        bool shouldLock = line.movementLock == MovementLockMode.Inherit
            ? conversationLocksMovement
            : line.movementLock == MovementLockMode.Lock;

        if (shouldLock)
            PlayerControl.Lock(this);
        else
            PlayerControl.Unlock(this);

        PlayClip(voiceSource, line.voiceClip, true, line.voiceVolume);
        PlayClip(sfxSource, line.soundEffect, false);

        onLineStartedCallback?.Invoke(index);
        LineStarted?.Invoke(index);
    }

    private void ApplyPortrait(DialogueLine line)
    {
        if (view == null || view.portraitImage == null)
            return;

        Sprite portrait = line != null ? line.portrait : null;
        view.portraitImage.sprite = portrait;
        view.portraitImage.enabled = portrait != null;
        view.portraitImage.gameObject.SetActive(portrait != null);
    }

    private void SetAdvancePrompt(bool shown)
    {
        if (view != null && view.advancePrompt != null)
            view.advancePrompt.SetActive(shown);
    }

    private void FinishLine()
    {
        if (lineFinished)
            return;

        lineFinished = true;
        typing = false;
        revealed = fullText.Length;
        bodyText.text = fullText;
        lineFinishedAt = Time.unscaledTime;

        SetAdvancePrompt(true);

        DialogueLine line = lines != null && index < lines.Length
            ? lines[index]
            : null;

        if (line != null && line.onLineFinished != null)
            line.onLineFinished.Invoke();

        onLineFinishedCallback?.Invoke(index);
        LineFinished?.Invoke(index);
    }

    private void PlayClip(
        AudioSource source,
        AudioClip clip,
        bool stopCurrent,
        float clipVolume = 1f)
    {
        if (source == null)
            return;

        // Stop the previous line even when this line has no clip, so a voice
        // from an earlier line can never keep talking over the next one.
        if (stopCurrent)
            source.Stop();

        if (clip == null)
            return;

        source.PlayOneShot(
            clip,
            Mathf.Clamp01(volume * Mathf.Clamp01(clipVolume))
        );
    }

    private void Update()
    {
        if (!visible)
            return;

        // A menu (pause) can be opened on top of dialogue. Do not let its
        // clicks or key presses advance the hidden conversation.
        if (UIState.Top != this)
            return;

        if (waitingForNextLine)
        {
            if (Time.unscaledTime >= nextLineAt)
                ShowLine(nextIndex);

            return;
        }

        if (typing)
        {
            revealed += charactersPerSecond * Time.unscaledDeltaTime;

            if (revealed >= fullText.Length)
            {
                FinishLine();
            }
            else
            {
                bodyText.text = fullText.Substring(
                    0,
                    Mathf.FloorToInt(revealed)
                );
            }
        }

        bool advance =
            (leftClickAdvances && Input.GetMouseButtonDown(0)) ||
            (advanceKey != KeyCode.None && Input.GetKeyDown(advanceKey));

        if (!advance)
            return;

        // First click finishes the line; that same click never advances.
        if (!lineFinished)
        {
            FinishLine();
            return;
        }

        float pause = lines[index] != null
            ? lines[index].pauseAfter
            : 0f;

        if (Time.unscaledTime < lineFinishedAt + pause)
            return;

        Advance();
    }

    /// <summary>
    /// Simulates one left click: finishes the typing line first, then advances.
    /// Used by automated play-mode checks.
    /// </summary>
    public void Click()
    {
        if (!visible || waitingForNextLine)
            return;

        if (!lineFinished)
        {
            FinishLine();
            return;
        }

        Advance();
    }

    private void Advance()
    {
        int next = index + 1;

        if (next >= lines.Length)
        {
            ClosePanel();
            return;
        }

        float delay = lines[next] != null
            ? lines[next].delayBefore
            : 0f;

        if (delay > 0f)
        {
            // The delay runs from when the previous line finished, so a sound
            // sequence (e.g. the scanner) and the next line stay in sync even
            // if the player clicks early.
            float availableAt = lineFinishedAt + delay;

            if (Time.unscaledTime < availableAt)
            {
                nextIndex = next;
                nextLineAt = availableAt;
                waitingForNextLine = true;
                SetAdvancePrompt(false);
                return;
            }
        }

        ShowLine(next);
    }

    private void ClosePanel()
    {
        visible = false;
        typing = false;
        waitingForNextLine = false;

        panel.gameObject.SetActive(false);
        SetAdvancePrompt(false);
        UIState.PopModal(this);
        PlayerControl.Unlock(this);

        Action callback = onComplete;
        string id = conversationId;

        onComplete = null;
        lines = null;
        fullText = string.Empty;
        onLineFinishedCallback = null;
        onLineStartedCallback = null;

        callback?.Invoke();
        ConversationFinished?.Invoke(id);

        // A callback may have started a new conversation already.
        if (!visible && queue.Count > 0)
        {
            QueuedConversation next = queue.Dequeue();

            StartConversation(
                next.lines,
                next.id,
                next.lockMovement,
                next.onComplete,
                next.onLineFinished,
                next.onLineStarted
            );
        }
    }

}
