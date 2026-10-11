using UnityEngine;
using UnityEngine.UI;

public class LockedObject : MonoBehaviour
{
    [Tooltip("The message to display in the top-right corner when the player touches this object")]
    [SerializeField] private string displayMessage = "Locked";

    [Tooltip("How long the message should stay visible (in seconds). Set to 0 to keep it visible until reset.")]
    [SerializeField] private float displayDuration = 3f;

    [Tooltip("Font size of the message")]
    [SerializeField] private int fontSize = 24;

    [Tooltip("Text color")]
    [SerializeField] private Color textColor = Color.white;

    private GameObject canvasObject;
    private GameObject textObject;
    private Text messageText;

    private void Start()
    {
        CreateUI();
    }

    private void CreateUI()
    {
        // Create Canvas GameObject
        canvasObject = new GameObject("LockedObjectCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Make sure it appears on top
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        // Create Text GameObject
        textObject = new GameObject("LockedMessageText");
        textObject.transform.SetParent(canvasObject.transform, false);

        messageText = textObject.AddComponent<Text>();
        messageText.text = displayMessage;
        messageText.fontSize = fontSize;
        messageText.color = textColor;
        messageText.alignment = TextAnchor.UpperRight;
        messageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Position in top-right corner
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.anchoredPosition = new Vector2(-20f, -20f);
        rectTransform.sizeDelta = new Vector2(400f, 60f);

        // Hide the text by default
        textObject.SetActive(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            DisplayLockedMessage();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            DisplayLockedMessage();
        }
    }

    private void DisplayLockedMessage()
    {
        // Show on UI
        if (messageText != null && textObject != null)
        {
            messageText.text = displayMessage;
            textObject.SetActive(true);
        }

        // Display in console/debug log
        Debug.Log(displayMessage);

        // Hide after duration if specified
        if (displayDuration > 0)
        {
            CancelInvoke(nameof(HideMessage));
            Invoke(nameof(HideMessage), displayDuration);
        }
    }

    private void HideMessage()
    {
        if (textObject != null)
        {
            textObject.SetActive(false);
        }
    }
}