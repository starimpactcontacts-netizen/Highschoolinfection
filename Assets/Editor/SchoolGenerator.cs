using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Procedural greybox generator for the school level. Builds navigable blockout
/// geometry — courtyard, multi-floor building, hallways, classrooms with doorways,
/// and a perimeter wall — that you later skin with imported art assets.
///
/// Run via: Tools → Generate School Greybox
///
/// This is a BLOCKOUT, not final art. Everything is a grey primitive so you can
/// walk the layout, tune proportions, then replace pieces with real models.
/// </summary>
public class SchoolGenerator
{
    // --- Tunable layout constants (metres) ---
    const float WallThickness = 0.3f;
    const float FloorHeight = 4f;
    const int FloorCount = 3;

    const float BuildingWidth = 60f;   // along X
    const float BuildingDepth = 14f;   // along Z (hallway + classroom band)
    const float HallwayDepth = 4f;     // walkway width inside the building

    const float CourtyardDepth = 40f;  // space in front of the building
    const float PathWidth = 8f;

    const int ClassroomCount = 6;      // rooms per floor along the hallway

    // Shared materials, built once per generation.
    static Material _matFloor, _matWall, _matAccent, _matGround, _matPath, _matGlass;

    [MenuItem("Tools/Generate School Greybox")]
    public static void Generate()
    {
        if (!EditorUtility.DisplayDialog("Generate School Greybox",
            "This will clear any existing 'School' object and rebuild the blockout.\n\nContinue?",
            "Build it", "Cancel"))
            return;

        var existing = GameObject.Find("School");
        if (existing != null) Object.DestroyImmediate(existing);

        BuildMaterials();

        GameObject root = new GameObject("School");

        BuildGround(root.transform);
        BuildCourtyard(root.transform);
        BuildBuilding(root.transform);
        BuildPerimeterWall(root.transform);
        BuildTreePlaceholders(root.transform);

        Selection.activeGameObject = root;
        SceneView.FrameLastActiveSceneView();

        EditorUtility.DisplayDialog("Greybox Complete!",
            "School blockout generated under the 'School' object.\n\n" +
            "Next: import a school asset pack and start swapping these grey\n" +
            "boxes for real models. Move the Player onto the courtyard to walk it.",
            "Nice");
    }

    // ------------------------------------------------------------------
    //  Section builders
    // ------------------------------------------------------------------

    static void BuildGround(Transform parent)
    {
        // Big flat ground plane covering courtyard + building footprint.
        float totalDepth = CourtyardDepth + BuildingDepth + 20f;
        var ground = CreateBox("Ground", parent,
            new Vector3(0, -0.5f, (BuildingDepth * 0.5f) - (CourtyardDepth * 0.5f)),
            new Vector3(BuildingWidth + 40f, 1f, totalDepth),
            _matGround);
        ground.isStatic = true;
    }

    static void BuildCourtyard(Transform parent)
    {
        var group = new GameObject("Courtyard").transform;
        group.SetParent(parent);

        // Central path leading from the front gate to the building entrance.
        CreateBox("Path", group,
            new Vector3(0, 0.01f, -CourtyardDepth * 0.5f),
            new Vector3(PathWidth, 0.05f, CourtyardDepth),
            _matPath);

        // A couple of benches flanking the path (simple blockout props).
        CreateBox("Bench_L", group, new Vector3(-PathWidth, 0.4f, -12f),
            new Vector3(3f, 0.8f, 1f), _matAccent);
        CreateBox("Bench_R", group, new Vector3(PathWidth, 0.4f, -12f),
            new Vector3(3f, 0.8f, 1f), _matAccent);

        // Clocktower placeholder at the entrance (matches the YS silhouette).
        CreateBox("Clocktower", group, new Vector3(0, 6f, 1f),
            new Vector3(4f, 12f, 4f), _matWall);
    }

    static void BuildBuilding(Transform parent)
    {
        var building = new GameObject("Building").transform;
        building.SetParent(parent);

        float halfW = BuildingWidth * 0.5f;
        // Classroom band sits behind the hallway (further +Z from courtyard).
        float hallwayCenterZ = HallwayDepth * 0.5f;
        float classroomCenterZ = HallwayDepth + (BuildingDepth - HallwayDepth) * 0.5f;
        float classroomBandDepth = BuildingDepth - HallwayDepth;

        for (int floor = 0; floor < FloorCount; floor++)
        {
            float y = floor * FloorHeight;
            var floorGroup = new GameObject($"Floor_{floor + 1}").transform;
            floorGroup.SetParent(building);

            // Floor slab.
            CreateBox("Slab", floorGroup,
                new Vector3(0, y, BuildingDepth * 0.5f),
                new Vector3(BuildingWidth, WallThickness, BuildingDepth),
                _matFloor);

            // Back exterior wall (behind classrooms).
            CreateBox("Wall_Back", floorGroup,
                new Vector3(0, y + FloorHeight * 0.5f, BuildingDepth),
                new Vector3(BuildingWidth, FloorHeight, WallThickness),
                _matWall);

            // Side walls.
            CreateBox("Wall_Left", floorGroup,
                new Vector3(-halfW, y + FloorHeight * 0.5f, BuildingDepth * 0.5f),
                new Vector3(WallThickness, FloorHeight, BuildingDepth),
                _matWall);
            CreateBox("Wall_Right", floorGroup,
                new Vector3(halfW, y + FloorHeight * 0.5f, BuildingDepth * 0.5f),
                new Vector3(WallThickness, FloorHeight, BuildingDepth),
                _matWall);

            // Front wall = windows (glass band) with a door gap on the ground floor.
            BuildFrontWall(floorGroup, y, floor == 0);

            // Interior wall separating hallway from classrooms, with door gaps.
            BuildHallwayWall(floorGroup, y, HallwayDepth);

            // Classroom dividers along the band.
            BuildClassroomDividers(floorGroup, y, classroomCenterZ, classroomBandDepth);
        }

        // Roof cap.
        CreateBox("Roof", building,
            new Vector3(0, FloorCount * FloorHeight, BuildingDepth * 0.5f),
            new Vector3(BuildingWidth, WallThickness, BuildingDepth),
            _matFloor);
    }

    // Front wall facing the courtyard: a glass window band, split for an entrance.
    static void BuildFrontWall(Transform parent, float y, bool groundFloor)
    {
        float halfW = BuildingWidth * 0.5f;
        float doorGap = groundFloor ? PathWidth : 0f;

        if (doorGap <= 0f)
        {
            CreateBox("Wall_Front", parent,
                new Vector3(0, y + FloorHeight * 0.5f, 0f),
                new Vector3(BuildingWidth, FloorHeight, WallThickness),
                _matGlass);
            return;
        }

        // Two segments left and right of the central entrance gap.
        float segWidth = (BuildingWidth - doorGap) * 0.5f;
        float segOffset = (doorGap + segWidth) * 0.5f;

        CreateBox("Wall_Front_L", parent,
            new Vector3(-segOffset, y + FloorHeight * 0.5f, 0f),
            new Vector3(segWidth, FloorHeight, WallThickness),
            _matGlass);
        CreateBox("Wall_Front_R", parent,
            new Vector3(segOffset, y + FloorHeight * 0.5f, 0f),
            new Vector3(segWidth, FloorHeight, WallThickness),
            _matGlass);

        // Lintel above the entrance so the gap reads as a doorway, not a hole.
        CreateBox("Entrance_Lintel", parent,
            new Vector3(0, y + FloorHeight - 0.5f, 0f),
            new Vector3(doorGap, 1f, WallThickness),
            _matWall);
    }

    // Interior wall between hallway and classrooms, punched with doorways.
    static void BuildHallwayWall(Transform parent, float y, float z)
    {
        float doorWidth = 1.4f;
        float spacing = BuildingWidth / ClassroomCount;
        float halfW = BuildingWidth * 0.5f;

        // Build wall as segments, leaving a door gap at each classroom centre.
        List<Vector2> gaps = new List<Vector2>(); // (centerX, width)
        for (int i = 0; i < ClassroomCount; i++)
        {
            float cx = -halfW + spacing * (i + 0.5f);
            gaps.Add(new Vector2(cx, doorWidth));
        }

        float cursor = -halfW;
        int seg = 0;
        foreach (var gap in gaps)
        {
            float gapStart = gap.x - gap.y * 0.5f;
            if (gapStart > cursor)
            {
                float w = gapStart - cursor;
                CreateBox($"HallWall_{seg++}", parent,
                    new Vector3(cursor + w * 0.5f, y + FloorHeight * 0.5f, z),
                    new Vector3(w, FloorHeight, WallThickness),
                    _matWall);
            }
            cursor = gap.x + gap.y * 0.5f;
        }
        // Final segment after the last door.
        if (cursor < halfW)
        {
            float w = halfW - cursor;
            CreateBox($"HallWall_{seg}", parent,
                new Vector3(cursor + w * 0.5f, y + FloorHeight * 0.5f, z),
                new Vector3(w, FloorHeight, WallThickness),
                _matWall);
        }
    }

    // Walls dividing individual classrooms in the back band.
    static void BuildClassroomDividers(Transform parent, float y, float bandCenterZ, float bandDepth)
    {
        float spacing = BuildingWidth / ClassroomCount;
        float halfW = BuildingWidth * 0.5f;

        for (int i = 1; i < ClassroomCount; i++)
        {
            float x = -halfW + spacing * i;
            CreateBox($"Divider_{i}", parent,
                new Vector3(x, y + FloorHeight * 0.5f, bandCenterZ),
                new Vector3(WallThickness, FloorHeight, bandDepth),
                _matWall);
        }
    }

    static void BuildPerimeterWall(Transform parent)
    {
        var group = new GameObject("Perimeter").transform;
        group.SetParent(parent);

        float w = BuildingWidth + 30f;
        float frontZ = -CourtyardDepth - 5f;
        float backZ = BuildingDepth + 5f;
        float depth = backZ - frontZ;
        float h = 3f;
        float centerZ = (frontZ + backZ) * 0.5f;

        // Left & right perimeter walls.
        CreateBox("Perim_Left", group,
            new Vector3(-w * 0.5f, h * 0.5f, centerZ),
            new Vector3(WallThickness * 2, h, depth), _matAccent);
        CreateBox("Perim_Right", group,
            new Vector3(w * 0.5f, h * 0.5f, centerZ),
            new Vector3(WallThickness * 2, h, depth), _matAccent);

        // Front wall with a gate gap for the path.
        float segW = (w - PathWidth) * 0.5f;
        float segOff = (PathWidth + segW) * 0.5f;
        CreateBox("Perim_Front_L", group,
            new Vector3(-segOff, h * 0.5f, frontZ),
            new Vector3(segW, h, WallThickness * 2), _matAccent);
        CreateBox("Perim_Front_R", group,
            new Vector3(segOff, h * 0.5f, frontZ),
            new Vector3(segW, h, WallThickness * 2), _matAccent);
    }

    // Simple cherry-tree stand-ins: a trunk + a canopy sphere.
    static void BuildTreePlaceholders(Transform parent)
    {
        var group = new GameObject("Trees").transform;
        group.SetParent(parent);

        Material trunk = MakeMaterial(new Color(0.35f, 0.25f, 0.2f));
        Material canopy = MakeMaterial(new Color(0.95f, 0.7f, 0.85f)); // sakura pink

        float[] rows = { -8f, -20f, -32f };
        float xOff = PathWidth + 4f;

        foreach (float z in rows)
        {
            foreach (float sign in new[] { -1f, 1f })
            {
                float x = sign * xOff;
                var t = CreatePrimitive("Trunk", group, PrimitiveType.Cylinder,
                    new Vector3(x, 2f, z), new Vector3(0.6f, 2f, 0.6f), trunk);
                CreatePrimitive("Canopy", group, PrimitiveType.Sphere,
                    new Vector3(x, 4.8f, z), new Vector3(5f, 4f, 5f), canopy);
            }
        }
    }

    // ------------------------------------------------------------------
    //  Helpers
    // ------------------------------------------------------------------

    static GameObject CreateBox(string name, Transform parent, Vector3 pos, Vector3 size, Material mat)
    {
        return CreatePrimitive(name, parent, PrimitiveType.Cube, pos, size, mat);
    }

    static GameObject CreatePrimitive(string name, Transform parent, PrimitiveType type,
        Vector3 pos, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        go.isStatic = true;
        var r = go.GetComponent<Renderer>();
        if (r != null && mat != null) r.sharedMaterial = mat;
        return go;
    }

    static void BuildMaterials()
    {
        _matGround = MakeMaterial(new Color(0.45f, 0.6f, 0.4f));  // grass green
        _matPath   = MakeMaterial(new Color(0.8f, 0.78f, 0.72f)); // pavement
        _matFloor  = MakeMaterial(new Color(0.75f, 0.8f, 0.85f)); // tile blue-grey
        _matWall   = MakeMaterial(new Color(0.9f, 0.9f, 0.92f));  // off-white
        _matAccent = MakeMaterial(new Color(0.7f, 0.55f, 0.45f)); // wood/brown
        _matGlass  = MakeMaterial(new Color(0.7f, 0.85f, 0.95f, 1f)); // window blue
    }

    static Material MakeMaterial(Color c)
    {
        // Prefer URP Lit; fall back to Standard if the project is Built-in.
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var m = new Material(shader) { color = c };
        return m;
    }
}
