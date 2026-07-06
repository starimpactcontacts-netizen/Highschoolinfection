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
        bodyVisual.transform.localPosition = charController.center; // match the CharacterController's capsule, not the transform origin
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

        // NOTE: This intentionally does NOT touch the "School" greybox object
        // (built by Tools → Generate School Greybox) or the imported anime
        // classroom. It only (re)creates the Player and camera so you can run
        // it any time without wiping your level.

        EditorUtility.DisplayDialog("Scene Setup Complete!",
            "Player + over-the-shoulder camera ready.\n\n" +
            "Controls:\n" +
            "WASD - Move (relative to camera)\n" +
            "Mouse - Orbit camera\n" +
            "Shift - Run\n" +
            "Esc - Release mouse / Click - recapture\n\n" +
            "Press Play to test!", "OK");
    }
}
