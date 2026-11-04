using UnityEngine;

/// <summary>
/// Marks a UI text that must keep its own font. GameFont.ApplyToAll normally
/// forces the electronic highway font on every legacy Text in the scene; the
/// main menu title, loading screen and chapter titles use their own styles, so
/// they carry this marker instead.
/// </summary>
public class GameFontIgnore : MonoBehaviour
{
}
