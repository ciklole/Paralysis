using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// References to a hand-authored settings panel. SettingsPanel binds to these
/// objects instead of creating a new layout, so the panel can be edited in the
/// MainMenu scene or the PauseMenu prefab while the slider/button behaviour
/// stays exactly the same.
/// </summary>
public class SettingsPanelView : MonoBehaviour
{
    [Header("Root")]
    public RectTransform root;

    [Header("Text")]
    public Text titleText;
    public Text sensitivityLabel;
    public Text frameRateLabel;

    [Header("Controls")]
    public Slider sensitivitySlider;
    public Text sensitivityValue;
    public MenuButton[] optionButtons;
    public MenuButton backButton;

    [Tooltip(
        "When on, SettingsPanelConfig overwrites the labels saved in the " +
        "canvas. Leave off to edit the wording directly in the Canvas.")]
    public bool applyConfigText;
}
