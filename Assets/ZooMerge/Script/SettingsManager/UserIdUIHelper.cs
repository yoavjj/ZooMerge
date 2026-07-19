using System.Collections;
using TMPro;
using UnityEngine;

public static class UserIdUIHelper
{
    private const string CACHED_USER_ID_KEY =
        "CachedUserId";

    private const string CONNECTING_TEXT =
        "Connecting...";

    /// <summary>
    /// Returns the best currently available user ID.
    /// Prefers Firebase, then falls back to the cached ID.
    /// </summary>
    public static string GetCurrentUserId()
    {
        if (!string.IsNullOrWhiteSpace(
                FirebaseInitializer.UserId))
        {
            return FirebaseInitializer.UserId;
        }

        return PlayerPrefs.GetString(
            CACHED_USER_ID_KEY,
            CONNECTING_TEXT
        );
    }

    /// <summary>
    /// Updates a TMP text with the current user ID.
    /// </summary>
    public static void RefreshText(
        TextMeshProUGUI userIdText)
    {
        if (userIdText == null)
            return;

        userIdText.text =
            $"User ID: {GetCurrentUserId()}";
    }

    /// <summary>
    /// Copies the current user ID.
    /// Returns false if no real ID is available yet.
    /// </summary>
    public static bool TryCopyToClipboard()
    {
        string userId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(userId) ||
            userId == CONNECTING_TEXT)
        {
            return false;
        }

        GUIUtility.systemCopyBuffer = userId;

        Debug.Log(
            $"[UserIdUI] Copied User ID: {userId}"
        );

        return true;
    }

    /// <summary>
    /// Temporary visual copy feedback.
    /// The MonoBehaviour calling this routine starts it.
    /// </summary>
    public static IEnumerator FlashCopiedFeedback(
        TextMeshProUGUI userIdText,
        float duration = 1.5f)
    {
        if (userIdText == null)
            yield break;

        userIdText.text =
            "<color=green>Copied to Clipboard!</color>";

        yield return new WaitForSecondsRealtime(
            duration
        );

        RefreshText(userIdText);
    }
}