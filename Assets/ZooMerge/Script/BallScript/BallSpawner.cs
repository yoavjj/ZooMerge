using UnityEngine;
using UnityEngine.AddressableAssets;

public class BallSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private AddressableInstantiator instantiator;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private BallPicker picker;

    private void Start()
    {
        SpawnCircle();
    }

    public void SpawnCircle()
    {
        if (instantiator == null)
        {
            Debug.LogError("BallSpawner: No AddressableInstantiator assigned!");
            return;
        }

        var pos = spawnPoint != null ? spawnPoint.position : transform.position;

        AssetReferenceGameObject prefabRef = null;
        string why = "";
        if (picker != null)
        {
            picker.TryPickRandom(out prefabRef, out why);
        }

        if (prefabRef != null)
        {
            instantiator.SpawnAssetAt(prefabRef, pos);
        }
        else
        {
            instantiator.SpawnBallAt(pos);
#if UNITY_EDITOR
            var msg = string.IsNullOrEmpty(why) ? "Picker returned null." : why;
            Debug.LogWarning($"BallSpawner: {msg} Falling back to default _ballPrefab.");
#endif
        }
    }
}
