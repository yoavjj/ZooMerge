using UnityEngine;
using Unity.Services.LevelPlay;
using System;
using System.Collections;

#if UNITY_IOS
using Unity.Advertisement.IosSupport;
#endif

public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    private LevelPlayBannerAd bannerAd;
    private LevelPlayRewardedAd rewardedAd;

    private bool rewardedReady;
    private bool adsInitialized;

    private Action onRewardGranted;
    private Action<string> onRewardFailed;

    private void Awake()
    {
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
                StartCoroutine(WaitATTAndInitAds());
            },
            onError: error =>
            {
                Debug.LogError($"[AdManager] Firebase failed: {error}");
            }
        );
    }

    private IEnumerator WaitATTAndInitAds()
    {
#if UNITY_IOS
        Debug.Log("[AdManager] Waiting for ATT Permission resolution...");

        while (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() ==
               ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
        {
            yield return null;
        }

        Debug.Log($"[AdManager] ATT Resolved: {ATTrackingStatusBinding.GetAuthorizationTrackingStatus()}");
#endif

        InitAds();
        yield return null;
    }

    private string CleanId(string value)
    {
        return string.IsNullOrEmpty(value) ? "" : value.Trim();
    }

    private void InitAds()
    {
        if (adsInitialized)
        {
            Debug.LogWarning("[AdManager] LevelPlay already initialized. Skipping.");
            return;
        }

        adsInitialized = true;

        string appKey = CleanId(RemoteAdConfig.AppKey);
        string bannerId = CleanId(RemoteAdConfig.BannerAdUnitId);
        string rewardedId = CleanId(RemoteAdConfig.RewardedAdUnitId);
        string userId = CleanId(FirebaseInitializer.UserId);

        // Debug.Log("[AdManager] Initializing LevelPlay...");
        // Debug.Log($"[AdManager] AppKey='{appKey}', length={appKey.Length}");
        // Debug.Log($"[AdManager] BannerAdUnitId='{bannerId}', length={bannerId.Length}");
        // Debug.Log($"[AdManager] RewardedAdUnitId='{rewardedId}', length={rewardedId.Length}");
        // Debug.Log($"[AdManager] Firebase UserId='{userId}', length={userId.Length}");

        if (string.IsNullOrEmpty(appKey))
        {
            Debug.LogError("[AdManager] AppKey is empty. Cannot initialize LevelPlay.");
            return;
        }

        LevelPlay.SetMetaData("is_test_suite", "enable");

        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;

        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;

        if (!string.IsNullOrEmpty(userId))
        {
            Debug.Log($"[AdManager] Initializing LevelPlay with UserId: {userId}");
            LevelPlay.Init(appKey, userId);
        }
        else
        {
            Debug.LogWarning("[AdManager] Firebase UserId is empty. Initializing LevelPlay without UserId.");
            LevelPlay.Init(appKey);
        }
    }

    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log("[AdManager] LevelPlay Init Success");

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        Debug.Log("[AdManager] Launching LevelPlay Test Suite...");
        LevelPlay.LaunchTestSuite();
#endif

        // TEMP: Do not auto-load ads while debugging error 626.
        // SetupBanner();
        // SetupRewarded();
        // LoadRewarded();
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogError($"[AdManager] LevelPlay Init Failed: {error}");
    }

    private void SetupBanner()
    {
        string bannerId = CleanId(RemoteAdConfig.BannerAdUnitId);

        if (string.IsNullOrEmpty(bannerId))
        {
            Debug.LogError("[AdManager] BannerAdUnitId is empty. Cannot create banner.");
            return;
        }

        var config = new LevelPlayBannerAd.Config.Builder()
            .SetPosition(LevelPlayBannerPosition.BottomCenter)
            .SetRespectSafeArea(true)
            .SetDisplayOnLoad(true)
            .Build();

        bannerAd = new LevelPlayBannerAd(bannerId, config);

        bannerAd.OnAdLoaded += info =>
        {
            Debug.Log($"[AdManager] Banner Loaded for '{bannerId}'");
        };

        bannerAd.OnAdLoadFailed += error =>
        {
            Debug.LogError($"[AdManager] Banner Load Failed for '{bannerId}': {error}");
        };
    }

    public void LoadBanner()
    {
        if (bannerAd == null)
        {
            Debug.Log("[AdManager] Re-creating banner instance...");
            SetupBanner();
        }

        if (bannerAd == null)
        {
            Debug.LogError("[AdManager] Banner is still null. Cannot load banner.");
            return;
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
            bannerAd.DestroyAd();
            bannerAd = null;
        }
        else
        {
            Debug.LogWarning("[AdManager] Tried to hide banner, but it's null.");
        }
    }

    private void SetupRewarded()
    {
        string rewardedId = CleanId(RemoteAdConfig.RewardedAdUnitId);

        if (string.IsNullOrEmpty(rewardedId))
        {
            Debug.LogError("[AdManager] RewardedAdUnitId is empty. Cannot create rewarded ad.");
            return;
        }

        rewardedAd = new LevelPlayRewardedAd(rewardedId);

        rewardedAd.OnAdLoaded += info =>
        {
            rewardedReady = true;
            Debug.Log($"[AdManager] Rewarded Loaded for '{rewardedId}'");
        };

        rewardedAd.OnAdLoadFailed += error =>
        {
            rewardedReady = false;
            Debug.LogError($"[AdManager] Rewarded Load Failed for '{rewardedId}': {error}");
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
            LoadRewarded();
        };
    }

    public void LoadRewarded()
    {
        if (rewardedAd == null)
        {
            SetupRewarded();
        }

        if (rewardedAd == null)
        {
            Debug.LogError("[AdManager] Rewarded ad is still null. Cannot load rewarded.");
            return;
        }

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
            LoadRewarded();
            return;
        }

        onRewardGranted = onReward;
        onRewardFailed = onFail;

        Debug.Log("[AdManager] Showing rewarded...");
        rewardedAd.ShowAd();
    }
}