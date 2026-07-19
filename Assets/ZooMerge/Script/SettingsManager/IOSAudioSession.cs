using System.Runtime.InteropServices;
using UnityEngine;

public static class IOSAudioSession
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void SetGameAudioPlaybackMode();
#endif

    public static void EnablePlaybackInSilentMode()
    {
#if UNITY_IOS && !UNITY_EDITOR
        SetGameAudioPlaybackMode();
#endif
    }
}