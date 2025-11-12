using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ShipRayMarker : MonoBehaviour
{
    [Header("Ray Settings")]
    [SerializeField] private float rayLength = 3f;
    [SerializeField] private Vector2 offset = Vector2.zero;
    [SerializeField] private Color rayColor = Color.cyan;
    [SerializeField] private Color flashColor = Color.yellow;
    [SerializeField] private float flashRadius = 0.15f;
    [SerializeField] private float spotlightSmoothTime = 0.05f;
    private Vector3 spotlightVelocity = Vector3.zero;

    [Header("Raycast Settings")]
    [SerializeField] private LayerMask ballLayer;

    [Header("Highlight Settings")]
    [SerializeField] private GameObject spotlightPrefab;
    [SerializeField] private Transform spotlightContainer;
    private GameObject spotlightInstance;

    [Header("Spotlight Intensity Control")]
    [SerializeField] private float spotlightMaxIntensity = 1f;
    [SerializeField] private float spotlightMinIntensity = 1f;
    [SerializeField] private float spotlightFadeDuration = 0.2f;
    private Light2D spotlightLight;
    private Coroutine spotlightFadeRoutine;

    private float currentRayLength;

    public Vector3 RayStart => transform.position + (Vector3)offset;
    public Vector3 RayEnd => RayStart + Vector3.down * currentRayLength;

    // ======================================================
    // MAIN RAYCAST LOGIC
    // ======================================================
    public bool TryGetBallHit(out Collider2D hit)
    {
        var start = RayStart;
        var direction = Vector2.down;

        float thickness = 0.1f;
        RaycastHit2D rayHit = Physics2D.CircleCast(start, thickness, direction, rayLength, ballLayer);

        hit = rayHit.collider;

        if (hit != null)
        {
            currentRayLength = rayHit.distance;
            Debug.DrawRay(start, direction * currentRayLength, Color.green, 1f);
            HighlightBall(rayHit.point);
            return true;
        }

        // 🧠 Check if we hit an Enclosure before fading
        RaycastHit2D anyHit = Physics2D.CircleCast(start, thickness, direction, rayLength);
        if (anyHit.collider != null && anyHit.collider.gameObject.layer == LayerMask.NameToLayer("Enclosure"))
        {
            currentRayLength = anyHit.distance;
            Debug.DrawRay(start, direction * currentRayLength, Color.blue, 1f);
            return false; // Don't call DisableHighlight
        }

        currentRayLength = rayLength;
        Debug.DrawRay(start, direction * currentRayLength, Color.red, 1f);
        DisableHighlight(); // Only fade out if nothing or non-enclosure hit
        return false;
    }

    // ======================================================
    // SPOTLIGHT HANDLING
    // ======================================================
    private void HighlightBall(Vector3 hitPoint)
    {
        if (spotlightPrefab == null) return;

        // create instance if missing
        if (spotlightInstance == null)
        {
            spotlightInstance = Instantiate(spotlightPrefab, spotlightContainer != null ? spotlightContainer : null);
        }

        spotlightInstance.transform.SetParent(spotlightContainer, true);

        // ensure we have a Light2D reference
        if (spotlightLight == null)
            spotlightLight = spotlightInstance.GetComponentInChildren<Light2D>();

        // smoothly move spotlight
        Vector3 spotlightPosition = hitPoint;
        Vector3 targetPosition = new Vector3(spotlightPosition.x, spotlightPosition.y, -1f);
        spotlightInstance.transform.position = Vector3.SmoothDamp(
            spotlightInstance.transform.position,
            targetPosition,
            ref spotlightVelocity,
            spotlightSmoothTime
        );

        // fade in the light
        StartSpotlightFade(spotlightMaxIntensity);
    }

    public void DisableHighlight()
    {
        // fade out instead of instantly disabling
        StartSpotlightFade(spotlightMinIntensity);
    }

    // ======================================================
    // FADE LOGIC (NO UPDATE USED)
    // ======================================================
    private void StartSpotlightFade(float targetIntensity)
    {
        if (spotlightInstance == null) return;
        if (spotlightLight == null)
            spotlightLight = spotlightInstance.GetComponentInChildren<Light2D>();

        if (spotlightFadeRoutine != null)
            StopCoroutine(spotlightFadeRoutine);

        spotlightFadeRoutine = StartCoroutine(FadeSpotlightIntensity(targetIntensity));
    }

    private System.Collections.IEnumerator FadeSpotlightIntensity(float target)
    {
        spotlightInstance.SetActive(true);

        float start = spotlightLight.intensity;
        float elapsed = 0f;

        while (elapsed < spotlightFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / spotlightFadeDuration);
            spotlightLight.intensity = Mathf.Lerp(start, target, t);
            yield return null;
        }

        spotlightLight.intensity = target;

        // fully fade-out → disable spotlight object
        // if (Mathf.Approximately(target, 0f))
        //     spotlightInstance.SetActive(false);
    }

    // ======================================================
    // GIZMOS
    // ======================================================
    private void OnDrawGizmos()
    {
        Gizmos.color = rayColor;
        Gizmos.DrawLine(RayStart, RayEnd);
        Gizmos.color = flashColor;
        Gizmos.DrawSphere(RayEnd, flashRadius);
    }
}
