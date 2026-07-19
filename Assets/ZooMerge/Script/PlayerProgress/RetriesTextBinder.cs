using TMPro;
using UnityEngine;

public class RetriesTextBinder : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TextMeshProUGUI text;

    [Header("Format")]
    [SerializeField] private string format = "{0}";
    [SerializeField] private string unlimitedText = "∞";

    [Header("Arrive FX Source")]
    [SerializeField] private CollectibleFlyTarget flyTarget; // assign the SAME target that plays the Add trigger

    private void Reset()
    {
        if (text == null) text = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        PlayerProgress.RetriesChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        PlayerProgress.RetriesChanged -= Refresh;
    }

    public void Refresh()
    {
        if (text == null) return;

        int remaining = PlayerProgress.CurrentLevelRetriesRemaining();

        // unlimited
        if (remaining == int.MaxValue)
        {
            text.text = string.Format(format, unlimitedText);
            return;
        }

        // clamped normal display
        int cap = PlayerProgress.GetRetryCap();
        remaining = Mathf.Clamp(remaining, 0, cap);
        text.text = string.Format(format, remaining);
    }

    // 🔥 Animation Event calls this (no args, safest)
    public void AE_AddArriveAmountToText()
    {
        if (text == null) return;

        if (flyTarget != null)
            flyTarget.CommitPendingArrive();
            
        Refresh();
    }
}