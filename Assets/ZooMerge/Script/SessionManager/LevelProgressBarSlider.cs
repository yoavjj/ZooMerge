using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class LevelProgressBarSlider : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Slider slider;                // LvlProgress_Slider
    [SerializeField] private RectTransform widthTarget;    // usually the slider's RectTransform
    [SerializeField] private BallSet ballSet;              // for enemy icons

    [Header("Level Info")]
    [SerializeField] private TextMeshProUGUI levelNameText;

    [Header("Icon Strip")]
    [SerializeField] private RectTransform iconsContainer; // parent for EnemyLvl_Image / Line_Image
    [SerializeField] private GameObject lineImagePrefab;   // tick line prefab (between enemies)

    [Header("Sizing")]
    [SerializeField] private RectTransform sliderFillArea; // Assign in inspector (Slider > Fill Area)
    private float finalLineXPos = 0f; // dynamically calculated
    [SerializeField] private CountWidth[] widthByEnemyCount;
    [SerializeField] private float minWidth = 200f;
    [SerializeField] private float maxWidth = 600f;
    [SerializeField] private float leftPadding = 24f;
    [SerializeField] private float rightPadding = 24f;

    [Header("Layout")]
    [SerializeField] UIFloorAnchor floorAnchor;

    // Static global list (order = instantiation order across all bars)
    public static readonly List<EnemyIconNode> GlobalIcons = new List<EnemyIconNode>();

    // Build-time static context so icons can self-register without GetComponent
    private static LevelProgressBarSlider _buildingOwner;
    private static int _buildingIndex;
    private Coroutine advanceRoutine;

    private int _lastLevelNumber = -1;
    private int _lastEnemyCount = -1;

    private EnemyProgressConfig config;
    private EnemyIconController iconController = new();
    private SliderAnimator sliderAnimator;
    private EnemyStripBuilder stripBuilder;


    [ContextMenu("Initialize Current Level")]

    private void Start()
    {
        config = new EnemyProgressConfig(widthByEnemyCount, minWidth, maxWidth, leftPadding, rightPadding);
        stripBuilder = new EnemyStripBuilder(iconsContainer, lineImagePrefab, ballSet);
        sliderAnimator = new SliderAnimator(slider, this);
    }

    public void InitializeCurrentLevel(bool skipSliderSet = false)
    {
        if (slider == null) slider = GetComponent<Slider>();
        if (widthTarget == null) widthTarget = GetComponent<RectTransform>();
        if (config == null)
            config = new EnemyProgressConfig(widthByEnemyCount, minWidth, maxWidth, leftPadding, rightPadding);
        if (stripBuilder == null)
            stripBuilder = new EnemyStripBuilder(iconsContainer, lineImagePrefab, ballSet);
        if (sliderAnimator == null)
            sliderAnimator = new SliderAnimator(slider, this);
        if (slider == null || widthTarget == null) return;

        int currentLevel = MergeLevelManager.CurrentLevelNumber;
        int totalEnemies = Mathf.Max(0, MergeLevelManager.TotalEnemiesInLevel);
        int currentIndex = Mathf.Max(0, MergeLevelManager.CurrentEnemyIndex);

        bool needsRebuild = currentLevel != _lastLevelNumber || totalEnemies != _lastEnemyCount;

        slider.wholeNumbers = false;
        slider.minValue = 0f;
        slider.maxValue = Mathf.Max(1, totalEnemies);
        if (!skipSliderSet)
        {
            slider.value = Mathf.Clamp(currentIndex, slider.minValue, slider.maxValue);
        }

        float targetW = config.GetWidth(totalEnemies);
        widthTarget.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetW);

        if (needsRebuild)
        {
            Debug.Log($"[ProgressBar] Rebuilding layout for Level {currentLevel} with {totalEnemies} enemies.");
            var level = MergeLevelManager.GetCurrentLevel();

            if (level == null)
            {
                Debug.LogWarning("[ProgressBar] GetCurrentLevel() returned null. Skipping strip build.");
                return;
            }

            if (levelNameText != null)
            {
                levelNameText.text = level.name;
            }

            if (stripBuilder == null)
                stripBuilder = new EnemyStripBuilder(iconsContainer, lineImagePrefab, ballSet);

            // ✅ Clear all existing child GameObjects before building
            foreach (Transform child in iconsContainer)
            {
                Destroy(child.gameObject);
            }

            RestartVisuals(); // Reset visuals like checkmarks if needed

            stripBuilder.Build(
                iconController.GetRawList(),
                config,
                widthTarget.rect.width,
                level.enemy_data,
                i =>
                {
                    _buildingOwner = this;
                    _buildingIndex = i;
                },
                out finalLineXPos
            );

            _buildingOwner = null;
            _buildingIndex = -1;
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

        // ✅ Align after layout is done
        if (floorAnchor != null)
            floorAnchor.AlignNow();
    }

    // === EnemyIcon registry (no GetComponent) ===
    internal static void TryRegisterBuildingIcon(EnemyIconNode node)
    {
        if (_buildingOwner == null || node == null) return;

        _buildingOwner.iconController.Add(node);
        GlobalIcons.Add(node);
    }

    public void SyncIconsToCurrentProgress(bool includeCurrent = false)
    {
        int current = Mathf.Clamp(MergeLevelManager.CurrentEnemyIndex, 0, iconController.Icons.Count);
        iconController.SyncToIndex(current, includeCurrent);
        sliderAnimator?.SetInstant(current);
    }

    public void SetSliderInstant(float value)
    {
        sliderAnimator?.SetInstant(value);
    }

    public void MarkRangeDoneInclusive(int from, int to) => iconController.MarkRangeDoneInclusive(from, to);

    public void PlayAdvanceAnimationFromPopup(bool toLevelEnd, float delay, float duration, AnimationCurve curve)
    {
        if (advanceRoutine != null) StopCoroutine(advanceRoutine);
        advanceRoutine = StartCoroutine(AdvanceRoutine(toLevelEnd, delay, duration, curve));
    }

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
            // MID-LEVEL ADVANCE

            int nextIndex = Mathf.Clamp(MergeLevelManager.CurrentEnemyIndex, 0, total);
            int previousIndex = Mathf.Max(0, nextIndex - 1);

            // 1️⃣ Upgrade ALL previous greys to DONE (no delay)
            iconController.UpgradePreviousToDone(previousIndex);

            // 2️⃣ Slider starts at previous index
            SetSliderInstant(previousIndex);

            // 3️⃣ Wait for delay
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            // 4️⃣ Newly defeated enemy becomes GREY
            MarkEnemyDone(previousIndex);

            // 5️⃣ Animate slider forward
            AnimateSliderTo(nextIndex, duration, curve);
        }

        advanceRoutine = null;
    }

    public void MarkEnemyDone(int index) => iconController.MarkEnemyDone(index);

    public void AnimateSliderTo(float value, float duration, AnimationCurve curve = null)
    {
        sliderAnimator?.AnimateTo(value, duration, curve);
    }

    public void RestartVisuals() => iconController.TriggerRestartAll();
}
