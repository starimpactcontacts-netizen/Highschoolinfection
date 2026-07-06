using System.Linq;
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
    // Character models get a Humanoid rig so Unity generates an Avatar from their skeleton —
    // required for Animator-driven animation (SimpleHumanoidWalkAnimator specifically checks
    // animator.isHuman and disables itself otherwise). Architectural models (walls/floors/etc.)
    // don't have a biped skeleton, so leave those on the default import (Generic/None). Former
    // Player models (StudentChan, MitteltCharacter) are kept in this list even after being swapped
    // out — if one ever gets wired back in, it still needs this to work.
    static readonly string[] HumanoidCharacterFolders =
    {
        "Assets/Models/Resources/StudentChan/",
        "Assets/Models/Resources/MitteltCharacter/",
        "Assets/Models/Resources/OsanaCharacter/",
    };

    void OnPreprocessModel()
    {
        string normalizedPath = assetPath.Replace('\\', '/');
        if (!normalizedPath.Contains("Assets/Models/")) return;

        var importer = (ModelImporter)assetImporter;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.materialLocation = ModelImporterMaterialLocation.External;
        importer.materialSearch = ModelImporterMaterialSearch.RecursiveUp;

        if (HumanoidCharacterFolders.Any(normalizedPath.Contains))
        {
            importer.animationType = ModelImporterAnimationType.Human;
        }
    }
}
