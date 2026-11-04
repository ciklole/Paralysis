using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// References for the authored end-of-demo Canvas. The controller only
/// animates these objects; the scene owns the wording, fonts, colours and
/// layout, so everything can be edited in the normal Canvas editor.
/// </summary>
public class EndScreenView : MonoBehaviour
{
    public Canvas canvas;
    public Image backgroundImage;
    public Text titleText;

    [Tooltip("Lines that fly in from alternating sides, in order.")]
    public Text[] lines = new Text[0];

    public MenuButton backButton;
    public MenuAudio menuAudio;

    [Tooltip(
        "Leave off to keep the text authored in the Canvas. Turn on to let " +
        "the controller write its own defaults instead.")]
    public bool applyConfigText;
}
