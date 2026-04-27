using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fog-of-war effect: radial visibility mask centered on the player.
/// Starts very tight (can barely see), expands when lantern is picked up.
/// </summary>
public class FogOfWar : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;
    public Camera mainCamera;

    [Header("Fog Settings")]
    public float viewRadius = 2f; // starts very small, lantern expands it
    public Color fogColor = new Color(0f, 0f, 0f, 1f);

    [Header("UI References")]
    public RawImage fogImage;

    private Texture2D fogTexture;
    private int texSize = 64;
    private Color[] fogPixels;
    private float currentAlphaMultiplier = 1f;
    private float updateInterval = 0.05f; // update 20 times per second max
    private float updateTimer = 0f;

    public float AlphaMultiplier
    {
        get => currentAlphaMultiplier;
        set => currentAlphaMultiplier = Mathf.Clamp(value, 0f, 2f);
    }

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        CreateFogTexture();
    }

    private void LateUpdate()
    {
        updateTimer -= Time.deltaTime;
        if (updateTimer > 0f) return;
        updateTimer = updateInterval;
        UpdateFogTexture();
    }

    private void CreateFogTexture()
    {
        fogTexture = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        fogTexture.filterMode = FilterMode.Bilinear;
        fogTexture.wrapMode = TextureWrapMode.Clamp;
        fogPixels = new Color[texSize * texSize]; // preallocate

        if (fogImage != null)
            fogImage.texture = fogTexture;
    }

    private void UpdateFogTexture()
    {
        if (fogImage == null || mainCamera == null) return;

        float orthoSize = mainCamera.orthographicSize;
        float aspect = mainCamera.aspect;
        float radiusFraction = viewRadius / (orthoSize * 2f);

        float centerX = 0.5f;
        float centerY = 0.5f;

        if (playerTransform != null)
        {
            Vector3 vp = mainCamera.WorldToViewportPoint(playerTransform.position);
            centerX = vp.x;
            centerY = vp.y;
        }

        float baseAlpha = fogColor.a * currentAlphaMultiplier;

        for (int x = 0; x < texSize; x++)
        {
            for (int y = 0; y < texSize; y++)
            {
                float u = (float)x / texSize;
                float v = (float)y / texSize;

                float dx = (u - centerX) * aspect;
                float dy = v - centerY;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float normDist = dist / radiusFraction;

                float alpha;
                if (normDist < 0.5f)
                    alpha = 0f;
                else if (normDist < 1f)
                    alpha = Mathf.SmoothStep(0f, 1f, (normDist - 0.5f) / 0.5f);
                else
                    alpha = 1f;

                alpha = Mathf.Clamp01(alpha * baseAlpha);
                fogPixels[y * texSize + x] = new Color(fogColor.r, fogColor.g, fogColor.b, alpha);
            }
        }

        fogTexture.SetPixels(fogPixels); // bulk set — much faster
        fogTexture.Apply();
    }

    public void SetFullBlack()
    {
        if (fogTexture == null) return;
        for (int i = 0; i < fogPixels.Length; i++)
            fogPixels[i] = Color.black;
        fogTexture.SetPixels(fogPixels);
        fogTexture.Apply();
    }

    public void RestoreNormalFog()
    {
        currentAlphaMultiplier = 1f;
    }
}
