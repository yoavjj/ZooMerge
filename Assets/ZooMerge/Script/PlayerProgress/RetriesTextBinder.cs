using TMPro;
using UnityEngine;

public class RetriesTextBinder : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TextMeshProUGUI text;

    [Header("Format")]
    [SerializeField] private string format = "{0}";
    [SerializeField] private string unlimitedText = "∞";

    private void Reset()
    {
        if (text == null) text = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        PlayerProgress.RetriesChanged += Refresh;
        BallEventManager.OnSessionStarted += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        PlayerProgress.RetriesChanged -= Refresh;
        BallEventManager.OnSessionStarted -= Refresh;
    }

    public void Refresh()
    {
        if (text == null) return;

        // ✅ unlimited ONLY for Galaxy 1 Level 1
        if (PlayerProgress.LastGalaxyId == 1 && PlayerProgress.LastLevelInGalaxy == 1)
        {
            text.text = string.Format(format, unlimitedText);
            return;
        }

        int remaining = Mathf.Max(0, PlayerProgress.CurrentLevelRetriesRemaining());
        text.text = string.Format(format, remaining);
    }
}