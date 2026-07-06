using UnityEngine;

/// <summary>
/// Finds ground/metal/window-like renderers on the loaded map and swaps their materials to the
/// wet/rain shaders (Assets/Shaders/WetSurface.shader, Assets/Shaders/RainWindow.shader) —
/// there's no per-object "this is the floor" flag on an arbitrary imported FBX, so this matches
/// by object/material name instead. Carries over each material's existing _MainTex/_Color so
/// none of the already-correct textures need re-linking (the GameBootstrap_Diagnostics.txt dump
/// showed this map's actual ground plane is a material literally named "Paint - Metallic (Green)",
/// hence "green" being a ground hint below — it's this map's specific naming, not a general rule).
/// </summary>
public static class WetSurfaceApplier
{
    static readonly string[] GroundHints = { "floor", "ground", "concrete", "tile", "gravel", "sidewalk", "green" };
    static readonly string[] MetalHints = { "metal", "silver", "steel", "chrome", "aluminum" };
    static readonly string[] WindowHints = { "window", "glass", "wndw" };

    public static void Apply(GameObject root)
    {
        Shader wetShader = Shader.Find("Custom/WetSurface");
        Shader rainShader = Shader.Find("Custom/RainWindow");
        if (wetShader == null || rainShader == null)
        {
            Debug.LogWarning("[WetSurfaceApplier] Wet/rain shaders not found — skipping.");
            return;
        }

        int wetCount = 0, metalCount = 0, windowCount = 0;

        foreach (var renderer in root.GetComponentsInChildren<Renderer>())
        {
            string objName = renderer.gameObject.name.ToLowerInvariant();
            var mats = renderer.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < mats.Length; i++)
            {
                var mat = mats[i];
                if (mat == null) continue;
                string combined = objName + " " + mat.name.ToLowerInvariant();

                Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;

                if (ContainsAny(combined, WindowHints))
                {
                    var newMat = new Material(rainShader) { name = mat.name + "_Rain" };
                    if (mainTex != null) newMat.SetTexture("_MainTex", mainTex);
                    newMat.SetColor("_Color", color);
                    mats[i] = newMat;
                    changed = true; windowCount++;
                }
                else if (ContainsAny(combined, MetalHints))
                {
                    var newMat = new Material(wetShader) { name = mat.name + "_Wet" };
                    if (mainTex != null) newMat.SetTexture("_MainTex", mainTex);
                    newMat.SetColor("_Color", color);
                    newMat.SetFloat("_Wetness", 0.95f);
                    mats[i] = newMat;
                    changed = true; metalCount++;
                }
                else if (ContainsAny(combined, GroundHints))
                {
                    var newMat = new Material(wetShader) { name = mat.name + "_Wet" };
                    if (mainTex != null) newMat.SetTexture("_MainTex", mainTex);
                    newMat.SetColor("_Color", color);
                    newMat.SetFloat("_Wetness", 0.75f);
                    mats[i] = newMat;
                    changed = true; wetCount++;
                }
            }

            if (changed) renderer.sharedMaterials = mats;
        }

        Debug.Log($"[WetSurfaceApplier] Wet shader: {wetCount} ground + {metalCount} metal renderers. Rain shader: {windowCount} window renderers.");
    }

    static bool ContainsAny(string haystack, string[] needles)
    {
        foreach (var n in needles)
            if (haystack.Contains(n)) return true;
        return false;
    }
}
