using UnityEngine;
public interface IBallFactory
{
    /// <summary>
    /// Spawns a ball of the given level at the specified world position.
    /// Optionally accepts a parent transform to attach the spawned object.
    /// </summary>
    BallInfo SpawnLevel(BallType type, int level, Vector3 position, Transform parentOverride = null);

    /// <summary>
    /// Destroys or despawns the given ball instance.
    /// </summary>
    void Despawn(GameObject go);
}
