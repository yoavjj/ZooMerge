using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MergeCounterItem : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Animator animator;

    private int count = 1;
    private float lastAddTime = -1f;
    private float addCooldown = 1f; // seconds between "Add" animation triggers

    public void Initialize(Sprite icon)
    {
        if (iconImage != null)
            iconImage.sprite = icon;

        SetCount(0);
        lastAddTime = Time.time;
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

    // Optional animation peak callback
    public void OnPeakAnimationEvent()
    {
        // Do something here if needed
    }
}
