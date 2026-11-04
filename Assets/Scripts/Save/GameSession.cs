using UnityEngine;

/// <summary>
/// Cleans up the persistent gameplay managers when the player returns to the
/// main menu. The managers recreate themselves from the StoryManagers prefab
/// when a new game loads, so the menu never runs story logic in the
/// background.
/// </summary>
public static class GameSession
{
    public static void CleanupForMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        UIState.ClearModals();
        PlayerControl.ForceUnlockAll();
        CarDriveSession.Clear();

        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.StopImmediately();
            Object.Destroy(DialogueSystem.Instance.gameObject);
        }

        if (StoryDirector.Instance != null)
        {
            StoryDirector.Instance.ResetSession(false);
            Object.Destroy(StoryDirector.Instance.gameObject);
        }

        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.ClearAll();
            Object.Destroy(ObjectiveManager.Instance.gameObject);
        }

        if (ShoppingCart.Instance != null)
        {
            ShoppingCart.Instance.ResetProgress(false);
            Object.Destroy(ShoppingCart.Instance.gameObject);
        }

        if (QuestLogUI.Instance != null)
        {
            QuestLogUI.Instance.Close();
            Object.Destroy(QuestLogUI.Instance.gameObject);
        }

        if (ToastUI.Instance != null)
        {
            ToastUI.HideCurrent();
            Object.Destroy(ToastUI.Instance.gameObject);
        }

        if (PlayerInteractor.Instance != null)
        {
            PlayerInteractor.Instance.SetSuspended(false);
            Object.Destroy(PlayerInteractor.Instance.gameObject);
        }

        if (PauseManager.Instance != null)
            PauseManager.Instance.HardResetForMenu();

        GameProgress.EndSessionForMenu();
        GameSettings.Save();
    }
}
