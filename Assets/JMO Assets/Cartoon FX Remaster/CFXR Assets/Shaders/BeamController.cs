using UnityEngine;

public class BeamController : MonoBehaviour
{
    [SerializeField] private Material beamMat;
    [SerializeField] private Transform beamOrigin;
    [SerializeField] private Transform beamDirectionRef;
    [SerializeField] private float beamAngle = 0.35f;
    [SerializeField] private float beamHeight = 6f;
    [SerializeField] private float softness = 0.3f;

    void LateUpdate()
    {
        if (!beamMat) return;

        Vector3 dir = (beamDirectionRef ? beamDirectionRef.forward : -transform.up).normalized;
        beamMat.SetVector("_BeamPos", beamOrigin.position);
        beamMat.SetVector("_BeamDir", dir);
        beamMat.SetFloat("_BeamAngle", beamAngle);
        beamMat.SetFloat("_BeamHeight", beamHeight);
        beamMat.SetFloat("_BeamSoftness", softness);
    }
}

