using UnityEditor;

/// <summary>
/// Any FBX imported under Assets/Models/ gets its materials extracted to real, external
/// material assets with textures auto-searched from sibling folders (recursively) — the
/// scripted equivalent of manually selecting the model, opening the Materials tab, and
/// clicking "Extract Textures" / "Extract Materials". Without this, Unity's default import
/// mode embeds materials in the model with no textures wired up, so freshly imported
/// FBX+textures bundles (Sketchfab-style downloads) show up pink/untextured until someone
/// does that by hand.
/// </summary>
public class ModelTexturePostprocessor : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        if (!assetPath.Replace('\\', '/').Contains("Assets/Models/")) return;

        var importer = (ModelImporter)assetImporter;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.materialLocation = ModelImporterMaterialLocation.External;
        importer.materialSearch = ModelImporterMaterialSearch.RecursiveUp;

        // Character models get a Humanoid rig so Unity generates an Avatar from their skeleton —
        // required for Animator-driven animation. Architectural models (walls/floors/etc.) don't
        // have a biped skeleton, so leave those on the default import (Generic/None).
        if (assetPath.Replace('\\', '/').Contains("Assets/Models/Resources/StudentChan/"))
        {
            importer.animationType = ModelImporterAnimationType.Human;
        }
    }
}
