using System.Collections;
using TMPro;
using UnityEngine;

public class CoinCooldownWidget : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Animator")]
    [SerializeField] private Animator cooldownAnimator;
    [SerializeField] private string doneTrigger = "Done";
    [SerializeField] private string collectTrigger = "Collect";

    [Header("Config")]
    [SerializeField] private CollectibleFlyController collectibleFlyController;
    [SerializeField] private CoinFlyService coinFlyService; // (e.g. main menu)
    [SerializeField] private RectTransform cooldownCoinSpawnContainer;
    [SerializeField] private CooldownRewardScheduleSO schedule;

    private Coroutine tickRoutine;
    private bool doneStateShown;

    private void OnEnable()
    {
        if (schedule == null)
        {
            Debug.LogError("[CoinCooldownWidget] Missing schedule ScriptableObject.");
            return;
        }

        var step = schedule.GetStep(CooldownRewardProgress.StepIndex);

        CoinCooldown.RewardCoins = step.rewardCoins;
        CoinCooldown.CooldownSeconds = step.cooldownSeconds;
        CoinCooldown.EnsureInitialized();

        doneStateShown = false;

        tickRoutine = StartCoroutine(Tick());
    }

    private void OnDisable()
    {
        if (tickRoutine != null)
        {
            StopCoroutine(tickRoutine);
            tickRoutine = null;
        }
    }

    private IEnumerator Tick()
    {
        while (true)
        {
            if (CoinCooldown.IsReadyToCollect())
            {
                if (!doneStateShown)
                {
                    doneStateShown = true;
                    if (cooldownAnimator != null && !string.IsNullOrEmpty(doneTrigger))
                        cooldownAnimator.SetTrigger(doneTrigger);
                }

                // When ready, we can stop updating the timer text (optional)
                if (timerText != null)
                    timerText.text = ": 00:00";
            }
            else
            {
                if (timerText != null)
                    timerText.text = CoinCooldown.GetRemainingText();
            }

            yield return new WaitForSecondsRealtime(0.5f);
        }
    }

    public void Collect()
    {
        if (!CoinCooldown.IsReadyToCollect())
            return;

        int coinsToGrant = CoinCooldown.RewardCoins; // snapshot

        if (collectibleFlyController != null)
        {
            collectibleFlyController.SpawnCoinsToTopBar(
                coinsToGrant,
                cooldownCoinSpawnContainer,
                CollectibleFlyController.CoinSpawnSource.Cooldown
            );
        }
        else if (coinFlyService != null)
        {
            coinFlyService.FlyCoins(
                coinsToGrant,
                CoinFlyService.Source.Cooldown,
                cooldownCoinSpawnContainer
            );
        }
        else
        {
            GameInventory.Instance.Add(CurrencyType.Coins, coinsToGrant);
            CloudSaveManager.SyncEconomyNow();
        }

        cooldownAnimator.SetTrigger(collectTrigger);

        // ✅ move to next step for next timer
        CooldownRewardProgress.AdvanceStepLoop(schedule.MaxStepCount);

        // ✅ apply next step values
        var nextStep = schedule.GetStep(CooldownRewardProgress.StepIndex);
        CoinCooldown.RewardCoins = nextStep.rewardCoins;
        CoinCooldown.CooldownSeconds = nextStep.cooldownSeconds;

        // ✅ restart cooldown with new duration
        CoinCooldown.RestartCooldown();

        doneStateShown = false;
    }
}