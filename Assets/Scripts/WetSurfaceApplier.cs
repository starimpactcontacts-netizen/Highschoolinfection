using UnityEngine;

/// <summary>
/// Puts every renderer on the loaded map through one of two cel-shaded shaders (Assets/Shaders/
/// WetSurface.shader for opaque surfaces, Assets/Shaders/RainWindow.shader for glass) so the whole
/// environment reads as the same stylized/toon art direction as the character, instead of a mix
/// of default-imported PBR materials plus a few hand-picked "wet" ones. There's no per-object
/// "this is the floor/wall/metal" flag on an arbitrary imported FBX, so this matches by
/// object/material name — anything that doesn't match a specific category still gets the
/// generic "building surface" treatment, which is what makes this a *consistent* pass rather than
/// a partial one. Carries over each material's existing _MainTex/_Color so none of the
/// already-correct textures need re-linking (the GameBootstrap_Diagnostics.txt dump showed this
/// map's actual ground plane is a material literally named "Paint - Metallic (Green)", hence
/// "green" being a ground hint below — it's this map's specific naming, not a general rule).
/// </summary>
public static class WetSurfaceApplier
{
    static readonly string[] GroundHints = { "floor", "ground", "concrete", "tile", "gravel", "sidewalk", "grass", "green" };
    static readonly string[] MetalHints = { "metal", "silver", "steel", "chrome", "aluminum", "railing" };
    static readonly string[] WindowHints = { "window", "glass", "wndw" };

    // Per-category tint: pushes everything toward the requested grey/dark-blue/desaturated
    // storm palette, distinct enough per surface type to still read as different materials.
    static readonly Color GroundTint = new Color(0.55f, 0.62f, 0.55f);
    static readonly Color GroundShadowTint = new Color(0.30f, 0.36f, 0.34f);
    static readonly Color MetalTint = new Color(0.60f, 0.63f, 0.68f);
    static readonly Color MetalShadowTint = new Color(0.28f, 0.30f, 0.36f);
    static readonly Color WallTint = new Color(0.75f, 0.78f, 0.85f);
    static readonly Color WallShadowTint = new Color(0.45f, 0.48f, 0.58f);
    static readonly Color WindowShadowTint = new Color(0.5f, 0.55f, 0.65f);

    public static void Apply(GameObject root)
    {
        Shader wetShader = Shader.Find("Custom/WetSurface");
        Shader rainShader = Shader.Find("Custom/RainWindow");
        if (wetShader == null || rainShader == null)
        {
            Debug.LogWarning("[WetSurfaceApplier] Wet/rain shaders not found — skipping.");
            return;
        }

        int wetCount = 0, metalCount = 0, windowCount = 0, wallCount = 0;

        foreach (var renderer in root.GetComponentsInChildren<Renderer>())
        {
            string objName = renderer.gameObject.name.ToLowerInvariant();
            var mats = renderer.sharedMaterials;

            for (int i = 0; i < mats.Length; i++)
            {
                var mat = mats[i];
                if (mat == null) continue;
                string combined = objName + " " + mat.name.ToLowerInvariant();
                Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;

                if (ContainsAny(combined, WindowHints))
                {
                    var newMat = new Material(rainShader) { name = mat.name + "_Rain" };
                    CopyMainTex(mat, newMat);
                    newMat.SetColor("_Color", color);
                    newMat.SetColor("_ShadowTint", WindowShadowTint);
                    mats[i] = newMat;
                    windowCount++;
                }
                else if (ContainsAny(combined, MetalHints))
                {
                    var newMat = new Material(wetShader) { name = mat.name + "_Wet" };
                    CopyMainTex(mat, newMat);
                    newMat.SetColor("_Color", color * MetalTint);
                    newMat.SetColor("_ShadowTint", MetalShadowTint);
                    newMat.SetFloat("_Wetness", 0.95f);
                    mats[i] = newMat;
                    metalCount++;
                }
                else if (ContainsAny(combined, GroundHints))
                {
                    var newMat = new Material(wetShader) { name = mat.name + "_Wet" };
                    CopyMainTex(mat, newMat);
                    newMat.SetColor("_Color", color * GroundTint);
                    newMat.SetColor("_ShadowTint", GroundShadowTint);
                    newMat.SetFloat("_Wetness", 0.8f);
                    mats[i] = newMat;
                    wetCount++;
                }
                else
                {
                    // Generic building surface (walls, doors, brick/concrete trim, anything not
                    // otherwise categorized) — lower wetness than ground puddles, but still cel-shaded
                    // and rain-damp so nothing is left on the old default-imported material.
                    var newMat = new Material(wetShader) { name = mat.name + "_Wet" };
                    CopyMainTex(mat, newMat);
                    newMat.SetColor("_Color", color * WallTint);
                    newMat.SetColor("_ShadowTint", WallShadowTint);
                    newMat.SetFloat("_Wetness", 0.35f);
                    mats[i] = newMat;
                    wallCount++;
                }
            }

            renderer.sharedMaterials = mats;
        }

        Debug.Log($"[WetSurfaceApplier] Ground: {wetCount}, Metal: {metalCount}, Windows: {windowCount}, Walls/generic: {wallCount}.");
    }

    // Copies not just the texture reference but its tiling scale/offset too — Material.SetTexture
    // alone drops those, which would silently undo any tiling set on the source material (e.g. a
    // ground texture meant to repeat 15x across a large plane would default back to a single
    // stretched, blurry copy otherwise).
    static void CopyMainTex(Material from, Material to)
    {
        if (!from.HasProperty("_MainTex")) return;
        Texture tex = from.GetTexture("_MainTex");
        if (tex == null) return;
        to.SetTexture("_MainTex", tex);
        to.SetTextureScale("_MainTex", from.GetTextureScale("_MainTex"));
        to.SetTextureOffset("_MainTex", from.GetTextureOffset("_MainTex"));
    }

    static bool ContainsAny(string haystack, string[] needles)
    {
        foreach (var n in needles)
            if (haystack.Contains(n)) return true;
        return false;
    }
}
