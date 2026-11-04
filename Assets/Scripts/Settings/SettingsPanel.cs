using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reusable settings controls used by both the main menu and the pause menu.
/// The panel only presents values; GameSettings owns persistence and applies
/// the mouse sensitivity to the existing FirstPersonController.
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    private SettingsPanelConfig config;
    private SettingsConfig settingsConfig;
    private RectTransform root;
    private Slider sensitivitySlider;
    private Text sensitivityValue;
    private MenuButton backButton;
    private readonly List<MenuButton> optionButtons =
        new List<MenuButton>();
    private readonly List<int> optionValues = new List<int>();
    private Action backRequested;
    private MenuAudio audioPlayer;
    private SettingsPanelView view;
    private bool built;
    private bool hasShown;

    public bool IsBuilt => built;
    public bool IsOpen => built && root != null && root.gameObject.activeSelf;
    public Slider SensitivitySlider => sensitivitySlider;

    public void Build(
        RectTransform parent,
        SettingsPanelConfig panelConfig,
        SettingsConfig playerSettings,
        MenuAudio audio,
        Action onBack,
        SettingsPanelView authoredView = null)
    {
        if (built)
            return;

        config = panelConfig ?? new SettingsPanelConfig();
        settingsConfig = playerSettings;
        audioPlayer = audio;
        backRequested = onBack;
        view = authoredView;

        GameSettings.EnsureInitialized();

        if (view != null)
        {
            BindAuthored(parent);
            return;
        }

        root = UIHelper.CreateStretchRect(
            "SettingsPanel",
            parent,
            Vector2.zero,
            Vector2.zero
        );

        VerticalLayoutGroup layout =
            root.gameObject.AddComponent<VerticalLayoutGroup>();

        layout.padding = new RectOffset(
            Mathf.RoundToInt(config.padding.x),
            Mathf.RoundToInt(config.padding.x),
            Mathf.RoundToInt(config.padding.y),
            Mathf.RoundToInt(config.padding.y)
        );

        layout.spacing = config.rowSpacing;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        if (config.showTitle)
        {
            CreateTextRow(
                "Title",
                config.titleText,
                config.titleStyle,
                config.titleHeight
            );
        }

        CreateTextRow(
            "SensitivityLabel",
            config.sensitivityLabel,
            config.labelStyle,
            config.labelHeight
        );

        CreateSliderRow();

        CreateTextRow(
            "FrameRateLabel",
            config.frameRateLabel,
            config.labelStyle,
            config.labelHeight
        );

        CreateOptionsRow();

        RectTransform spacer = UIHelper.CreateRect(
            "Spacer",
            root,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            Vector2.zero,
            new Vector2(0f, config.backTopSpacing)
        );

        LayoutElement spacerLayout =
            spacer.gameObject.AddComponent<LayoutElement>();

        spacerLayout.minHeight = config.backTopSpacing;
        spacerLayout.flexibleHeight = 1f;
        spacerLayout.flexibleWidth = 1f;

        CreateBackButton();

        built = true;
        root.gameObject.SetActive(false);
        Refresh();
    }

    /// <summary>
    /// Binds the controls that were authored in the Canvas. No layout objects
    /// are created; only values, labels and click handlers are wired.
    /// </summary>
    private void BindAuthored(RectTransform parent)
    {
        root = view.root != null ? view.root : parent;

        sensitivitySlider = view.sensitivitySlider;
        sensitivityValue = view.sensitivityValue;
        backButton = view.backButton;

        if (view.applyConfigText)
        {
            if (view.titleText != null)
                view.titleText.text = config.titleText;

            if (view.sensitivityLabel != null)
                view.sensitivityLabel.text = config.sensitivityLabel;

            if (view.frameRateLabel != null)
                view.frameRateLabel.text = config.frameRateLabel;

            if (backButton != null)
                backButton.SetLabel(config.backLabel);
        }

        if (view.titleText != null)
            view.titleText.gameObject.SetActive(config.showTitle);

        optionButtons.Clear();
        optionValues.Clear();

        int[] options = GameSettings.FrameRateOptions;

        if (view.optionButtons != null)
        {
            int count = Mathf.Min(view.optionButtons.Length, options.Length);

            for (int i = 0; i < count; i++)
            {
                MenuButton button = view.optionButtons[i];

                if (button == null)
                    continue;

                int option = options[i];

                button.Bind(audioPlayer);

                if (view.applyConfigText)
                    button.SetLabel(string.Format(config.fpsFormat, option));

                button.Clicked += () => OnFrameRateSelected(option);

                optionButtons.Add(button);
                optionValues.Add(option);
            }
        }

        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = settingsConfig != null
                ? Mathf.Min(
                    settingsConfig.minMouseSensitivity,
                    settingsConfig.maxMouseSensitivity)
                : 0.1f;

            sensitivitySlider.maxValue = settingsConfig != null
                ? Mathf.Max(
                    settingsConfig.minMouseSensitivity,
                    settingsConfig.maxMouseSensitivity)
                : 10f;

            sensitivitySlider.wholeNumbers = false;
            sensitivitySlider.onValueChanged.AddListener(
                OnSensitivityChanged
            );
        }

        if (backButton != null)
        {
            backButton.Bind(audioPlayer);
            backButton.Clicked += OnBackClicked;
        }

        built = true;
        root.gameObject.SetActive(false);
        Refresh();
    }

    private void CreateTextRow(
        string name,
        string content,
        TextStyle style,
        float height)
    {
        RectTransform rect = UIHelper.CreateRect(
            name,
            root,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            Vector2.zero,
            new Vector2(0f, height)
        );

        LayoutElement layout =
            rect.gameObject.AddComponent<LayoutElement>();

        layout.minHeight = height;
        layout.preferredHeight = height;
        layout.flexibleWidth = 1f;

        Text text = UIHelper.AddText(rect, style, content);
        text.raycastTarget = false;
    }

    private void CreateSliderRow()
    {
        float height = Mathf.Max(
            config.sliderHeight + 8f,
            config.sliderHeight
        );

        RectTransform row = UIHelper.CreateRect(
            "SensitivityRow",
            root,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            Vector2.zero,
            new Vector2(0f, height)
        );

        LayoutElement rowLayout =
            row.gameObject.AddComponent<LayoutElement>();

        rowLayout.minHeight = height;
        rowLayout.preferredHeight = height;
        rowLayout.flexibleWidth = 1f;

        HorizontalLayoutGroup rowGroup =
            row.gameObject.AddComponent<HorizontalLayoutGroup>();

        rowGroup.spacing = 14f;
        rowGroup.childAlignment = TextAnchor.MiddleLeft;
        rowGroup.childControlWidth = true;
        rowGroup.childControlHeight = true;
        rowGroup.childForceExpandWidth = false;
        rowGroup.childForceExpandHeight = true;

        sensitivitySlider = CreateSlider(row);
        sensitivitySlider.minValue = settingsConfig != null
            ? Mathf.Min(
                settingsConfig.minMouseSensitivity,
                settingsConfig.maxMouseSensitivity)
            : 0.1f;

        sensitivitySlider.maxValue = settingsConfig != null
            ? Mathf.Max(
                settingsConfig.minMouseSensitivity,
                settingsConfig.maxMouseSensitivity)
            : 10f;

        sensitivitySlider.wholeNumbers = false;
        sensitivitySlider.onValueChanged.AddListener(
            OnSensitivityChanged
        );

        RectTransform valueRect = UIHelper.CreateRect(
            "SensitivityValue",
            row,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            Vector2.zero,
            new Vector2(config.valueWidth, height)
        );

        LayoutElement valueLayout =
            valueRect.gameObject.AddComponent<LayoutElement>();

        valueLayout.minWidth = config.valueWidth;
        valueLayout.preferredWidth = config.valueWidth;
        valueLayout.minHeight = height;
        valueLayout.preferredHeight = height;

        sensitivityValue = UIHelper.AddText(
            valueRect,
            config.valueStyle,
            "0.0"
        );

        sensitivityValue.raycastTarget = false;
    }

    private Slider CreateSlider(RectTransform parent)
    {
        GameObject sliderObject = new GameObject("SensitivitySlider");
        sliderObject.transform.SetParent(parent, false);

        RectTransform sliderRect =
            sliderObject.AddComponent<RectTransform>();

        LayoutElement layout =
            sliderObject.AddComponent<LayoutElement>();

        layout.minHeight = config.sliderHeight;
        layout.preferredHeight = config.sliderHeight;
        layout.flexibleWidth = 1f;

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;

        RectTransform background = UIHelper.CreateRect(
            "Background",
            sliderRect,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            Vector2.zero,
            new Vector2(0f, 8f)
        );

        background.anchorMin = new Vector2(0f, 0.5f);
        background.anchorMax = new Vector2(1f, 0.5f);
        background.sizeDelta = new Vector2(0f, 8f);

        Image backgroundImage = UIHelper.AddPanel(
            background,
            config.sliderBackground
        );

        backgroundImage.raycastTarget = true;

        RectTransform fillArea = UIHelper.CreateStretchRect(
            "Fill Area",
            sliderRect,
            new Vector2(10f, 0f),
            new Vector2(-10f, 0f)
        );

        fillArea.anchorMin = new Vector2(0f, 0.5f);
        fillArea.anchorMax = new Vector2(1f, 0.5f);
        fillArea.offsetMin = new Vector2(10f, -4f);
        fillArea.offsetMax = new Vector2(-10f, 4f);

        RectTransform fill = UIHelper.CreateStretchRect(
            "Fill",
            fillArea,
            Vector2.zero,
            Vector2.zero
        );

        Image fillImage = UIHelper.AddPanel(fill, config.sliderFill);
        fillImage.raycastTarget = false;

        RectTransform handleArea = UIHelper.CreateStretchRect(
            "Handle Slide Area",
            sliderRect,
            new Vector2(10f, 0f),
            new Vector2(-10f, 0f)
        );

        RectTransform handle = UIHelper.CreateRect(
            "Handle",
            handleArea,
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            Vector2.zero,
            new Vector2(18f, 0f)
        );

        Image handleImage = UIHelper.AddPanel(
            handle,
            config.sliderHandle
        );

        handleImage.raycastTarget = true;

        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;

        return slider;
    }

    private void CreateOptionsRow()
    {
        RectTransform row = UIHelper.CreateRect(
            "FrameRateOptions",
            root,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            Vector2.zero,
            new Vector2(0f, config.optionHeight)
        );

        LayoutElement rowLayout =
            row.gameObject.AddComponent<LayoutElement>();

        rowLayout.minHeight = config.optionHeight;
        rowLayout.preferredHeight = config.optionHeight;
        rowLayout.flexibleWidth = 1f;

        HorizontalLayoutGroup rowGroup =
            row.gameObject.AddComponent<HorizontalLayoutGroup>();

        rowGroup.spacing = config.optionSpacing;
        rowGroup.childAlignment = TextAnchor.MiddleLeft;
        rowGroup.childControlWidth = true;
        rowGroup.childControlHeight = true;
        rowGroup.childForceExpandWidth = false;
        rowGroup.childForceExpandHeight = false;

        int[] options = GameSettings.FrameRateOptions;

        for (int i = 0; i < options.Length; i++)
        {
            int option = options[i];

            MenuButton button = CreateTextButton(
                "FPS " + option,
                config.buttonTextStyle,
                config.optionButtonStyle,
                row,
                config.optionWidth,
                config.optionHeight,
                false
            );

            button.SetLabel(string.Format(config.fpsFormat, option));
            button.Clicked += () => OnFrameRateSelected(option);

            optionButtons.Add(button);
            optionValues.Add(option);
        }
    }

    private void CreateBackButton()
    {
        backButton = CreateTextButton(
            "Back",
            config.buttonTextStyle,
            config.backButtonStyle,
            root,
            config.backWidth,
            config.backHeight,
            true
        );

        backButton.SetLabel(config.backLabel);
        backButton.Clicked += OnBackClicked;
    }

    private MenuButton CreateTextButton(
        string name,
        TextStyle textStyle,
        MenuButtonStyle buttonStyle,
        RectTransform parent,
        float width,
        float height,
        bool flexibleWidth)
    {
        RectTransform buttonRect = UIHelper.CreateRect(
            name,
            parent,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            Vector2.zero,
            new Vector2(width, height)
        );

        LayoutElement layout =
            buttonRect.gameObject.AddComponent<LayoutElement>();

        layout.minWidth = width;
        layout.preferredWidth = width;
        layout.minHeight = height;
        layout.preferredHeight = height;
        layout.flexibleWidth = flexibleWidth ? 1f : 0f;

        Image hitbox = UIHelper.AddPanel(
            buttonRect,
            new Color(0f, 0f, 0f, 0f)
        );

        hitbox.raycastTarget = true;

        MenuButton button =
            buttonRect.gameObject.AddComponent<MenuButton>();

        RectTransform labelRect = UIHelper.CreateStretchRect(
            "Label",
            buttonRect,
            new Vector2(8f, 2f),
            new Vector2(-8f, -2f)
        );

        Text label = UIHelper.AddText(
            labelRect,
            textStyle,
            name
        );

        label.raycastTarget = false;

        button.Configure(
            label,
            null,
            hitbox,
            buttonStyle,
            audioPlayer
        );

        return button;
    }

    public void Show()
    {
        if (!built)
            return;

        hasShown = true;
        root.gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        if (!built)
            return;

        if (hasShown)
            GameSettings.Save();

        root.gameObject.SetActive(false);
    }

    public void Refresh()
    {
        if (!built)
            return;

        GameSettings.EnsureInitialized();

        if (sensitivitySlider != null)
        {
            sensitivitySlider.SetValueWithoutNotify(
                GameSettings.MouseSensitivity
            );
        }

        if (sensitivityValue != null)
        {
            sensitivityValue.text = string.Format(
                config.valueFormat,
                GameSettings.FormatSensitivity(
                    GameSettings.MouseSensitivity
                )
            );
        }

        RefreshOptions();
    }

    private void RefreshOptions()
    {
        for (int i = 0; i < optionButtons.Count; i++)
        {
            optionButtons[i].SetSelected(
                optionValues[i] == GameSettings.FrameRate
            );
        }
    }

    private void OnSensitivityChanged(float value)
    {
        GameSettings.SetMouseSensitivity(value, false);

        if (sensitivityValue != null)
        {
            sensitivityValue.text = string.Format(
                config.valueFormat,
                GameSettings.FormatSensitivity(value)
            );
        }
    }

    private void OnFrameRateSelected(int value)
    {
        GameSettings.SetFrameRate(value, true);
        RefreshOptions();
    }

    private void OnBackClicked()
    {
        GameSettings.Save();
        backRequested?.Invoke();
    }

    private void OnDisable()
    {
        if (built && hasShown)
            GameSettings.Save();
    }

    private void OnDestroy()
    {
        if (built && hasShown)
            GameSettings.Save();
    }
}
