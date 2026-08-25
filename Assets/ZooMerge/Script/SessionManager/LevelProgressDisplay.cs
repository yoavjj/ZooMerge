using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelProgressDisplay : LevelProgressBarSlider
{
    [Header("Simple Progress")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private GameObject fakeFillTip;

    [Header("Text Format")]
    [SerializeField] private string progressFormat = "{0}%";

    public override void InitializeCurrentLevel(bool skipSliderSet = false)
    {
        int totalLevels = Mathf.Max(1, MergeLevelManager.LevelsInCurrentGalaxy);

        int currentLevel = Mathf.Clamp(
            MergeLevelManager.CurrentLevelInGalaxy,
            1,
            totalLevels
        );

        float normalizedProgress = (float)(currentLevel - 1) / totalLevels;
        int percentage = Mathf.RoundToInt(normalizedProgress * 100f);

        if (progressSlider != null)
        {
            progressSlider.wholeNumbers = false;
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;

            if (!skipSliderSet)
                progressSlider.value = normalizedProgress;
        }

        if (fakeFillTip != null)
            fakeFillTip.SetActive(normalizedProgress > 0f);

        if (progressText != null)
            progressText.text = string.Format(progressFormat, percentage);
    }
}