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
    private int texSize = 256; // smaller for performance
    private float currentAlphaMultiplier = 1f;

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
        UpdateFogTexture();
    }

    private void CreateFogTexture()
    {
        fogTexture = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        fogTexture.filterMode = FilterMode.Bilinear;
        fogTexture.wrapMode = TextureWrapMode.Clamp;

        if (fogImage != null)
            fogImage.texture = fogTexture;
    }

    private void UpdateFogTexture()
    {
        if (fogImage == null || mainCamera == null) return;

        float orthoSize = mainCamera.orthographicSize;
        float aspect = mainCamera.aspect;

        // Radius as fraction of screen height
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

                // Distance in normalized screen space, accounting for aspect ratio
                float dx = (u - centerX) * aspect;
                float dy = v - centerY;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Normalize by radius fraction
                float normDist = dist / radiusFraction;

                float alpha;
                if (normDist < 0.5f)
                    alpha = 0f; // clear center
                else if (normDist < 1f)
                    alpha = Mathf.SmoothStep(0f, 1f, (normDist - 0.5f) / 0.5f);
                else
                    alpha = 1f; // fully dark

                alpha = Mathf.Clamp01(alpha * baseAlpha);
                fogTexture.SetPixel(x, y, new Color(fogColor.r, fogColor.g, fogColor.b, alpha));
            }
        }

        fogTexture.Apply();
    }

    public void SetFullBlack()
    {
        if (fogTexture == null) return;
        Color black = Color.black;
        for (int x = 0; x < texSize; x++)
            for (int y = 0; y < texSize; y++)
                fogTexture.SetPixel(x, y, black);
        fogTexture.Apply();
    }

    public void RestoreNormalFog()
    {
        currentAlphaMultiplier = 1f;
    }
}
