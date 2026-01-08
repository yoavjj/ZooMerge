using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MergeCounterItem : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Animator animator;
    [SerializeField] private int minCountToAnimate = 5;


    public BallType Type { get; private set; }
    // 👇 Stored animation request
    private int targetCount;
    public int TargetCount => targetCount;
    public System.Action<MergeCounterItem> OnCountAnimationFinished;


    private float countAnimDuration;
    private MonoBehaviour runner;

    private int count = 1;
    private float lastAddTime = -1f;
    private float addCooldown = 1f; // seconds between "Add" animation triggers

    private bool shouldAnimateCount;

    public void Initialize(Sprite icon)
    {
        if (iconImage != null)
            iconImage.sprite = icon;

        SetCount(0);
    }

    public void PlayIn()
    {
        animator?.SetTrigger("In");
    }

    public void PrepareCountAnimation(int target, float duration, MonoBehaviour runner)
    {
        this.targetCount = target;
        this.countAnimDuration = duration;
        this.runner = runner;

        // 👇 Decide here
        shouldAnimateCount = target > minCountToAnimate;

        // If we won't animate, set value immediately
        if (!shouldAnimateCount)
        {
            SetCount(targetCount);
        }
    }

    // Called from Animation Event
    public void PlayCountAnimation()
    {
        // ❗ If we don't animate the count, we STILL finish immediately
        if (!shouldAnimateCount)
        {
            OnCountAnimationFinished?.Invoke(this);
            return;
        }

        if (runner == null)
        {
            SetCount(targetCount);
            return;
        }

        runner.StartCoroutine(AnimateRoutine(targetCount, countAnimDuration));
    }

    public void Increment()
    {
        count++;
        SetCount(count);

        if (Time.time - lastAddTime > addCooldown)
        {
            animator?.SetTrigger("Add");
            lastAddTime = Time.time;
        }
    }

    public void SetCount(int newCount)
    {
        count = newCount;
        if (countText != null)
            countText.text = count.ToString();
    }

    private IEnumerator AnimateRoutine(int target, float duration)
    {
        int start = 0;
        float time = 0f;

        SetCount(0);

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            int value = Mathf.RoundToInt(Mathf.Lerp(start, target, t));
            SetCount(value);
            yield return null;
        }

        SetCount(target);
        OnCountAnimationFinished?.Invoke(this);

    }

    public void SetType(BallType type)
    {
        Type = type;
    }
}
