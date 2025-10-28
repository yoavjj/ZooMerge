using UnityEngine;

public static class ParticleEvents
{
    public delegate void ParticleRequest(string effectKey, Vector3 position);
    public static event ParticleRequest OnParticleRequested;

    public static void Request(string effectKey, Vector3 position)
    {
        OnParticleRequested?.Invoke(effectKey, position);
    }
}
