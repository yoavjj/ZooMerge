using UnityEngine;
using Unity.Services.LevelPlay;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    private LevelPlayBannerAd bannerAd;

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
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogError($"[AdManager] LevelPlay Init Failed: {error}");
    }

    private void SetupBanner()
    {
        var config = new LevelPlayBannerAd.Config.Builder()
            .SetPosition(LevelPlayBannerPosition.BottomCenter)
            .SetRespectSafeArea(false)   // 🔥 THIS moves it lower
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
}
