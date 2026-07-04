using UnityEngine;
using UnityEditor;

/// <summary>
/// Places the imported "Anime Class Room" OBJ model into the scene.
///
/// Run via: Tools → Place Anime Classroom
///
/// This is deliberately NON-DESTRUCTIVE: it never touches the "School"
/// greybox (built by Tools → Generate School Greybox) or the Player. It only
/// clears and rebuilds its own "AnimeClassroom" parent object, so you can run
/// it repeatedly while tuning without wiping your level.
///
/// The model is a large, self-contained room (~38 x 32 x 90 units) with its
/// own baked sky/landscape backdrop, so it is placed once, off to the side of
/// the greybox courtyard, rather than tiled into every blockout cell.
///
/// Model: "Anime Class Room" (https://skfb.ly/oEvMs) by AnixMoonLight,
/// licensed under CC-BY-4.0 (http://creativecommons.org/licenses/by/4.0/).
/// </summary>
public class ClassroomPlacer
{
    const string ModelPath = "Assets/AnimeClassroom/AnimeClassroom.obj";

    // The model's floor sits at roughly Y = -2.1 in local space, so lift it so
    // the floor lands on the ground plane. Placed clear of the greybox (which
    // spans X: -65..65, Z: -60..27) so the two don't intersect.
    static readonly Vector3 PlacePosition = new Vector3(0f, 2.11f, 90f);
    static readonly Vector3 PlaceEuler = new Vector3(0f, 180f, 0f); // face the courtyard
    const float PlaceScale = 1f;

    [MenuItem("Tools/Place Anime Classroom")]
    public static void Place()
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null)
        {
            EditorUtility.DisplayDialog("Model Not Found",
                "Couldn't load:\n" + ModelPath + "\n\n" +
                "Make sure the OBJ imported (check the Project window under " +
                "Assets/AnimeClassroom) and that there are no import errors in the Console.",
                "OK");
            return;
        }

        // Clear only our own previous placement — never the greybox "School".
        var existing = GameObject.Find("AnimeClassroom");
        if (existing != null) Object.DestroyImmediate(existing);

        GameObject parent = new GameObject("AnimeClassroom");

        GameObject instance = PrefabUtility.InstantiatePrefab(model, parent.transform) as GameObject;
        if (instance == null)
        {
            // Fallback for non-prefab model assets.
            instance = Object.Instantiate(model, parent.transform);
        }

        instance.name = "AnimeClassroom_Model";
        instance.transform.position = PlacePosition;
        instance.transform.rotation = Quaternion.Euler(PlaceEuler);
        instance.transform.localScale = Vector3.one * PlaceScale;

        Selection.activeGameObject = parent;
        SceneView.FrameLastActiveSceneView();

        EditorUtility.DisplayDialog("Anime Classroom Placed",
            "Placed the anime classroom under the 'AnimeClassroom' object.\n\n" +
            "Your greybox 'School' map was left untouched.\n\n" +
            "If it looks untextured: select the model in the Project window, open " +
            "the Materials tab in the Inspector, set 'Location' to 'Use External " +
            "Materials (Legacy)' (or Extract Materials/Textures), then Apply.",
            "Nice");
    }
}
