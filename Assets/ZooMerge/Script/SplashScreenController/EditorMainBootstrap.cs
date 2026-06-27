using System.Collections;
using UnityEngine;

public class EditorMainBootstrap : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("Assign the same prefabs you use in Splash")]
    [SerializeField] private AdManager adManagerPrefab;
    [SerializeField] private AudioManager audioManagerPrefab;

    private IEnumerator Start()
    {
        // 1) Local fast resume
        GameInventory.Instance.LoadFromPrefs();

        int g = PlayerProgress.LastGalaxyId;
        int l = PlayerProgress.LastLevelInGalaxy;
        int e = PlayerProgress.LastEnemyIndex;

        MergeLevelManager.SetProgress(g, l, e);

        // 2) Ensure persistent managers exist
        if (audioManagerPrefab != null && AudioManager.Instance == null)
        {
            Instantiate(audioManagerPrefab);
        }

        if (adManagerPrefab != null && AdManager.Instance == null)
        {
            Instantiate(adManagerPrefab);
        }

        // 3) Firebase + Remote Config
        bool firebaseReady = false;

        FirebaseInitializer.WaitForFirebase(
            onReady: () =>
            {
                firebaseReady = true;
            },
            onError: err =>
            {
                Debug.LogError($"[EditorMainBootstrap] Firebase failed: {err}");
                firebaseReady = true;
            }
        );

        while (!firebaseReady)
            yield return null;

        // 4) Sync progress
        bool synced = false;
        CloudSaveManager.SyncProgressFromCloud(() => synced = true);

        while (!synced)
            yield return null;

        // 5) Sync economy
        bool econSynced = false;
        CloudSaveManager.SyncEconomyFromCloud(() => econSynced = true);

        while (!econSynced)
            yield return null;

        FirebaseInitializer.BootComplete = true;

        var topBar = FindObjectOfType<TopBarMenu>();
        if (topBar != null)
            topBar.RefreshCoins();

        Debug.Log(
            "[EditorMainBootstrap] Boot complete " +
            "(audio + ads + firebase + cloud progress + economy)."
        );
    }
#endif
}