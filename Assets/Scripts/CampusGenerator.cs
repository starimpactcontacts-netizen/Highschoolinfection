using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the full explorable campus procedurally at runtime — front gate, courtyard,
/// 3-floor main building (cafeteria, library, classrooms, lockers, internal stairwell,
/// railed rooftop), a west classroom annex, a gym, and the surrounding grounds/fence.
///
/// This is the runtime equivalent of the Assets/Editor setup scripts (SchoolGenerator,
/// ClassroomPlacer): same greybox techniques (shared materials, door-gap wall segments,
/// CreateBox/CreatePrimitive helpers), but it runs automatically via Awake() the moment
/// Play starts — no Tools-menu click needed. Re-runnable: destroys and rebuilds "Campus"
/// each time, and hides (not deletes) any pre-existing "School"/"AnimeClassroom" objects
/// so they don't overlap.
///
/// Structural elements (floors, walls, beams, ramps) stay primitives. Furniture/props
/// (desks, chairs, boards, computers, lockers, bookshelves, doors, blinds, the bus) are
/// instantiated from the imported "School Assets" pack via Resources.Load — see
/// Assets/school/Prefabs/Resources/props. SpawnProp() adds a BoxCollider to anything
/// that doesn't already have one, so every prop is solid even if the pack didn't include
/// collision. This is a BLOCKOUT for layout/scale: prop orientations are best-guess since
/// the pack wasn't authored for this layout — expect to nudge a few rotations in-editor.
/// </summary>
public class CampusGenerator : MonoBehaviour
{
    private enum RoomType { Classroom, Cafeteria, Library, Stairwell }

    // --- Main building (matches the original SchoolGenerator scale) ---
    const float WallThickness = 0.3f;
    const float FloorHeight = 4.5f;
    const int FloorCount = 3;
    const float BuildingWidth = 90f;   // X: -45..45
    const float BuildingDepth = 22f;   // Z: 0..22
    const float HallwayDepth = 6f;     // Z: 0..6 band nearest the courtyard
    const float SegmentWidth = 15f;    // 6 room segments per floor
    const float HalfWidth = BuildingWidth * 0.5f;

    // --- Courtyard / grounds ---
    const float CourtyardDepth = 65f;  // Building front (Z=0) back to the gate (Z=-65)
    const float PathWidth = 10f;

    // --- Annex (west wing) ---
    const float AnnexGap = 8f;
    const float AnnexWidth = 35f;
    const float AnnexEastX = -HalfWidth - AnnexGap;              // -53
    const float AnnexWestX = AnnexEastX - AnnexWidth;             // -88

    // --- Gym (south, behind main building) ---
    const float GymGap = 8f;
    const float GymWidth = 34f;
    const float GymDepth = 30f;
    const float GymHeight = 9f;
    const float GymFrontZ = BuildingDepth + GymGap;               // 30
    const float GymBackZ = GymFrontZ + GymDepth;                  // 60

    // --- Perimeter ---
    const float PerimWestX = AnnexWestX - 8f;
    const float PerimEastX = HalfWidth + 10f;
    const float PerimFrontZ = -CourtyardDepth;                    // -65
    const float PerimBackZ = GymBackZ + 8f;                       // 68

    static readonly RoomType[][] FloorPlan =
    {
        new[] { RoomType.Cafeteria, RoomType.Cafeteria, RoomType.Classroom, RoomType.Classroom, RoomType.Classroom, RoomType.Stairwell },
        new[] { RoomType.Library, RoomType.Library, RoomType.Classroom, RoomType.Classroom, RoomType.Classroom, RoomType.Stairwell },
        new[] { RoomType.Classroom, RoomType.Classroom, RoomType.Classroom, RoomType.Classroom, RoomType.Classroom, RoomType.Stairwell },
    };

    // Shared materials, built once per generation.
    Material _matGround, _matPath, _matFloor, _matWall, _matAccent, _matGlass, _matFence,
        _matTrunk, _matCanopy, _matGymFloor, _matRoofRail;

    void Awake()
    {
        Build();
    }

    void Build()
    {
        var existingSchool = GameObject.Find("School");
        if (existingSchool != null) existingSchool.SetActive(false);
        var existingClassroom = GameObject.Find("AnimeClassroom");
        if (existingClassroom != null) existingClassroom.SetActive(false);

        var existingCampus = GameObject.Find("Campus");
        if (existingCampus != null) Object.Destroy(existingCampus);

        BuildMaterials();

        var root = new GameObject("Campus").transform;

        BuildGround(root);
        BuildPerimeterFenceAndGate(root);
        BuildCourtyard(root);
        BuildMainBuilding(root);
        BuildAnnex(root);
        BuildGym(root);

        EnsurePlayerAndCamera();
    }

    // ------------------------------------------------------------------
    //  Grounds
    // ------------------------------------------------------------------

    void BuildGround(Transform parent)
    {
        float width = (PerimEastX - PerimWestX) + 10f;
        float depth = (PerimBackZ - PerimFrontZ) + 20f;
        float centerX = (PerimEastX + PerimWestX) * 0.5f;
        float centerZ = (PerimBackZ + PerimFrontZ) * 0.5f;

        CreateBox("Ground", parent, new Vector3(centerX, -0.5f, centerZ),
            new Vector3(width, 1f, depth), _matGround);
    }

    void BuildPerimeterFenceAndGate(Transform parent)
    {
        var group = new GameObject("Perimeter").transform;
        group.SetParent(parent);

        float h = 3f;
        float depthSpan = PerimBackZ - PerimFrontZ;
        float centerZ = (PerimFrontZ + PerimBackZ) * 0.5f;

        // Solid perimeter walls: west, east, back.
        CreateBox("Perim_West", group, new Vector3(PerimWestX, h * 0.5f, centerZ),
            new Vector3(WallThickness * 2f, h, depthSpan), _matAccent);
        CreateBox("Perim_East", group, new Vector3(PerimEastX, h * 0.5f, centerZ),
            new Vector3(WallThickness * 2f, h, depthSpan), _matAccent);
        CreateBox("Perim_Back", group, new Vector3((PerimWestX + PerimEastX) * 0.5f, h * 0.5f, PerimBackZ),
            new Vector3(PerimEastX - PerimWestX, h, WallThickness * 2f), _matAccent);

        // Front boundary is a fence (posts + rail) flanking the gate gap, distinct from the solid walls above.
        BuildFenceRun(group, PerimWestX, -PathWidth * 0.5f, PerimFrontZ, h);
        BuildFenceRun(group, PathWidth * 0.5f, PerimEastX, PerimFrontZ, h);

        BuildGate(parent);
    }

    void BuildFenceRun(Transform parent, float xStart, float xEnd, float z, float height)
    {
        var run = new GameObject("Fence").transform;
        run.SetParent(parent);

        float span = xEnd - xStart;
        if (span <= 0f) return;

        float postSpacing = 4f;
        int postCount = Mathf.Max(2, Mathf.RoundToInt(span / postSpacing) + 1);

        for (int i = 0; i < postCount; i++)
        {
            float x = xStart + span * (i / (float)(postCount - 1));
            CreateBox($"Post_{i}", run, new Vector3(x, height * 0.5f, z),
                new Vector3(0.2f, height, 0.2f), _matFence);
        }

        CreateBox("Rail", run, new Vector3(xStart + span * 0.5f, height - 0.15f, z),
            new Vector3(span, 0.15f, 0.15f), _matFence);
    }

    void BuildGate(Transform parent)
    {
        var group = new GameObject("Gate").transform;
        group.SetParent(parent);

        float pillarH = 4f;
        float gapHalf = PathWidth * 0.5f;

        CreateBox("Gate_Pillar_L", group, new Vector3(-gapHalf - 0.5f, pillarH * 0.5f, PerimFrontZ),
            new Vector3(1f, pillarH, 1f), _matWall);
        CreateBox("Gate_Pillar_R", group, new Vector3(gapHalf + 0.5f, pillarH * 0.5f, PerimFrontZ),
            new Vector3(1f, pillarH, 1f), _matWall);
        CreateBox("Gate_Lintel", group, new Vector3(0, pillarH - 0.25f, PerimFrontZ),
            new Vector3(PathWidth + 2f, 0.5f, 1f), _matWall);
    }

    void BuildCourtyard(Transform parent)
    {
        var group = new GameObject("Courtyard").transform;
        group.SetParent(parent);

        float pathLength = CourtyardDepth;
        float pathCenterZ = -CourtyardDepth * 0.5f;

        CreateBox("Path", group, new Vector3(0, 0.01f, pathCenterZ),
            new Vector3(PathWidth, 0.05f, pathLength), _matPath);

        CreateBox("Bench_L", group, new Vector3(-PathWidth, 0.4f, -14f), new Vector3(3f, 0.8f, 1f), _matAccent);
        CreateBox("Bench_R", group, new Vector3(PathWidth, 0.4f, -14f), new Vector3(3f, 0.8f, 1f), _matAccent);
        CreateBox("Bench_L2", group, new Vector3(-PathWidth, 0.4f, -32f), new Vector3(3f, 0.8f, 1f), _matAccent);
        CreateBox("Bench_R2", group, new Vector3(PathWidth, 0.4f, -32f), new Vector3(3f, 0.8f, 1f), _matAccent);

        CreateBox("Clocktower", group, new Vector3(0, 6f, -2f), new Vector3(4f, 12f, 4f), _matWall);

        SpawnProp("bus", group, new Vector3(-22f, 0f, -50f), Quaternion.Euler(0f, 90f, 0f));

        BuildCherryBlossoms(group);
    }

    void BuildCherryBlossoms(Transform parent)
    {
        var group = new GameObject("CherryTrees").transform;
        group.SetParent(parent);

        float[] rows = { -8f, -20f, -32f, -44f, -56f };
        float xOff = PathWidth + 4f;

        foreach (float z in rows)
        {
            foreach (float sign in new[] { -1f, 1f })
            {
                float x = sign * xOff;
                CreatePrimitive("Trunk", group, PrimitiveType.Cylinder,
                    new Vector3(x, 2f, z), new Vector3(0.6f, 2f, 0.6f), _matTrunk);
                CreatePrimitive("Canopy", group, PrimitiveType.Sphere,
                    new Vector3(x, 4.8f, z), new Vector3(5f, 4f, 5f), _matCanopy);
            }
        }
    }

    // ------------------------------------------------------------------
    //  Main building
    // ------------------------------------------------------------------

    void BuildMainBuilding(Transform parent)
    {
        var building = new GameObject("MainBuilding").transform;
        building.SetParent(parent);

        for (int floor = 0; floor < FloorCount; floor++)
        {
            float y = floor * FloorHeight;
            var floorGroup = new GameObject($"Floor_{floor + 1}").transform;
            floorGroup.SetParent(building);

            RoomType[] segments = FloorPlan[floor];

            // Floor slab — open (no slab) over the stairwell column for floors above ground,
            // so the shaft is a real vertical opening.
            bool openStairwellHole = floor > 0;
            BuildSlab(floorGroup, y, openStairwellHole);

            BuildBackWall(floorGroup, y, floor == 0);
            BuildSideWall(floorGroup, y, -HalfWidth, floor == 0); // west wall: annex breezeway gap on ground floor
            BuildSideWall(floorGroup, y, HalfWidth, false);               // east wall: solid every floor
            BuildFrontWall(floorGroup, y, floor == 0);
            BuildWindowBlinds(floorGroup, y, floor == 0);

            BuildRoomSegments(floorGroup, y, segments);
            BuildLockers(floorGroup, y, segments);
            BuildCeilingLights(floorGroup, y);
        }

        // Roof / rooftop deck (walkable, full width — the stairwell's top landing).
        CreateBox("Roof", building, new Vector3(0, FloorCount * FloorHeight, BuildingDepth * 0.5f),
            new Vector3(BuildingWidth, WallThickness, BuildingDepth), _matFloor);
        BuildRoofRailing(building);

        BuildStairwell(building);
    }

    void BuildSlab(Transform parent, float y, bool openStairwellHole)
    {
        if (!openStairwellHole)
        {
            CreateBox("Slab", parent, new Vector3(0, y, BuildingDepth * 0.5f),
                new Vector3(BuildingWidth, WallThickness, BuildingDepth), _matFloor);
            return;
        }

        // Leave the stairwell column (segment 5, the last SegmentWidth of the building) open.
        float solidWidth = BuildingWidth - SegmentWidth;
        CreateBox("Slab_Main", parent,
            new Vector3(-SegmentWidth * 0.5f, y, BuildingDepth * 0.5f),
            new Vector3(solidWidth, WallThickness, BuildingDepth), _matFloor);
    }

    void BuildBackWall(Transform parent, float y, bool groundFloor)
    {
        float gymGapWidth = 12f;
        if (!groundFloor || gymGapWidth <= 0f)
        {
            CreateBox("Wall_Back", parent, new Vector3(0, y + FloorHeight * 0.5f, BuildingDepth),
                new Vector3(BuildingWidth, FloorHeight, WallThickness), _matWall);
            return;
        }

        float segWidth = (BuildingWidth - gymGapWidth) * 0.5f;
        float segOffset = (gymGapWidth + segWidth) * 0.5f;
        CreateBox("Wall_Back_L", parent, new Vector3(-segOffset, y + FloorHeight * 0.5f, BuildingDepth),
            new Vector3(segWidth, FloorHeight, WallThickness), _matWall);
        CreateBox("Wall_Back_R", parent, new Vector3(segOffset, y + FloorHeight * 0.5f, BuildingDepth),
            new Vector3(segWidth, FloorHeight, WallThickness), _matWall);
        CreateBox("GymCorridor_Lintel", parent, new Vector3(0, y + FloorHeight - 0.5f, BuildingDepth),
            new Vector3(gymGapWidth, 1f, WallThickness), _matWall);
    }

    void BuildSideWall(Transform parent, float y, float x, bool hasBreezewayGap)
    {
        if (!hasBreezewayGap)
        {
            CreateBox(x < 0 ? "Wall_Left" : "Wall_Right", parent,
                new Vector3(x, y + FloorHeight * 0.5f, BuildingDepth * 0.5f),
                new Vector3(WallThickness, FloorHeight, BuildingDepth), _matWall);
            return;
        }

        // Gap for the annex breezeway sits within the hallway band (Z: 0..HallwayDepth).
        float gapStart = 0f, gapEnd = HallwayDepth;
        float frontSegDepth = gapStart;             // 0, no segment needed
        float backSegDepth = BuildingDepth - gapEnd;
        float backSegCenterZ = gapEnd + backSegDepth * 0.5f;

        if (frontSegDepth > 0.01f)
        {
            CreateBox("Wall_Left_Front", parent, new Vector3(x, y + FloorHeight * 0.5f, frontSegDepth * 0.5f),
                new Vector3(WallThickness, FloorHeight, frontSegDepth), _matWall);
        }
        CreateBox("Wall_Left_Back", parent, new Vector3(x, y + FloorHeight * 0.5f, backSegCenterZ),
            new Vector3(WallThickness, FloorHeight, backSegDepth), _matWall);
        CreateBox("BreezewayCorridor_Lintel", parent, new Vector3(x, y + FloorHeight - 0.5f, gapEnd * 0.5f),
            new Vector3(WallThickness, 1f, gapEnd), _matWall);
    }

    // Front wall facing the courtyard: glass window band, with a door gap on the ground floor.
    void BuildFrontWall(Transform parent, float y, bool groundFloor)
    {
        float doorGap = groundFloor ? PathWidth : 0f;

        if (doorGap <= 0f)
        {
            CreateBox("Wall_Front", parent, new Vector3(0, y + FloorHeight * 0.5f, 0f),
                new Vector3(BuildingWidth, FloorHeight, WallThickness), _matGlass);
            return;
        }

        float segWidth = (BuildingWidth - doorGap) * 0.5f;
        float segOffset = (doorGap + segWidth) * 0.5f;

        CreateBox("Wall_Front_L", parent, new Vector3(-segOffset, y + FloorHeight * 0.5f, 0f),
            new Vector3(segWidth, FloorHeight, WallThickness), _matGlass);
        CreateBox("Wall_Front_R", parent, new Vector3(segOffset, y + FloorHeight * 0.5f, 0f),
            new Vector3(segWidth, FloorHeight, WallThickness), _matGlass);
        CreateBox("Entrance_Lintel", parent, new Vector3(0, y + FloorHeight - 0.5f, 0f),
            new Vector3(doorGap, 1f, WallThickness), _matWall);
    }

    // Blinds on the front glass band, skipping the ground-floor entrance gap.
    void BuildWindowBlinds(Transform parent, float y, bool groundFloor)
    {
        int count = 6;
        float spacing = BuildingWidth / count;
        float doorHalf = groundFloor ? PathWidth * 0.5f + 1f : 0f;

        for (int i = 0; i < count; i++)
        {
            float x = -HalfWidth + spacing * (i + 0.5f);
            if (Mathf.Abs(x) < doorHalf) continue;
            SpawnProp("jalousie", parent, new Vector3(x, y + FloorHeight * 0.6f, 0.2f), Quaternion.identity);
        }
    }

    // Builds hallway wall (with door gaps), classroom dividers, and furniture for each segment.
    void BuildRoomSegments(Transform parent, float y, RoomType[] segments)
    {
        float hallwayZ = HallwayDepth;
        float bandCenterZ = HallwayDepth + (BuildingDepth - HallwayDepth) * 0.5f;
        float bandDepth = BuildingDepth - HallwayDepth;

        // Group consecutive segments of the same type (Cafeteria/Library merge into one big room).
        List<(int start, int count, RoomType type)> groups = new List<(int, int, RoomType)>();
        int i2 = 0;
        while (i2 < segments.Length)
        {
            int start = i2;
            RoomType type = segments[i2];
            int count = 1;
            while (i2 + count < segments.Length && segments[i2 + count] == type) count++;
            groups.Add((start, count, type));
            i2 += count;
        }

        int hallSeg = 0;
        foreach (var group in groups)
        {
            float groupStartX = -HalfWidth + SegmentWidth * group.start;
            float groupWidth = SegmentWidth * group.count;
            float groupCenterX = groupStartX + groupWidth * 0.5f;

            if (group.type == RoomType.Stairwell)
            {
                // Fully open: no hallway wall, no divider, no furniture.
                continue;
            }

            // Hallway wall segment with a centered doorway.
            float doorWidth = Mathf.Min(groupWidth * 0.4f, 4f);
            BuildWallWithGap($"HallWall_{hallSeg}", parent, groupCenterX, groupWidth,
                y + FloorHeight * 0.5f, hallwayZ, FloorHeight, doorWidth, _matWall, true);
            SpawnProp(hallSeg % 2 == 0 ? "a door" : "a door1", parent,
                new Vector3(groupCenterX - doorWidth * 0.5f - 0.05f, y, hallwayZ), Quaternion.Euler(0f, 90f, 0f));
            hallSeg++;

            // Divider walls at the boundaries between distinct room groups (not merged rooms, not stairwell).
            bool nextIsStairwell = group.start + group.count < segments.Length &&
                                   segments[group.start + group.count] == RoomType.Stairwell;
            if (!nextIsStairwell && group.start + group.count < segments.Length)
            {
                float dividerX = -HalfWidth + SegmentWidth * (group.start + group.count);
                CreateBox($"Divider_{group.start + group.count}", parent,
                    new Vector3(dividerX, y + FloorHeight * 0.5f, bandCenterZ),
                    new Vector3(WallThickness, FloorHeight, bandDepth), _matWall);
            }

            switch (group.type)
            {
                case RoomType.Classroom:
                    BuildDesks(parent, y, groupCenterX, bandCenterZ, groupWidth, bandDepth);
                    break;
                case RoomType.Cafeteria:
                    BuildCafeteriaFurniture(parent, y, groupCenterX, bandCenterZ, groupWidth, bandDepth);
                    break;
                case RoomType.Library:
                    BuildLibraryFurniture(parent, y, groupCenterX, bandCenterZ, groupWidth, bandDepth);
                    break;
            }
        }
    }

    // Builds a wall along X centered at (centerX, centerZ) with a doorway gap in the middle.
    void BuildWallWithGap(string namePrefix, Transform parent, float centerX, float totalWidth,
        float centerY, float z, float height, float gapWidth, Material mat, bool addLintel)
    {
        float segWidth = (totalWidth - gapWidth) * 0.5f;
        float segOffset = (gapWidth + segWidth) * 0.5f;

        if (segWidth > 0.01f)
        {
            CreateBox($"{namePrefix}_L", parent, new Vector3(centerX - segOffset, centerY, z),
                new Vector3(segWidth, height, WallThickness), mat);
            CreateBox($"{namePrefix}_R", parent, new Vector3(centerX + segOffset, centerY, z),
                new Vector3(segWidth, height, WallThickness), mat);
        }
        if (addLintel)
        {
            CreateBox($"{namePrefix}_Lintel", parent, new Vector3(centerX, centerY + height * 0.5f - 0.5f, z),
                new Vector3(gapWidth, 1f, WallThickness), mat);
        }
    }

    static readonly string[] ClassroomTableProps = { "table2", "table3" };
    static readonly string[] ChairProps = { "chair", "chair1" };

    // Board + teacher's desk against the back (exterior) wall; student tables/chairs face it.
    void BuildDesks(Transform parent, float y, float centerX, float bandCenterZ, float width, float depth)
    {
        float backZ = bandCenterZ + depth * 0.5f; // back exterior wall
        SpawnProp("board", parent, new Vector3(centerX, y + 2f, backZ - 0.2f), Quaternion.identity);

        float teacherZ = backZ - 1.8f;
        SpawnProp("table1", parent, new Vector3(centerX, y, teacherZ), Quaternion.identity);
        SpawnProp("computer", parent, new Vector3(centerX + 0.6f, y + 0.75f, teacherZ), Quaternion.identity);

        int cols = 3, rows = 2;
        float marginX = width * 0.2f;
        float usableW = width - marginX * 2f;
        float rowsStartZ = bandCenterZ - depth * 0.5f + depth * 0.3f;
        float rowsSpan = Mathf.Max(teacherZ - 2f - rowsStartZ, 1f);

        for (int c = 0; c < cols; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                float dx = centerX - usableW * 0.5f + usableW * ((c + 0.5f) / cols);
                float dz = rowsStartZ + rowsSpan * ((r + 0.5f) / rows);
                int variant = c + r * cols;
                SpawnProp(ClassroomTableProps[variant % ClassroomTableProps.Length], parent, new Vector3(dx, y, dz), Quaternion.identity);
                SpawnProp(ChairProps[variant % ChairProps.Length], parent, new Vector3(dx, y, dz - 0.7f), Quaternion.identity);
            }
        }
    }

    void BuildCafeteriaFurniture(Transform parent, float y, float centerX, float bandCenterZ, float width, float depth)
    {
        int tables = 3;
        float spacing = width / (tables + 1);
        float startX = centerX - width * 0.5f;

        for (int t = 0; t < tables; t++)
        {
            float x = startX + spacing * (t + 1);
            SpawnProp("table3", parent, new Vector3(x, y, bandCenterZ), Quaternion.identity);
            SpawnProp("tray", parent, new Vector3(x - 0.4f, y + 0.75f, bandCenterZ), Quaternion.identity);
            SpawnProp("tray", parent, new Vector3(x + 0.4f, y + 0.75f, bandCenterZ), Quaternion.identity);

            SpawnProp(ChairProps[t % ChairProps.Length], parent, new Vector3(x, y, bandCenterZ - 1.3f), Quaternion.identity);
            SpawnProp(ChairProps[(t + 1) % ChairProps.Length], parent, new Vector3(x, y, bandCenterZ + 1.3f), Quaternion.Euler(0f, 180f, 0f));
        }
    }

    static readonly string[] RackProps = { "rack", "rack1" };
    static readonly string[] BookProps =
    {
        "book", "book1", "book2", "book3", "book4", "book5", "book6", "book7", "book8",
        "book9", "book10", "book11", "book12", "book13", "book14", "book15", "book16",
    };

    void BuildLibraryFurniture(Transform parent, float y, float centerX, float bandCenterZ, float width, float depth)
    {
        int shelves = 4;
        float spacing = width / (shelves + 1);
        float startX = centerX - width * 0.5f;
        float shelfZ = bandCenterZ - depth * 0.3f;
        int bookIndex = 0;

        for (int s = 0; s < shelves; s++)
        {
            float x = startX + spacing * (s + 1);
            SpawnProp(RackProps[s % RackProps.Length], parent, new Vector3(x, y, shelfZ), Quaternion.identity);

            for (int b = 0; b < 3; b++)
            {
                SpawnProp(BookProps[bookIndex % BookProps.Length], parent,
                    new Vector3(x - 0.6f + b * 0.6f, y + 1.2f, shelfZ + 0.3f), Quaternion.identity);
                bookIndex++;
            }
        }

        SpawnProp("showcase", parent, new Vector3(centerX, y, bandCenterZ - depth * 0.45f), Quaternion.identity);

        // Reading area: a couple of low tables near the front.
        float readingZ = bandCenterZ + depth * 0.25f;
        SpawnProp("table1", parent, new Vector3(centerX - width * 0.2f, y, readingZ), Quaternion.identity);
        SpawnProp("chair", parent, new Vector3(centerX - width * 0.2f, y, readingZ - 0.8f), Quaternion.identity);
        SpawnProp("table1", parent, new Vector3(centerX + width * 0.2f, y, readingZ), Quaternion.identity);
        SpawnProp("chair1", parent, new Vector3(centerX + width * 0.2f, y, readingZ - 0.8f), Quaternion.identity);
    }

    static readonly string[] LockerProps =
    {
        "locker", "locker1", "locker2", "locker_1", "locker_2", "locker_3", "locker_4", "locker_5",
    };

    void BuildLockers(Transform parent, float y, RoomType[] segments)
    {
        const float lockerZ = 0.4f; // hugging the front wall

        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i] == RoomType.Stairwell) continue;

            float segCenterX = -HalfWidth + SegmentWidth * (i + 0.5f);
            float x = segCenterX - SegmentWidth * 0.5f + 1.5f;
            SpawnProp(LockerProps[i % LockerProps.Length], parent, new Vector3(x, y, lockerZ), Quaternion.identity);

            if (i % 2 == 0)
                SpawnProp("fire", parent, new Vector3(segCenterX + SegmentWidth * 0.3f, y + 1.2f, lockerZ + 0.1f), Quaternion.identity);
        }
    }

    void BuildCeilingLights(Transform parent, float y)
    {
        int strips = 6;
        float spacing = BuildingWidth / strips;
        float ceilingY = y + FloorHeight - 0.15f;
        float hallwayZ = HallwayDepth * 0.5f;

        for (int i = 0; i < strips; i++)
        {
            float x = -HalfWidth + spacing * (i + 0.5f);

            CreateBox($"LightFixture_{i}", parent, new Vector3(x, ceilingY, hallwayZ),
                new Vector3(2.5f, 0.15f, 0.6f), MakeEmissiveMaterial(new Color(1f, 0.98f, 0.9f)));

            var lightGO = new GameObject($"HallLight_{i}");
            lightGO.transform.SetParent(parent);
            lightGO.transform.localPosition = new Vector3(x, ceilingY - 0.3f, hallwayZ);
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = spacing * 1.6f;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.98f, 0.9f);
        }
    }

    void BuildRoofRailing(Transform parent)
    {
        var group = new GameObject("RoofRailing").transform;
        group.SetParent(parent);

        float y = FloorCount * FloorHeight + 0.6f;
        float h = 1.2f;

        CreateBox("Rail_Front", group, new Vector3(0, y, 0), new Vector3(BuildingWidth, h, WallThickness), _matRoofRail);
        CreateBox("Rail_Back", group, new Vector3(0, y, BuildingDepth), new Vector3(BuildingWidth, h, WallThickness), _matRoofRail);
        CreateBox("Rail_Left", group, new Vector3(-HalfWidth, y, BuildingDepth * 0.5f), new Vector3(WallThickness, h, BuildingDepth), _matRoofRail);
        CreateBox("Rail_Right", group, new Vector3(HalfWidth, y, BuildingDepth * 0.5f), new Vector3(WallThickness, h, BuildingDepth), _matRoofRail);
    }

    // Internal vertical shaft (segment 5, X: 30..45) rising from the ground floor to the
    // rooftop through three straight ramps + landings. Slabs for floors 1 & 2 are already
    // left open there by BuildSlab(); the roof stays solid as the top landing surface.
    void BuildStairwell(Transform parent)
    {
        var group = new GameObject("Stairwell").transform;
        group.SetParent(parent);

        float xCenter = HalfWidth - SegmentWidth * 0.5f; // center of segment 5
        float width = SegmentWidth;

        // 1-unit buffers at both ends (z=0 is the front glass wall, z=22 the back wall) so
        // ramps never physically touch them; 1-unit landings between flights.
        BuildRamp(group, "Flight1", xCenter, width, 1f, 7f, 0f, FloorHeight);
        CreateBox("Landing1", group, new Vector3(xCenter, FloorHeight, 7.5f), new Vector3(width, WallThickness, 1f), _matFloor);

        BuildRamp(group, "Flight2", xCenter, width, 8f, 14f, FloorHeight, FloorHeight * 2f);
        CreateBox("Landing2", group, new Vector3(xCenter, FloorHeight * 2f, 14.5f), new Vector3(width, WallThickness, 1f), _matFloor);

        BuildRamp(group, "Flight3", xCenter, width, 15f, 21f, FloorHeight * 2f, FloorHeight * 3f);
    }

    // A rotated box spanning from (xCenter, yStart, zStart) to (xCenter, yEnd, zEnd) — a walkable ramp.
    void BuildRamp(Transform parent, string name, float xCenter, float width, float zStart, float zEnd, float yStart, float yEnd)
    {
        float runLength = zEnd - zStart;
        float rise = yEnd - yStart;
        float slopeLength = Mathf.Sqrt(runLength * runLength + rise * rise);
        float angle = Mathf.Atan2(rise, runLength) * Mathf.Rad2Deg;

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.localPosition = new Vector3(xCenter, (yStart + yEnd) * 0.5f, (zStart + zEnd) * 0.5f);
        go.transform.localRotation = Quaternion.Euler(-angle, 0f, 0f);
        go.transform.localScale = new Vector3(width, WallThickness, slopeLength);
        var r = go.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = _matFloor;
    }

    // ------------------------------------------------------------------
    //  Annex (west classroom wing)
    // ------------------------------------------------------------------

    void BuildAnnex(Transform parent)
    {
        var annex = new GameObject("Annex").transform;
        annex.SetParent(parent);

        float width = AnnexWidth;
        float depth = BuildingDepth;
        float y = 0f;
        float centerX = (AnnexEastX + AnnexWestX) * 0.5f;

        CreateBox("Slab", annex, new Vector3(centerX, y, depth * 0.5f), new Vector3(width, WallThickness, depth), _matFloor);
        CreateBox("Wall_Back", annex, new Vector3(centerX, y + FloorHeight * 0.5f, depth), new Vector3(width, FloorHeight, WallThickness), _matWall);
        CreateBox("Wall_Front", annex, new Vector3(centerX, y + FloorHeight * 0.5f, 0f), new Vector3(width, FloorHeight, WallThickness), _matGlass);
        CreateBox("Wall_West", annex, new Vector3(AnnexWestX, y + FloorHeight * 0.5f, depth * 0.5f), new Vector3(WallThickness, FloorHeight, depth), _matWall);
        CreateBox("Roof", annex, new Vector3(centerX, y + FloorHeight, depth * 0.5f), new Vector3(width, WallThickness, depth), _matFloor);

        // East wall has the breezeway gap (matches the main building's west-wall gap).
        float gapEnd = HallwayDepth;
        float backSegDepth = depth - gapEnd;
        CreateBox("Wall_East_Back", annex, new Vector3(AnnexEastX, y + FloorHeight * 0.5f, gapEnd + backSegDepth * 0.5f),
            new Vector3(WallThickness, FloorHeight, backSegDepth), _matWall);
        CreateBox("BreezewayCorridor_Lintel", annex, new Vector3(AnnexEastX, y + FloorHeight - 0.5f, gapEnd * 0.5f),
            new Vector3(WallThickness, 1f, gapEnd), _matWall);

        BuildBreezeway(annex, y);

        // Hallway wall + 3 classrooms with doorways and dividers.
        int classroomCount = 3;
        float bandCenterZ = HallwayDepth + (depth - HallwayDepth) * 0.5f;
        float bandDepth = depth - HallwayDepth;
        float roomWidth = width / classroomCount;

        for (int i = 0; i < classroomCount; i++)
        {
            float roomCenterX = AnnexWestX + roomWidth * (i + 0.5f);
            float doorWidth = Mathf.Min(roomWidth * 0.4f, 3f);

            BuildWallWithGap($"HallWall_{i}", annex, roomCenterX, roomWidth,
                y + FloorHeight * 0.5f, HallwayDepth, FloorHeight, doorWidth, _matWall, true);
            SpawnProp(i % 2 == 0 ? "a door" : "a door1", annex,
                new Vector3(roomCenterX - doorWidth * 0.5f - 0.05f, y, HallwayDepth), Quaternion.Euler(0f, 90f, 0f));

            if (i > 0)
            {
                float dividerX = AnnexWestX + roomWidth * i;
                CreateBox($"Divider_{i}", annex, new Vector3(dividerX, y + FloorHeight * 0.5f, bandCenterZ),
                    new Vector3(WallThickness, FloorHeight, bandDepth), _matWall);
            }

            BuildDesks(annex, y, roomCenterX, bandCenterZ, roomWidth, bandDepth);
        }
    }

    void BuildBreezeway(Transform parent, float y)
    {
        var group = new GameObject("Breezeway").transform;
        group.SetParent(parent);

        float xStart = AnnexEastX;
        float xEnd = -HalfWidth;
        float width = xEnd - xStart;
        float centerX = (xStart + xEnd) * 0.5f;
        float centerZ = HallwayDepth * 0.5f;

        CreateBox("Floor", group, new Vector3(centerX, y, centerZ), new Vector3(width, WallThickness, HallwayDepth), _matFloor);
        CreateBox("Roof", group, new Vector3(centerX, y + FloorHeight, centerZ), new Vector3(width, WallThickness, HallwayDepth), _matFloor);
        CreateBox("Wall_North", group, new Vector3(centerX, y + FloorHeight * 0.5f, 0f), new Vector3(width, FloorHeight, WallThickness), _matGlass);
        CreateBox("Wall_South", group, new Vector3(centerX, y + FloorHeight * 0.5f, HallwayDepth), new Vector3(width, FloorHeight, WallThickness), _matGlass);
    }

    // ------------------------------------------------------------------
    //  Gym
    // ------------------------------------------------------------------

    void BuildGym(Transform parent)
    {
        var gym = new GameObject("Gym").transform;
        gym.SetParent(parent);

        float centerZ = (GymFrontZ + GymBackZ) * 0.5f;
        float halfW = GymWidth * 0.5f;

        CreateBox("Floor", gym, new Vector3(0, 0f, centerZ), new Vector3(GymWidth, WallThickness, GymDepth), _matGymFloor);
        CreateBox("Roof", gym, new Vector3(0, GymHeight, centerZ), new Vector3(GymWidth, WallThickness, GymDepth), _matFloor);
        CreateBox("Wall_Back", gym, new Vector3(0, GymHeight * 0.5f, GymBackZ), new Vector3(GymWidth, GymHeight, WallThickness), _matWall);
        CreateBox("Wall_Left", gym, new Vector3(-halfW, GymHeight * 0.5f, centerZ), new Vector3(WallThickness, GymHeight, GymDepth), _matWall);
        CreateBox("Wall_Right", gym, new Vector3(halfW, GymHeight * 0.5f, centerZ), new Vector3(WallThickness, GymHeight, GymDepth), _matWall);

        // Front wall with the corridor gap.
        float gapWidth = 12f;
        BuildWallWithGap("Wall_Front", gym, 0f, GymWidth, GymHeight * 0.5f, GymFrontZ, GymHeight, gapWidth, _matWall, true);

        BuildGymCorridor(gym);
        BuildBleachers(gym);
    }

    void BuildGymCorridor(Transform parent)
    {
        var group = new GameObject("Corridor").transform;
        group.SetParent(parent);

        float zStart = BuildingDepth;
        float zEnd = GymFrontZ;
        float depth = zEnd - zStart;
        float centerZ = (zStart + zEnd) * 0.5f;
        float width = 12f;

        CreateBox("Floor", group, new Vector3(0, 0f, centerZ), new Vector3(width, WallThickness, depth), _matFloor);
        CreateBox("Roof", group, new Vector3(0, FloorHeight, centerZ), new Vector3(width, WallThickness, depth), _matFloor);
        CreateBox("Wall_West", group, new Vector3(-width * 0.5f, FloorHeight * 0.5f, centerZ), new Vector3(WallThickness, FloorHeight, depth), _matWall);
        CreateBox("Wall_East", group, new Vector3(width * 0.5f, FloorHeight * 0.5f, centerZ), new Vector3(WallThickness, FloorHeight, depth), _matWall);
    }

    void BuildBleachers(Transform parent)
    {
        var group = new GameObject("Bleachers").transform;
        group.SetParent(parent);

        int tiers = 3;
        float tierHeight = 0.6f, tierDepth = 1.5f;
        float startZ = GymBackZ - 3f;

        for (int t = 0; t < tiers; t++)
        {
            float y = tierHeight * (t + 0.5f);
            float z = startZ - tierDepth * t;
            CreateBox($"Tier_{t}", group, new Vector3(0, y, z), new Vector3(GymWidth - 4f, tierHeight, tierDepth), _matAccent);
        }
    }

    // ------------------------------------------------------------------
    //  Player / camera
    // ------------------------------------------------------------------

    void EnsurePlayerAndCamera()
    {
        Vector3 spawn = new Vector3(0f, 1f, PerimFrontZ - 8f); // outside the gate, facing +Z into the school

        var player = GameObject.Find("Player");
        if (player == null)
        {
            player = new GameObject("Player");

            var cc = player.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.5f;
            cc.center = new Vector3(0, 1, 0);

            player.AddComponent<PrototypePlayerController>();

            var bodyVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bodyVisual.name = "BodyVisual";
            bodyVisual.transform.SetParent(player.transform);
            bodyVisual.transform.localPosition = Vector3.zero;
            Object.Destroy(bodyVisual.GetComponent<Collider>());

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                var oldIso = mainCamera.GetComponent<IsometricFollowCamera>();
                if (oldIso != null) Object.Destroy(oldIso);
                var tpc = mainCamera.GetComponent<ThirdPersonCamera>();
                if (tpc == null) tpc = mainCamera.gameObject.AddComponent<ThirdPersonCamera>();
                tpc.SetTarget(player.transform);
            }
        }

        var controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.transform.position = spawn;
        player.transform.rotation = Quaternion.identity;
        if (controller != null) controller.enabled = true;
    }

    // ------------------------------------------------------------------
    //  Helpers
    // ------------------------------------------------------------------

    static GameObject CreateBox(string name, Transform parent, Vector3 pos, Vector3 size, Material mat)
    {
        return CreatePrimitive(name, parent, PrimitiveType.Cube, pos, size, mat);
    }

    static GameObject CreatePrimitive(string name, Transform parent, PrimitiveType type, Vector3 pos, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        var r = go.GetComponent<Renderer>();
        if (r != null && mat != null) r.sharedMaterial = mat;
        return go;
    }

    // Instantiates a prop from the imported School Assets pack (Assets/school/Prefabs/Resources/props)
    // and guarantees it has a collider. Returns null (with a warning, not an error) if the name is missing.
    static GameObject SpawnProp(string prefabName, Transform parent, Vector3 localPos, Quaternion localRot)
    {
        var prefab = Resources.Load<GameObject>("props/" + prefabName);
        if (prefab == null)
        {
            Debug.LogWarning($"CampusGenerator: prop '{prefabName}' not found under Resources/props.");
            return null;
        }

        var go = Object.Instantiate(prefab, parent);
        go.name = prefabName;
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        EnsurePropCollider(go);
        return go;
    }

    static void EnsurePropCollider(GameObject go)
    {
        if (go.GetComponentInChildren<Collider>() != null) return;

        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) worldBounds.Encapsulate(renderers[i].bounds);

        Vector3 scale = go.transform.lossyScale;
        var box = go.AddComponent<BoxCollider>();
        box.center = go.transform.InverseTransformPoint(worldBounds.center);
        box.size = new Vector3(
            scale.x != 0f ? worldBounds.size.x / scale.x : worldBounds.size.x,
            scale.y != 0f ? worldBounds.size.y / scale.y : worldBounds.size.y,
            scale.z != 0f ? worldBounds.size.z / scale.z : worldBounds.size.z);
    }

    void BuildMaterials()
    {
        _matGround = MakeMaterial(new Color(0.45f, 0.6f, 0.4f));    // grass green
        _matPath = MakeMaterial(new Color(0.8f, 0.78f, 0.72f));     // pavement
        _matFloor = MakeMaterial(new Color(0.83f, 0.78f, 0.68f));   // tile, warm cream
        _matWall = MakeMaterial(new Color(0.88f, 0.82f, 0.68f));    // beige walls
        _matAccent = MakeMaterial(new Color(0.55f, 0.38f, 0.24f));  // wood/brown
        _matGlass = MakeMaterial(new Color(0.7f, 0.85f, 0.95f));    // window blue
        _matFence = MakeMaterial(new Color(0.2f, 0.2f, 0.22f));     // dark metal fence
        _matTrunk = MakeMaterial(new Color(0.35f, 0.25f, 0.2f));
        _matCanopy = MakeMaterial(new Color(0.95f, 0.7f, 0.85f));   // sakura pink
        _matGymFloor = MakeMaterial(new Color(0.75f, 0.55f, 0.3f)); // gym court tan
        _matRoofRail = MakeMaterial(new Color(0.5f, 0.5f, 0.52f));  // grey railing
    }

    static Material MakeMaterial(Color c)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        return new Material(shader) { color = c };
    }

    static Material MakeEmissiveMaterial(Color c)
    {
        var m = MakeMaterial(c);
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        if (m.HasProperty("_EmissionColor"))
            m.SetColor("_EmissionColor", c * 2f);
        return m;
    }

    // Ensures the generator runs the instant Play starts, with no manual setup.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Object.FindFirstObjectByType<CampusGenerator>() != null) return;
        new GameObject("CampusGenerator").AddComponent<CampusGenerator>();
    }
}
