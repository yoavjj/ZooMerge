using System.Collections;
using UnityEngine;

public class EditorMainBootstrap : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("Assign the same prefab you use in Splash")]
    [SerializeField] private AdManager adManagerPrefab;

    private IEnumerator Start()
    {
        // 1) Local fast resume (same as Splash)
        GameInventory.Instance.LoadFromPrefs();

        int g = PlayerProgress.LastGalaxyId;
        int l = PlayerProgress.LastLevelInGalaxy;
        int e = PlayerProgress.LastEnemyIndex;

        MergeLevelManager.SetProgress(g, l, e);

        // 2) Ensure AdManager exists (ATTRequest is on the same prefab)
        if (adManagerPrefab != null && AdManager.Instance == null)
            Instantiate(adManagerPrefab);

        // 3) Firebase + RemoteConfig + MergeLevelManager.Initialize happens inside FirebaseInitializer
        bool firebaseReady = false;
        FirebaseInitializer.WaitForFirebase(
            onReady: () => { firebaseReady = true; },
            onError: err =>
            {
                Debug.LogError($"[EditorMainBootstrap] Firebase failed: {err}");
                firebaseReady = true; // still continue offline
            }
        );

        // Wait until Firebase is ready (and JSON is parsed/initialized inside FirebaseInitializer)
        while (!firebaseReady)
            yield return null;

        // 4) Now sync progress from Firestore (same idea as Splash)
        bool synced = false;
        CloudSaveManager.SyncProgressFromCloud(() => synced = true);

        while (!synced)
            yield return null;

        Debug.Log("[EditorMainBootstrap] Boot complete (local + firebase + cloud progress sync).");
    }
#endif
}