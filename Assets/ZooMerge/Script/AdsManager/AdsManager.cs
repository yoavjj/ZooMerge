using UnityEngine;
using Unity.Services.LevelPlay;
using System;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    private LevelPlayBannerAd bannerAd;

    private LevelPlayRewardedAd rewardedAd;
    private bool rewardedReady;
    private Action onRewardGranted;
    private Action<string> onRewardFailed;

    private void Awake()
    {
        //Debug.Log($"[DeviceID] {SystemInfo.deviceUniqueIdentifier}");

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        FirebaseInitializer.WaitForFirebase(
    onReady: () =>
    {
        //Debug.Log("[AdManager] Firebase ready → init ads");
        InitAds();
    },
    onError: error =>
    {
        Debug.LogError($"[AdManager] Firebase failed: {error}");
    }
);
    }

    private void InitAds()
    {
        Debug.Log("[AdManager] Initializing LevelPlay...");

        // 🧪 Force test ads
        LevelPlay.SetMetaData("is_test_suite", "enable");

        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;
        LevelPlay.Init(RemoteAdConfig.AppKey);
    }

    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log("[AdManager] LevelPlay Init Success");
        SetupBanner();

        SetupRewarded();
        LoadRewarded();
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogError($"[AdManager] LevelPlay Init Failed: {error}");
    }

    private void SetupBanner()
    {
        var config = new LevelPlayBannerAd.Config.Builder()
            .SetPosition(LevelPlayBannerPosition.BottomCenter)
            .SetRespectSafeArea(true)
            .SetDisplayOnLoad(true)
            .Build();

        bannerAd = new LevelPlayBannerAd(
            RemoteAdConfig.BannerAdUnitId,
            config
        );

        bannerAd.OnAdLoaded += info =>
        {
            Debug.Log("[AdManager] Banner Loaded");
        };

        bannerAd.OnAdLoadFailed += error =>
        {
            Debug.LogError($"[AdManager] Banner Load Failed: {error}");
        };
    }


    public void LoadBanner()
    {
        if (bannerAd == null)
        {
            Debug.Log("[AdManager] Re-creating banner instance...");
            SetupBanner();
        }

        Debug.Log("[AdManager] Loading banner...");
        bannerAd.LoadAd();
    }

    public void HideBanner()
    {
        if (bannerAd != null)
        {
            Debug.Log("[AdManager] Hiding and destroying banner...");
            bannerAd.HideAd();
            bannerAd.DestroyAd();  // ✅ This resets the banner lifecycle
            bannerAd = null;
        }
        else
        {
            Debug.LogWarning("[AdManager] Tried to hide banner, but it's null.");
        }
    }

    private void SetupRewarded()
    {
        rewardedAd = new LevelPlayRewardedAd(RemoteAdConfig.RewardedAdUnitId);

        rewardedAd.OnAdLoaded += info =>
        {
            rewardedReady = true;
            Debug.Log("[AdManager] Rewarded Loaded");
        };

        rewardedAd.OnAdLoadFailed += error =>
        {
            rewardedReady = false;
            Debug.LogError($"[AdManager] Rewarded Load Failed: {error}");
        };

        rewardedAd.OnAdDisplayed += info =>
        {
            Debug.Log("[AdManager] Rewarded Displayed");
        };

        rewardedAd.OnAdDisplayFailed += (info, error) =>
        {
            Debug.LogError($"[AdManager] Rewarded Display Failed: {error}");

            onRewardFailed?.Invoke(error.ToString());
            onRewardGranted = null;
            onRewardFailed = null;

            rewardedReady = false;
            LoadRewarded();
        };

        rewardedAd.OnAdRewarded += (info, reward) =>
        {
            Debug.Log($"[AdManager] Rewarded! name={reward.Name} amount={reward.Amount}");

            onRewardGranted?.Invoke();
            onRewardGranted = null;
            onRewardFailed = null;
        };

        rewardedAd.OnAdClosed += info =>
        {
            Debug.Log("[AdManager] Rewarded Closed");

            rewardedReady = false;
            LoadRewarded(); // preload next one
        };
    }

    public void LoadRewarded()
    {
        if (rewardedAd == null) SetupRewarded();
        Debug.Log("[AdManager] Loading rewarded...");
        rewardedAd.LoadAd();
    }

    public bool IsRewardedReady()
    {
        return rewardedReady && rewardedAd != null;
    }

    public void ShowRewarded(Action onReward, Action<string> onFail = null)
    {
        if (!IsRewardedReady())
        {
            onFail?.Invoke("Rewarded not ready");
            // kick a load attempt
            LoadRewarded();
            return;
        }

        onRewardGranted = onReward;
        onRewardFailed = onFail;

        Debug.Log("[AdManager] Showing rewarded...");
        rewardedAd.ShowAd();
    }
}
