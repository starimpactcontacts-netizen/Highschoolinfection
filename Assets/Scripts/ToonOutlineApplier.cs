using UnityEngine;

/// <summary>
/// Adds a black cel-shading outline to a character by duplicating each renderer it finds into a
/// sibling GameObject using the inverted-hull Custom/ToonOutline shader (Assets/Shaders/ToonOutline.shader)
/// — the character's own materials are never touched or replaced, so this is safe to run on
/// StudentChan without disturbing her already-fixed skin/hair/uniform textures.
/// </summary>
public static class ToonOutlineApplier
{
    public static void Apply(GameObject root, Color outlineColor, float outlineWidth = 0.0025f)
    {
        Shader shader = Shader.Find("Custom/ToonOutline");
        if (shader == null)
        {
            Debug.LogWarning("[ToonOutlineApplier] Shader 'Custom/ToonOutline' not found — skipping outline.");
            return;
        }

        var material = new Material(shader);
        material.SetColor("_OutlineColor", outlineColor);
        material.SetFloat("_OutlineWidth", outlineWidth);

        int count = 0;
        foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (smr.sharedMesh == null) continue;

            var outlineGO = new GameObject(smr.gameObject.name + "_Outline");
            outlineGO.transform.SetParent(smr.transform.parent, false);
            outlineGO.transform.localPosition = smr.transform.localPosition;
            outlineGO.transform.localRotation = smr.transform.localRotation;
            outlineGO.transform.localScale = smr.transform.localScale;

            var outlineRenderer = outlineGO.AddComponent<SkinnedMeshRenderer>();
            outlineRenderer.sharedMesh = smr.sharedMesh;
            outlineRenderer.bones = smr.bones;
            outlineRenderer.rootBone = smr.rootBone;
            outlineRenderer.localBounds = smr.localBounds;
            outlineRenderer.sharedMaterial = material;
            count++;
        }

        foreach (var mr in root.GetComponentsInChildren<MeshRenderer>())
        {
            var mf = mr.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            var outlineGO = new GameObject(mr.gameObject.name + "_Outline");
            outlineGO.transform.SetParent(mr.transform, false);

            var outlineFilter = outlineGO.AddComponent<MeshFilter>();
            outlineFilter.sharedMesh = mf.sharedMesh;

            var outlineRenderer = outlineGO.AddComponent<MeshRenderer>();
            outlineRenderer.sharedMaterial = material;
            count++;
        }

        if (count == 0)
            Debug.LogWarning("[ToonOutlineApplier] No renderers found under " + root.name + " — no outline added.");
    }
}
