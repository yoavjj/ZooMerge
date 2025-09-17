using UnityEngine;
using UnityEngine.AddressableAssets;

public class BallSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private AddressableInstantiator instantiator;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private BallPicker picker;
    [SerializeField] private Transform ignoredSpawnChild;

    private void Start()
    {
        SpawnCircle();
    }

    public void SpawnCircle() => SpawnCircleInternal(null);

    public void SpawnCircleAtX(float x) => SpawnCircleInternal(x);

    private void SpawnCircleInternal(float? overrideX)
    {
        if (!CanSpawnActiveBall()) return;

        if (instantiator == null)
        {
            Debug.LogError("BallSpawner: No AddressableInstantiator assigned!");
            return;
        }

        var pos = spawnPoint != null ? spawnPoint.position : transform.position;
        if (overrideX.HasValue) pos.x = overrideX.Value;

        GameObject go = null;
        string why = "";

        if (picker != null && picker.TryPickRandomEntry(out var entry, out why))
        {
            go = BallFactoryAddressables.Instance.SpawnEntry(entry, pos, CircleDragInput.Instance?.spawnContainer);
        }
        else
        {
            Debug.LogWarning("[BallSpawner] Picker returned no valid entry. Falling back to default ball.");
            go = BallFactoryAddressables.Instance.SpawnLevel(BallType.Bug, 0, pos, CircleDragInput.Instance?.spawnContainer);
        }

#if UNITY_EDITOR
        if (go == null)
        {
            var msg = string.IsNullOrEmpty(why) ? "Picker returned null." : why;
            Debug.LogWarning($"BallSpawner: {msg} Falling back to default _ballPrefab.");
        }
#endif

        if (go != null)
        {
            var controller = go.GetComponentInChildren<CircleDropController>();
            if (controller != null)
            {
                CircleDragInput.Instance?.SetActiveBall(controller);
                controller.PlayIntroNew();
            }
        }
    }

    private bool CanSpawnActiveBall()
    {
        var input = CircleDragInput.Instance;
        if (input == null) return true;

        var sc = input.spawnContainer;
        if (sc == null) return true;

        for (int i = 0; i < sc.childCount; i++)
        {
            var child = sc.GetChild(i);
            if (ignoredSpawnChild != null && child == ignoredSpawnChild)
                continue; // ignore your art/marker object

            // any other child present means an active ball exists
            Debug.LogWarning("[BallSpawner] spawnContainer not empty. Skipping spawn.");
            return false;
        }
        return true;
    }
}
