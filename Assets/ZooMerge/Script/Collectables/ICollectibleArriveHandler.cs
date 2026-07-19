using UnityEngine;

public interface ICollectibleArriveHandler
{
    void HandleArrive(int amount, string collectibleId, GameObject source);
}
