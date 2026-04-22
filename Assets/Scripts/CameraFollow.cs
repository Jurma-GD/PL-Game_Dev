using UnityEngine;
using System.Collections;

/// <summary>
/// Smooth camera follow with support for zoom-in and screen shake effects.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow Settings")]
    public float smoothSpeed = 10f;
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Zoom")]
    public float defaultOrthoSize = 5f;

    private Camera cam;
    private float targetOrthoSize;
    private Vector3 shakeOffset;
    private bool isShaking;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
            targetOrthoSize = defaultOrthoSize;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset + shakeOffset;
        Vector3 smoothed = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
        transform.position = smoothed;

        // Smooth zoom
        if (cam != null && cam.orthographic)
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetOrthoSize, 4f * Time.deltaTime);
    }

    /// <summary>
    /// Smoothly zoom to a target orthographic size.
    /// </summary>
    public void ZoomTo(float orthoSize)
    {
        targetOrthoSize = orthoSize;
    }

    /// <summary>
    /// Reset zoom to default.
    /// </summary>
    public void ResetZoom()
    {
        targetOrthoSize = defaultOrthoSize;
    }

    /// <summary>
    /// Snap the camera immediately to the target (no lerp).
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null) return;
        transform.position = target.position + offset;
    }

    /// <summary>
    /// Start a screen shake effect.
    /// </summary>
    public Coroutine Shake(float duration, float intensity)
    {
        return StartCoroutine(ShakeCoroutine(duration, intensity));
    }

    private IEnumerator ShakeCoroutine(float duration, float intensity)
    {
        isShaking = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float currentIntensity = intensity * (1f - t); // fade out
            shakeOffset = new Vector3(
                Random.Range(-currentIntensity, currentIntensity),
                Random.Range(-currentIntensity, currentIntensity),
                0f);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        shakeOffset = Vector3.zero;
        isShaking = false;
    }
}
