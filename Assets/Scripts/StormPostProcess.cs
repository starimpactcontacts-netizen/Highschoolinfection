using UnityEngine;

/// <summary>
/// Drives the full-screen storm color grade (Assets/Shaders/StormColorGrade.shader) via the
/// legacy Camera.OnRenderImage path — this project uses the Built-in Render Pipeline (no URP
/// package installed), so there's no Volume system available; this is the Built-in RP
/// equivalent of a post-process stack.
///
/// Covers: desaturation, contrast, cold/green tint, vignette, film grain, and a cheap
/// depth-based defocus blur (not true bokeh DOF — a small fixed-pattern blur weighted by how far
/// a pixel's depth is from the focus distance, which reads as "subtle heavy-atmosphere blur"
/// without the cost/complexity of a real depth-of-field pass).
/// </summary>
[RequireComponent(typeof(Camera))]
public class StormPostProcess : MonoBehaviour
{
    [Header("Color")]
    [Range(0f, 1f)] public float desaturation = 0.65f;
    public float contrast = 1.2f;
    [Tooltip("Multiplied into the final color — cold blue-grey with a slight sickly green push.")]
    public Color colorTint = new Color(0.80f, 0.90f, 0.86f);

    [Header("Vignette")]
    [Range(0f, 3f)] public float vignetteIntensity = 0.9f; // tuned so edges read ~20% darker
    [Range(0.01f, 1f)] public float vignetteSmoothness = 0.9f;

    [Header("Film Grain")]
    [Range(0f, 1f)] public float grainIntensity = 0.03f; // shader-space noise amplitude; "0.3" spec value scaled to a sane range

    [Header("Depth Blur")]
    public float focusDistance = 8f;
    public float focusRange = 6f;
    [Range(0f, 1f)] public float blurStrength = 0.35f;

    private Material material;
    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.depthTextureMode |= DepthTextureMode.Depth;

        Shader shader = Shader.Find("Hidden/StormColorGrade");
        if (shader == null)
        {
            Debug.LogWarning("[StormPostProcess] Shader 'Hidden/StormColorGrade' not found — post-processing disabled.");
            enabled = false;
            return;
        }
        material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (material == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        material.SetFloat("_Saturation", desaturation);
        material.SetFloat("_Contrast", contrast);
        material.SetColor("_ColorTint", colorTint);
        material.SetFloat("_VignetteIntensity", vignetteIntensity);
        material.SetFloat("_VignetteSmoothness", vignetteSmoothness);
        material.SetFloat("_GrainIntensity", grainIntensity);
        material.SetFloat("_Time01", Time.time);
        material.SetFloat("_FocusDistance", focusDistance);
        material.SetFloat("_FocusRange", focusRange);
        material.SetFloat("_BlurStrength", blurStrength);

        Graphics.Blit(source, destination, material);
    }
}
