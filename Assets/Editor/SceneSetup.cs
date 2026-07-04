using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor script that auto-generates a complete third-person demo scene.
/// Run via: Window → Generate Demo Scene
/// </summary>
public class SceneSetup
{
    [MenuItem("Window/Generate Demo Scene")]
    public static void GenerateScene()
    {
        // Clear existing objects (optional, comment out if you want to preserve existing stuff)
        var existingPlayer = GameObject.Find("Player");
        if (existingPlayer != null)
            Object.DestroyImmediate(existingPlayer);

        var existingCanvas = GameObject.Find("Canvas");
        if (existingCanvas != null)
            Object.DestroyImmediate(existingCanvas);

        // --- Create Player ---
        GameObject playerGO = new GameObject("Player");
        playerGO.transform.position = Vector3.zero;

        // Add capsule mesh (visual representation)
        CapsuleCollider capsuleCollider = playerGO.AddComponent<CapsuleCollider>();
        capsuleCollider.height = 2f;
        capsuleCollider.radius = 0.5f;

        // Add CharacterController
        CharacterController charController = playerGO.AddComponent<CharacterController>();
        charController.height = 2f;
        charController.radius = 0.5f;
        charController.center = new Vector3(0, 1, 0);

        // Add PrototypePlayerController
        PrototypePlayerController playerController = playerGO.AddComponent<PrototypePlayerController>();
        playerController.GetType().GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(playerController, 4f);

        // Add simple visual (a cube for the body)
        GameObject bodyVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        bodyVisual.name = "BodyVisual";
        bodyVisual.transform.SetParent(playerGO.transform);
        bodyVisual.transform.localPosition = Vector3.zero;
        bodyVisual.transform.localScale = new Vector3(1, 1, 1);
        Object.DestroyImmediate(bodyVisual.GetComponent<Collider>());

        // --- Setup Camera ---
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            IsometricFollowCamera followCamera = mainCamera.gameObject.AddComponent<IsometricFollowCamera>();
            followCamera.GetType().GetField("target", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(followCamera, playerGO.transform);
        }

        // --- Create HUD Canvas ---
        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        // Add CanvasScaler
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Add GraphicRaycaster
        canvasGO.AddComponent<GraphicRaycaster>();

        // --- Create Clock Text (Top Right) ---
        GameObject clockGO = new GameObject("ClockText");
        clockGO.transform.SetParent(canvasGO.transform);
        RectTransform clockRect = clockGO.AddComponent<RectTransform>();
        clockRect.anchorMin = new Vector2(1, 1);
        clockRect.anchorMax = new Vector2(1, 1);
        clockRect.pivot = new Vector2(1, 1);
        clockRect.offsetMin = new Vector2(-200, -60);
        clockRect.offsetMax = Vector2.zero;

        TextMeshProUGUI clockText = clockGO.AddComponent<TextMeshProUGUI>();
        clockText.text = "7:10 AM";
        clockText.fontSize = 48;
        clockText.alignment = TextAlignmentOptions.TopRight;
        clockText.color = Color.white;

        // --- Create Activity Text ---
        GameObject activityGO = new GameObject("ActivityText");
        activityGO.transform.SetParent(canvasGO.transform);
        RectTransform activityRect = activityGO.AddComponent<RectTransform>();
        activityRect.anchorMin = new Vector2(1, 1);
        activityRect.anchorMax = new Vector2(1, 1);
        activityRect.pivot = new Vector2(1, 1);
        activityRect.offsetMin = new Vector2(-200, -120);
        activityRect.offsetMax = Vector2.zero;

        TextMeshProUGUI activityText = activityGO.AddComponent<TextMeshProUGUI>();
        activityText.text = "Before School";
        activityText.fontSize = 32;
        activityText.alignment = TextAlignmentOptions.TopRight;
        activityText.color = Color.white;

        // --- Create Day Text ---
        GameObject dayGO = new GameObject("DayText");
        dayGO.transform.SetParent(canvasGO.transform);
        RectTransform dayRect = dayGO.AddComponent<RectTransform>();
        dayRect.anchorMin = new Vector2(1, 1);
        dayRect.anchorMax = new Vector2(1, 1);
        dayRect.pivot = new Vector2(1, 1);
        dayRect.offsetMin = new Vector2(-200, -180);
        dayRect.offsetMax = Vector2.zero;

        TextMeshProUGUI dayText = dayGO.AddComponent<TextMeshProUGUI>();
        dayText.text = "Monday";
        dayText.fontSize = 32;
        dayText.alignment = TextAlignmentOptions.TopRight;
        dayText.color = Color.white;

        // --- Add TimeHUDManager to Canvas ---
        TimeHUDManager hudManager = canvasGO.AddComponent<TimeHUDManager>();
        hudManager.GetType().GetField("clockText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(hudManager, clockText);
        hudManager.GetType().GetField("activityText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(hudManager, activityText);
        hudManager.GetType().GetField("dayText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(hudManager, dayText);

        EditorUtility.DisplayDialog("Scene Setup Complete!", "Demo scene generated!\n\nPlayer, Camera, and HUD are ready.\n\nPress Play to test WASD movement.", "OK");
    }
}
