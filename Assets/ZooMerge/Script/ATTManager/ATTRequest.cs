using UnityEngine;

#if UNITY_IOS
using Unity.Advertisement.IosSupport;
using UnityEngine.iOS;
#endif

public class ATTRequest : MonoBehaviour
{
    private void Awake()
    {
#if UNITY_IOS
        var status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
        Debug.Log("📣 ATT Status: " + status.ToString());

        if (status == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
        {
            Debug.Log("📣 Requesting ATT permission...");
            ATTrackingStatusBinding.RequestAuthorizationTracking();
        }

        Invoke(nameof(LogIDFA), 2f);
#endif
    }

    private void LogIDFA()
    {
#if UNITY_IOS
        string idfa = Device.advertisingIdentifier;
        Debug.Log("🍏 IDFA: " + idfa);
#endif
    }
}
