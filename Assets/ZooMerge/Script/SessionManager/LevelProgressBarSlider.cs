using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public struct CountWidth
{
    public int enemyCount;
    public float width;
    public float leftPaddingOverride;  // Set to -1 to use default
    public float rightPaddingOverride; // Set to -1 to use default
}

public class LevelProgressBarSlider : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Slider slider;                // LvlProgress_Slider
    [SerializeField] private RectTransform widthTarget;    // usually the slider's RectTransform
    [SerializeField] private BallSet ballSet;              // for enemy icons

    [Header("Icon Strip")]
    [SerializeField] private RectTransform iconsContainer; // parent for EnemyLvl_Image / Line_Image
    [SerializeField] private GameObject lineImagePrefab;   // tick line prefab (between enemies)

    [Header("Sizing")]
    [SerializeField] private float minWidth = 200f;
    [SerializeField] private float maxWidth = 600f;
    [SerializeField] private RectTransform sliderFillArea; // Assign in inspector (Slider > Fill Area)
    private float finalLineXPos = 0f; // dynamically calculated

    // Optional: override width per enemy count (exact control per count)
    [Header("Width per enemy count (optional)")]
    [SerializeField]
    private CountWidth[] widthByEnemyCount = new CountWidth[] {
    new CountWidth{ enemyCount = 1, width = 300, leftPaddingOverride = 48f, rightPaddingOverride = 48f },
    new CountWidth{ enemyCount = 2, width = 360, leftPaddingOverride = 40f, rightPaddingOverride = 40f },
    new CountWidth{ enemyCount = 3, width = 420, leftPaddingOverride = -1, rightPaddingOverride = -1 }, // uses default
    new CountWidth{ enemyCount = 4, width = 480, leftPaddingOverride = 20f, rightPaddingOverride = 20f },
};

    [Header("Layout")]
    [SerializeField] private float leftPadding = 24f;
    [SerializeField] private float rightPadding = 24f;

    // Static global list (order = instantiation order across all bars)
    public static readonly List<EnemyIconNode> GlobalIcons = new List<EnemyIconNode>();

    // Per-instance ordered icons for THIS bar
    private readonly List<EnemyIconNode> _icons = new List<EnemyIconNode>();

    // Build-time static context so icons can self-register without GetComponent
    private static LevelProgressBarSlider _buildingOwner;
    private static int _buildingIndex;
    private Coroutine sliderAnimRoutine;
    private Coroutine advanceRoutine;

    private int _lastLevelNumber = -1;
    private int _lastEnemyCount = -1;

    [ContextMenu("Initialize Current Level")]
    public void InitializeCurrentLevel()
    {
        if (slider == null) slider = GetComponent<Slider>();
        if (widthTarget == null) widthTarget = GetComponent<RectTransform>();
        if (slider == null || widthTarget == null) return;

        int currentLevel = MergeLevelManager.CurrentLevelNumber;
        int totalEnemies = Mathf.Max(0, MergeLevelManager.TotalEnemiesInLevel);
        int currentIndex = Mathf.Max(0, MergeLevelManager.CurrentEnemyIndex);

        bool needsRebuild = currentLevel != _lastLevelNumber || totalEnemies != _lastEnemyCount;

        slider.wholeNumbers = false;
        slider.minValue = 0f;
        slider.maxValue = Mathf.Max(1, totalEnemies);
        slider.value = Mathf.Clamp(currentIndex, slider.minValue, slider.maxValue);

        float targetW = GetWidthForCount(totalEnemies);
        widthTarget.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetW);

        if (needsRebuild)
        {
            Debug.Log($"[ProgressBar] Rebuilding layout for Level {currentLevel} with {totalEnemies} enemies.");
            BuildEnemyStrip();
            StartCoroutine(AdjustFillAfterLayout());

            _lastLevelNumber = currentLevel;
            _lastEnemyCount = totalEnemies;
        }
        else
        {
            Debug.Log("[ProgressBar] Skipping layout rebuild — using cached visuals.");
        }
    }

    private IEnumerator AdjustFillAfterLayout()
    {
        // ensure UI layout is applied
        yield return new WaitForEndOfFrame();

        // Force layout (optional but safer across devices)
        LayoutRebuilder.ForceRebuildLayoutImmediate(widthTarget);

        float totalWidth = widthTarget.rect.width;
        float paddingOffset = 4f;
        float fillEnd = Mathf.Clamp(finalLineXPos + paddingOffset, 0, totalWidth);
        float normalizedFillX = fillEnd / totalWidth;

        sliderFillArea.anchorMin = new Vector2(0f, 0f);
        sliderFillArea.anchorMax = new Vector2(normalizedFillX, 1f);
        sliderFillArea.offsetMin = Vector2.zero;
        sliderFillArea.offsetMax = Vector2.zero;
    }

    private float GetWidthForCount(int count)
    {
        // if mapping provided, use it
        for (int i = 0; i < widthByEnemyCount.Length; i++)
            if (widthByEnemyCount[i].enemyCount == count)
                return Mathf.Clamp(widthByEnemyCount[i].width, minWidth, maxWidth);

        // fallback: spread nicely between min/max
        if (count <= 1) return Mathf.Clamp((minWidth + maxWidth) * 0.5f, minWidth, maxWidth);
        float t = Mathf.InverseLerp(2, 6, Mathf.Clamp(count, 2, 6)); // tweakable range
        float w = Mathf.Lerp(minWidth, maxWidth, t);
        return Mathf.Clamp(w, minWidth, maxWidth);
    }

    private void BuildEnemyStrip()
    {
        if (iconsContainer == null)
        {
            Debug.LogWarning("[LevelProgressBarSlider] Missing iconsContainer.");
            return;
        }

        // clear old children
        for (int i = iconsContainer.childCount - 1; i >= 0; i--)
        {
            var child = iconsContainer.GetChild(i);
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(child.gameObject);
            else
#endif
                Destroy(child.gameObject);
        }
        _icons.Clear();

        var level = MergeLevelManager.GetCurrentLevel();
        var enemyList = level.enemy_data;
        if (enemyList == null || enemyList.Count == 0) return;

        int total = enemyList.Count;

        // make sure widths line up
        iconsContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, widthTarget.rect.width);

        // (optional) rebuild so rects have valid sizes this frame
        // LayoutRebuilder.ForceRebuildLayoutImmediate(widthTarget);

        GetPaddingForCount(total, out float effectiveLeftPadding, out float effectiveRightPadding);

        float containerWidth = widthTarget.rect.width;
        float usableW = Mathf.Max(0f, containerWidth - effectiveLeftPadding - effectiveRightPadding);

        // allow EnemyIconNode self-register
        _buildingOwner = this;

        float spacing = usableW / (enemyList.Count * 2 - 1);

        for (int i = 0; i < enemyList.Count; i++)
        {
            int enemyId = enemyList[i].id;
            var iconPrefab = ballSet != null ? ballSet.GetEnemyIconPrefabById(enemyId.ToString()) : null;

            if (iconPrefab != null)
            {
                _buildingIndex = i;
                var iconGO = Instantiate(iconPrefab, iconsContainer);
                float xIcon = effectiveLeftPadding + spacing * (i * 2);
                Place((RectTransform)iconGO.transform, xIcon);
            }

            // place line AFTER the icon, if not the last
            bool hasNext = (i < enemyList.Count - 1);
            if (hasNext && lineImagePrefab != null)
            {
                var lineGO = Instantiate(lineImagePrefab, iconsContainer);
                float xLine = effectiveLeftPadding + spacing * (i * 2 + 1);
                Place((RectTransform)lineGO.transform, xLine);

                if (i == enemyList.Count - 2) // This is the final line
                {
                    finalLineXPos = xLine;
                }
            }
        }

        _buildingOwner = null;
        _buildingIndex = -1;
    }

    private static void Place(RectTransform rt, float x)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0f);
    }

    // === EnemyIcon registry (no GetComponent) ===
    internal static void TryRegisterBuildingIcon(EnemyIconNode node)
    {
        if (_buildingOwner == null || node == null) return;

        var list = _buildingOwner._icons;
        int index = Mathf.Clamp(_buildingIndex, 0, list.Count);
        if (index == list.Count) list.Add(node);
        else list.Insert(index, node);

        GlobalIcons.Add(node);
    }

    public void MarkEnemyDone(int index)
    {
        if (index < 0 || index >= _icons.Count)
        {
            Debug.LogWarning($"[MarkEnemyDone] Invalid index {index}");
            return;
        }

        //Debug.Log($"[MarkEnemyDone] Calling TriggerGrey on index {index} at {Time.time:F2}");
        _icons[index]?.TriggerGrey();
    }

    private void OnGameOver(BallInfo info, BallEventManager.GameOverReason reason)
    {
        if (reason == BallEventManager.GameOverReason.Won)
        {
            for (int i = 0; i < _icons.Count; i++)
                _icons[i]?.TriggerGrey();
            slider.value = slider.maxValue;
        }
    }

    public void AnimateSliderTo(float targetValue, float duration, AnimationCurve curve = null)
    {
        if (slider == null) return;
        if (sliderAnimRoutine != null) StopCoroutine(sliderAnimRoutine);
        sliderAnimRoutine = StartCoroutine(AnimateSliderRoutine(targetValue, duration, curve));
    }

    private IEnumerator AnimateSliderRoutine(float target, float duration, AnimationCurve curve)
    {
        float start = slider.value;

        if (duration <= 0f)
        {
            slider.value = target;
            sliderAnimRoutine = null;
            yield break;
        }

        var ease = curve ?? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float k = ease.Evaluate(Mathf.Clamp01(t));
            slider.value = Mathf.Lerp(start, target, k);
            yield return null;
        }

        slider.value = target;
        sliderAnimRoutine = null;
    }

    public void PrepareMidLevelAnimationStartAtPrevIndex()
    {
        int total = MergeLevelManager.TotalEnemiesInLevel;
        int nextIndex = Mathf.Clamp(MergeLevelManager.CurrentEnemyIndex, 0, total);
        float startIndex = Mathf.Clamp(nextIndex - 1f, slider.minValue, slider.maxValue);
        slider.value = startIndex;
    }

    public void SyncIconsToCurrentProgress(bool includeCurrent = false)
    {
        int totalIcons = _icons.Count;
        int current = Mathf.Clamp(MergeLevelManager.CurrentEnemyIndex, 0, totalIcons);

        int lastToGrey = includeCurrent ? current : current - 1;
        for (int i = 0; i <= lastToGrey; i++)
        {
            if (i >= 0 && i < totalIcons)
                _icons[i]?.TriggerGrey();
        }

        // Ensure slider shows the current progress tick
        if (slider != null)
            slider.value = Mathf.Clamp(current, slider.minValue, slider.maxValue);
    }

    public void SetSliderInstant(float value)
    {
        if (slider == null) return;
        slider.value = Mathf.Clamp(value, slider.minValue, slider.maxValue);
    }
    public void MarkRangeDoneInclusive(int from, int to)
    {
        if (_icons.Count == 0) return;
        if (from > to) return;
        int a = Mathf.Clamp(from, 0, _icons.Count - 1);
        int b = Mathf.Clamp(to, 0, _icons.Count - 1);
        for (int i = a; i <= b; i++)
            _icons[i]?.TriggerDone();
    }

    public void PlayAdvanceAnimationFromPopup(bool toLevelEnd, float delay, float duration, AnimationCurve curve)
    {
        if (advanceRoutine != null) StopCoroutine(advanceRoutine);
        advanceRoutine = StartCoroutine(AdvanceRoutine(toLevelEnd, delay, duration, curve));
    }

    // ADD this routine (moved logic from WinLosePopup)
    private IEnumerator AdvanceRoutine(bool toLevelEnd, float delay, float duration, AnimationCurve curve)
    {
        // 1) Build bar for current state
        InitializeCurrentLevel();

        int total = MergeLevelManager.TotalEnemiesInLevel;
        int current = Mathf.Clamp(MergeLevelManager.CurrentEnemyIndex, 0, total);

        if (toLevelEnd)
        {
            // prev enemies: 0..total-2 should be DONE immediately
            if (total >= 2)
                MarkRangeDoneInclusive(0, total - 2);

            // start slider from the last tick (e.g., 2/3 -> tick 2)
            SetSliderInstant(Mathf.Max(0, total - 1));

            // wait, then grey the last just-defeated enemy and animate to full
            if (delay > 0f) yield return new WaitForSeconds(delay);

            if (total > 0)
                MarkEnemyDone(total - 1); // this calls TriggerGrey on the final icon

            AnimateSliderTo(total, duration, curve);
        }
        else
        {
            // --- MID-LEVEL ADVANCE ---
            PrepareMidLevelAnimationStartAtPrevIndex();

            if (delay > 0f) yield return new WaitForSeconds(delay);

            int nextIndex = Mathf.Clamp(current, 0, total);

            //Debug.Log($"[AdvanceRoutine] Waiting for delay {delay} before TriggerGrey + Animate, time = {Time.time:F2}");
            if (delay > 0f) yield return new WaitForSeconds(delay);

            // Trigger grey
            int defeatedIndex = Mathf.Clamp(nextIndex - 1, 0, _icons.Count - 1);
            MarkEnemyDone(defeatedIndex);

            // Animate slider
            AnimateSliderTo(nextIndex, duration, curve);
        }

        advanceRoutine = null;
    }

    private void GetPaddingForCount(int count, out float left, out float right)
    {
        // Default to current serialized values
        left = leftPadding;
        right = rightPadding;

        foreach (var entry in widthByEnemyCount)
        {
            if (entry.enemyCount == count)
            {
                if (entry.leftPaddingOverride >= 0f)
                    left = entry.leftPaddingOverride;
                if (entry.rightPaddingOverride >= 0f)
                    right = entry.rightPaddingOverride;
                return;
            }
        }
    }

    public void RestartVisuals()
    {
        Debug.Log("[LevelProgressBarSlider] RestartVisuals called");

        // Restart each icon (animator trigger)
        foreach (var icon in _icons)
        {
            icon?.TriggerRestart();
        }

        // Reset slider to beginning
        slider.value = slider.minValue;
    }
}
