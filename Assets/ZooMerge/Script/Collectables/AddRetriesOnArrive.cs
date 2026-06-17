using UnityEngine;

public class AddRetriesOnArrive : MonoBehaviour, ICollectibleArriveHandler
{
    public void HandleArrive(int amount, string collectibleId, GameObject source)
    {
        PlayerProgress.AddRetries(1, saveToCloud: true);

        Debug.Log($"[AddRetriesOnArrive] Added {amount} retries.");
    }
}