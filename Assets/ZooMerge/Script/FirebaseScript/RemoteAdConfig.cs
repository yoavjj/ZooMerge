using Firebase.RemoteConfig;
using UnityEngine;

public static class RemoteAdConfig
{
    public static string AppKey =>
#if UNITY_ANDROID
        FirebaseRemoteConfig.DefaultInstance.GetValue("ad_app_key_android").StringValue;
#elif UNITY_IOS
        FirebaseRemoteConfig.DefaultInstance.GetValue("ad_app_key_ios").StringValue;
#else
        "unexpected_platform";
#endif

    public static string BannerAdUnitId =>
#if UNITY_ANDROID
        FirebaseRemoteConfig.DefaultInstance.GetValue("ad_banner_android").StringValue;
#elif UNITY_IOS
        FirebaseRemoteConfig.DefaultInstance.GetValue("ad_banner_ios").StringValue;
#else
        "unexpected_platform";
#endif

    public static string InterstitialAdUnitId =>
#if UNITY_ANDROID
        FirebaseRemoteConfig.DefaultInstance.GetValue("ad_interstitial_android").StringValue;
#elif UNITY_IOS
        FirebaseRemoteConfig.DefaultInstance.GetValue("ad_interstitial_ios").StringValue;
#else
        "unexpected_platform";
#endif

    public static string RewardedAdUnitId =>
#if UNITY_ANDROID
        FirebaseRemoteConfig.DefaultInstance.GetValue("ad_rewarded_android").StringValue;
#elif UNITY_IOS
        FirebaseRemoteConfig.DefaultInstance.GetValue("ad_rewarded_ios").StringValue;
#else
        "unexpected_platform";
#endif
}
