using UnityEngine;

public class EditorMainBootstrap : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("Assign the same prefab you use in Splash")]
    [SerializeField] private AdManager adManagerPrefab;

    private void Awake()
    {
        // If you pressed Play from Main, Firebase might not have been kicked off yet.
        FirebaseInitializer.WaitForFirebase(
            onReady: () => { },
            onError: err => Debug.LogError($"[EditorMainBootstrap] Firebase failed: {err}")
        );

        // Ensure AdManager exists (ATTRequest is on the same prefab)
        if (adManagerPrefab != null && AdManager.Instance == null)
        {
            Instantiate(adManagerPrefab);
        }
    }
#endif
}
