public static class AdConfig
{
    public static string AppKey => GetAppKey();
    public static string BannerAdUnitId => GetBannerAdUnitId();
    public static string InterstitalAdUnitId => GetInterstitialAdUnitId();
    public static string RewardedVideoAdUnitId => GetRewardedVideoAdUnitId();

    static string GetAppKey()
    {
#if UNITY_ANDROID
        return "2469f9045";
#elif UNITY_IPHONE
            return "8545d445";
#else
            return "unexpected_platform";
#endif
    }

    static string GetBannerAdUnitId()
    {
#if UNITY_ANDROID
        return "9ohv1zkq2meeb6hh"; // ✅ Real Banner ID from dashboard
#elif UNITY_IPHONE
    return "your_ios_banner_id_here";
#else
    return "unexpected_platform";
#endif
    }

    static string GetInterstitialAdUnitId()
    {
#if UNITY_ANDROID
        return "d8ozb9ns46svet5m"; // ✅ Real Interstitial ID
#elif UNITY_IPHONE
    return "your_ios_interstitial_id_here";
#else
    return "unexpected_platform";
#endif
    }

    static string GetRewardedVideoAdUnitId()
    {
#if UNITY_ANDROID
        return "2pjw7sdwb2tik23h"; // ✅ Real Rewarded ID
#elif UNITY_IPHONE
    return "your_ios_rewarded_id_here";
#else
    return "unexpected_platform";
#endif
    }

}
