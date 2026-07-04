using UnityEngine;
using UnityEditor;

/// <summary>
/// Simple editor script that auto-generates a third-person demo scene.
/// Run via: Window → Generate Demo Scene
/// </summary>
public class SceneSetup
{
    [MenuItem("Window/Generate Demo Scene")]
    public static void GenerateScene()
    {
        // Delete existing Player if it exists
        var existingPlayer = GameObject.Find("Player");
        if (existingPlayer != null)
            Object.DestroyImmediate(existingPlayer);

        // --- Create Player ---
        GameObject playerGO = new GameObject("Player");
        // Spawn at the front gate on the courtyard, facing the school (+Z).
        playerGO.transform.position = new Vector3(0, 1f, -30f);

        // Add CharacterController
        CharacterController charController = playerGO.AddComponent<CharacterController>();
        charController.height = 2f;
        charController.radius = 0.5f;
        charController.center = new Vector3(0, 1, 0);

        // Add PrototypePlayerController
        playerGO.AddComponent<PrototypePlayerController>();

        // Add visual (capsule)
        GameObject bodyVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        bodyVisual.name = "BodyVisual";
        bodyVisual.transform.SetParent(playerGO.transform);
        bodyVisual.transform.localPosition = Vector3.zero;
        bodyVisual.transform.localScale = new Vector3(1, 1, 1);
        Object.DestroyImmediate(bodyVisual.GetComponent<Collider>());

        // --- Setup Main Camera ---
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // Strip any old camera scripts so we don't stack duplicates.
            var oldIso = mainCamera.GetComponent<IsometricFollowCamera>();
            if (oldIso != null) Object.DestroyImmediate(oldIso);
            var oldTpc = mainCamera.GetComponent<ThirdPersonCamera>();
            if (oldTpc != null) Object.DestroyImmediate(oldTpc);

            var cam = mainCamera.gameObject.AddComponent<ThirdPersonCamera>();
            cam.SetTarget(playerGO.transform);
        }

        // --- Spawn Multiple Classrooms ---
        SpawnClassrooms();

        EditorUtility.DisplayDialog("Scene Setup Complete!",
            "Demo scene generated!\n\n" +
            "Player + over-the-shoulder camera ready.\n" +
            "Anime classrooms spawned around the school.\n\n" +
            "Controls:\n" +
            "WASD - Move (relative to camera)\n" +
            "Mouse - Orbit camera\n" +
            "Shift - Run\n" +
            "Esc - Release mouse / Click - recapture\n\n" +
            "Press Play to test!", "OK");
    }

    private static void SpawnClassrooms()
    {
        // Delete existing school if it exists
        var existingSchool = GameObject.Find("School");
        if (existingSchool != null)
            Object.DestroyImmediate(existingSchool);

        GameObject schoolParent = new GameObject("School");

        // Load the classroom model
        var classroomAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AnimeclassroomModel.usdz");
        if (classroomAsset == null)
        {
            Debug.LogWarning("AnimeclassroomModel.usdz not found. Skipping classroom spawn.");
            return;
        }

        // Spawn classrooms in a grid pattern (left wing, right wing, multiple floors)
        Vector3[] classroomPositions = new Vector3[]
        {
            new Vector3(-8f, 0f, 5f),      // Left wing, front
            new Vector3(-8f, 0f, 15f),     // Left wing, middle
            new Vector3(-8f, 0f, 25f),     // Left wing, back
            new Vector3(8f, 0f, 5f),       // Right wing, front
            new Vector3(8f, 0f, 15f),      // Right wing, middle
            new Vector3(8f, 0f, 25f),      // Right wing, back
            new Vector3(0f, 0f, 35f),      // Back building
        };

        foreach (Vector3 pos in classroomPositions)
        {
            GameObject classroomInstance = PrefabUtility.InstantiatePrefab(classroomAsset, schoolParent.transform) as GameObject;
            if (classroomInstance != null)
            {
                classroomInstance.transform.position = pos;
            }
        }
    }
}
