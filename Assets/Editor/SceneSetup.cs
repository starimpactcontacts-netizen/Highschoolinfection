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
        playerGO.transform.position = Vector3.zero;

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
            mainCamera.gameObject.AddComponent<IsometricFollowCamera>();
            var followCam = mainCamera.GetComponent<IsometricFollowCamera>();

            // Use reflection to set the target
            var targetField = typeof(IsometricFollowCamera).GetField("target",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            targetField?.SetValue(followCam, playerGO.transform);
        }

        EditorUtility.DisplayDialog("Scene Setup Complete!",
            "Demo scene generated!\n\n" +
            "Player and Camera follow system ready.\n\n" +
            "Controls:\n" +
            "WASD - Move\n" +
            "Shift - Run\n\n" +
            "Press Play to test!", "OK");
    }
}
