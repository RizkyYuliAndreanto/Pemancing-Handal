using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FishingPondGenerator
{
    private const string RootName = "FishingPond";
    private const string OutputFolder = "Assets/FishingPondGenerated";
    private const string ScenePath = OutputFolder + "/FishingPond.unity";
    private const string FishPrefabPath = "Assets/Alstra Infinite/Fish - PolyPack/Prefabs/FishV1.prefab";
    private const string FishingAnimatorControllerPath = "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Crafting Animations/AnimatorControllers/Male/HumanM@Fishing01.controller";

    [MenuItem("Mancing Cuy/Generate Fishing Pond Scene")]
    public static void Generate()
    {
        EnsureFolder();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject(RootName);

        // --- Materials nuansa Indonesia ---
        var grass      = MakeMaterial("Grass", new Color(0.20f, 0.42f, 0.12f));
        var earth      = MakeMaterial("Earth", new Color(0.38f, 0.22f, 0.10f));
        var mud        = MakeMaterial("Mud", new Color(0.30f, 0.18f, 0.08f));
        var water      = MakeMaterial("Water", new Color(0.04f, 0.35f, 0.52f, 0.82f));
        var bamboo     = MakeMaterial("Bamboo", new Color(0.62f, 0.52f, 0.28f));
        var wood       = MakeMaterial("Wood", new Color(0.32f, 0.13f, 0.055f));
        var thatch     = MakeMaterial("Thatch", new Color(0.55f, 0.42f, 0.18f));
        var stone      = MakeMaterial("Stone", new Color(0.35f, 0.33f, 0.30f));
        var leaf       = MakeMaterial("Leaves", new Color(0.08f, 0.32f, 0.10f));
        var palmLeaf   = MakeMaterial("PalmLeaf", new Color(0.12f, 0.38f, 0.08f));
        var sand       = MakeMaterial("Path", new Color(0.58f, 0.42f, 0.22f));
        var white      = MakeMaterial("Sign", new Color(0.92f, 0.78f, 0.48f));
        var lily       = MakeMaterial("LilyPad", new Color(0.08f, 0.45f, 0.12f));
        var floatMat   = MakeMaterial("Float", new Color(0.95f, 0.08f, 0.03f));
        var terracotta = MakeMaterial("Terracotta", new Color(0.72f, 0.32f, 0.12f));
        var tarpaulin  = MakeMaterial("Tarpaulin", new Color(0.10f, 0.22f, 0.65f));
        var red        = MakeMaterial("RedAccent", new Color(0.75f, 0.12f, 0.08f));
        var concrete   = MakeMaterial("Concrete", new Color(0.55f, 0.53f, 0.50f));
        var riceGreen  = MakeMaterial("RiceField", new Color(0.35f, 0.55f, 0.15f));

        // ==========================================================
        // TERRAIN
        // ==========================================================
        CreateSolid("Ground", PrimitiveType.Cube, new Vector3(0, -0.35f, 0),
            new Vector3(70, 0.5f, 60), grass, root.transform);

        // Kolam berbentuk persegi panjang khas empang Indonesia
        CreateSolid("PondBasin", PrimitiveType.Cube, new Vector3(0, -0.6f, 0),
            new Vector3(20f, 0.8f, 14f), mud, root.transform, removeCollider: true);
        // Invisible collider walls around pond to block entry
        CreatePondBarrier(root.transform);
        CreateSolid("WaterSurface", PrimitiveType.Cube, new Vector3(0, 0.05f, 0),
            new Vector3(19.5f, 0.06f, 13.5f), water, root.transform, removeCollider: true);

        // Tanggul / tepi kolam dari batu bata
        CreatePondEdge(concrete, stone, root.transform);

        // Lily pads & decorative fish
        CreatePondDetails(lily, root.transform);

        // ==========================================================
        // JALAN SETAPAK TANAH
        // ==========================================================
        CreateSolid("PathMain", PrimitiveType.Cube, new Vector3(0, -0.08f, -12.0f),
            new Vector3(3.5f, 0.08f, 12.0f), sand, root.transform, removeCollider: true);
        CreateSolid("PathSide", PrimitiveType.Cube, new Vector3(0, -0.08f, 12.0f),
            new Vector3(3.5f, 0.08f, 8.0f), sand, root.transform, removeCollider: true);

        // ==========================================================
        // WARUNG / SAUNG khas pemancingan Indonesia
        // ==========================================================
        CreateSaung(new Vector3(-16f, 0f, -14f), bamboo, thatch, root.transform);
        CreateSaung(new Vector3(16f, 0f, -14f), bamboo, thatch, root.transform);
        CreateSaung(new Vector3(-16f, 0f, 14f), bamboo, thatch, root.transform);
        CreateSaung(new Vector3(16f, 0f, 14f), bamboo, thatch, root.transform);

        // Warung makan di belakang
        CreateWarung(new Vector3(0f, 0f, -22f), wood, thatch, tarpaulin, terracotta, root.transform);

        // ==========================================================
        // PAPAN NAMA
        // ==========================================================
        CreateSign(new Vector3(0, 1.55f, -17.5f), white, wood, root.transform);

        // ==========================================================
        // BATU-BATU DEKORASI
        // ==========================================================
        var rocks = new[]
        {
            new Vector3(-13.5f, 0.15f, -5f), new Vector3(13.5f, 0.15f, -5f),
            new Vector3(-13.5f, 0.15f, 5f), new Vector3(13.5f, 0.15f, 5f),
            new Vector3(-8f, 0.1f, 9.5f), new Vector3(8f, 0.1f, 9.5f),
        };
        foreach (var pos in rocks)
            CreateRock(pos, stone, root.transform);

        // ==========================================================
        // POHON: Kelapa + Pohon rindang
        // ==========================================================
        CreateCoconutTree(new Vector3(-22f, 0f, 0f), palmLeaf, wood, root.transform);
        CreateCoconutTree(new Vector3(22f, 0f, 0f), palmLeaf, wood, root.transform);
        CreateCoconutTree(new Vector3(-18f, 0f, 18f), palmLeaf, wood, root.transform);
        CreateCoconutTree(new Vector3(18f, 0f, 18f), palmLeaf, wood, root.transform);
        CreateTree(new Vector3(-26f, 0f, -10f), leaf, wood, root.transform);
        CreateTree(new Vector3(26f, 0f, -10f), leaf, wood, root.transform);
        CreateTree(new Vector3(-26f, 0f, 12f), leaf, wood, root.transform);
        CreateTree(new Vector3(26f, 0f, 12f), leaf, wood, root.transform);

        // ==========================================================
        // AREA SAWAH di latar belakang
        // ==========================================================
        CreateSolid("RiceFieldLeft", PrimitiveType.Cube, new Vector3(-28f, -0.15f, 20f),
            new Vector3(14f, 0.1f, 16f), riceGreen, root.transform, removeCollider: true);
        CreateSolid("RiceFieldRight", PrimitiveType.Cube, new Vector3(28f, -0.15f, 20f),
            new Vector3(14f, 0.1f, 16f), riceGreen, root.transform, removeCollider: true);

        // ==========================================================
        // BANGKU DUDUK
        // ==========================================================
        CreateBench(new Vector3(-13f, 0.35f, -10f), wood, root.transform);
        CreateBench(new Vector3(13f, 0.35f, -10f), wood, root.transform);

        // ==========================================================
        // FISHING STATIONS (tempat pancing pinggir kolam)
        // ==========================================================
        CreateFishingStation(new Vector3(-12.5f, 0.2f, -8.5f), bamboo, floatMat, root.transform);
        CreateFishingStation(new Vector3(12.5f, 0.2f, -8.5f), bamboo, floatMat, root.transform);
        CreateFishingStation(new Vector3(-12.5f, 0.2f, 8.5f), bamboo, floatMat, root.transform);
        CreateFishingStation(new Vector3(12.5f, 0.2f, 8.5f), bamboo, floatMat, root.transform);

        // Ember umpan & terpal
        CreateSolid("BaitBucket1", PrimitiveType.Cylinder, new Vector3(-14f, 0.4f, -9f),
            new Vector3(0.5f, 0.4f, 0.5f), terracotta, root.transform);
        CreateSolid("BaitBucket2", PrimitiveType.Cylinder, new Vector3(14f, 0.4f, -9f),
            new Vector3(0.5f, 0.4f, 0.5f), terracotta, root.transform);

        // ==========================================================
        // CHARACTERS
        // ==========================================================
        var player = CreateCharacter("Player", new Vector3(0, 0.15f, -15.0f), true, root.transform, "Assets/Floreswa/Prefabs/male01_1.prefab");
        CreateCharacter("Bot_A", new Vector3(-16f, 0.15f, -10f), false, root.transform, "Assets/Floreswa/Prefabs/male02_1.prefab");
        CreateCharacter("Bot_B", new Vector3(16f, 0.15f, -10f), false, root.transform, "Assets/Floreswa/Prefabs/male03_1.prefab");
        CreateCharacter("Bot_C", new Vector3(-16f, 0.15f, 10f), false, root.transform, "Assets/Floreswa/Prefabs/male01_2.prefab");
        CreateCharacter("Bot_D", new Vector3(16f, 0.15f, 10f), false, root.transform, "Assets/Floreswa/Prefabs/male02_2.prefab");
        root.AddComponent<FishingLeaderboardController>();

        // ==========================================================
        // LIGHTING — suasana tropis sore
        // ==========================================================
        var lightObject = new GameObject("Sun");
        lightObject.transform.SetParent(root.transform);
        lightObject.transform.rotation = Quaternion.Euler(42, -30, 0);
        var sun = lightObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.95f, 0.82f); // warm tropical
        sun.intensity = 1.3f;
        sun.shadows = LightShadows.Soft;

        // ==========================================================
        // CAMERA
        // ==========================================================
        var cameraObject = new GameObject("PondCamera");
        cameraObject.transform.SetParent(root.transform);
        cameraObject.transform.position = new Vector3(28f, 22f, -32f);
        cameraObject.transform.rotation = Quaternion.Euler(32, 40, 0);
        var camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 55;
        camera.tag = "MainCamera";
        var followCamera = cameraObject.AddComponent<FishingThirdPersonCamera>();
        followCamera.target = player.transform;
        followCamera.offset = new Vector3(0, 5.5f, -8.5f);
        followCamera.lookHeight = 1.35f;

        // ==========================================================
        // RENDER SETTINGS — kabut tropis
        // ==========================================================
        RenderSettings.ambientLight = new Color(0.50f, 0.55f, 0.45f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.60f, 0.70f, 0.55f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 40;
        RenderSettings.fogEndDistance = 100;

        EditorSceneManager.SaveScene(scene, ScenePath);
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));
        Debug.Log("Kolam pemancingan Indonesia generated at " + ScenePath);
    }

    // ====================================================================
    // POND BARRIER — invisible walls so player/bot can't walk into water
    // ====================================================================
    private static void CreatePondBarrier(Transform parent)
    {
        var barrier = new GameObject("PondBarrier");
        barrier.transform.SetParent(parent);
        barrier.transform.localPosition = Vector3.zero;

        // Four walls around the pond (slightly inside the visual edge)
        CreateInvisibleWall("BarrierNorth", new Vector3(0, 0.5f, 7.2f), new Vector3(20.5f, 1.5f, 0.5f), barrier.transform);
        CreateInvisibleWall("BarrierSouth", new Vector3(0, 0.5f, -7.2f), new Vector3(20.5f, 1.5f, 0.5f), barrier.transform);
        CreateInvisibleWall("BarrierEast", new Vector3(10.2f, 0.5f, 0), new Vector3(0.5f, 1.5f, 14.9f), barrier.transform);
        CreateInvisibleWall("BarrierWest", new Vector3(-10.2f, 0.5f, 0), new Vector3(0.5f, 1.5f, 14.9f), barrier.transform);
    }

    private static void CreateInvisibleWall(string name, Vector3 pos, Vector3 size, Transform parent)
    {
        var wall = new GameObject(name);
        wall.transform.SetParent(parent);
        wall.transform.localPosition = pos;
        var col = wall.AddComponent<BoxCollider>();
        col.size = size;
        wall.layer = 0; // Default layer — CharacterController hits it
    }

    // ====================================================================
    // TEPI KOLAM — bata/beton khas empang
    // ====================================================================
    private static void CreatePondEdge(Material concrete, Material stone, Transform parent)
    {
        var edge = new GameObject("PondEdge");
        edge.transform.SetParent(parent);
        // Rectangular rim using cubes (like Indonesian fish pond concrete edges)
        CreateSolid("EdgeNorth", PrimitiveType.Cube, new Vector3(0, 0.15f, 7.0f), new Vector3(20.5f, 0.4f, 0.6f), concrete, edge.transform);
        CreateSolid("EdgeSouth", PrimitiveType.Cube, new Vector3(0, 0.15f, -7.0f), new Vector3(20.5f, 0.4f, 0.6f), concrete, edge.transform);
        CreateSolid("EdgeEast", PrimitiveType.Cube, new Vector3(10.0f, 0.15f, 0), new Vector3(0.6f, 0.4f, 14.6f), concrete, edge.transform);
        CreateSolid("EdgeWest", PrimitiveType.Cube, new Vector3(-10.0f, 0.15f, 0), new Vector3(0.6f, 0.4f, 14.6f), concrete, edge.transform);
        // Corner accent stones
        foreach (var cx in new[] { -10f, 10f })
            foreach (var cz in new[] { -7f, 7f })
                CreateSolid("CornerStone", PrimitiveType.Sphere, new Vector3(cx, 0.2f, cz), new Vector3(1f, 0.5f, 1f), stone, edge.transform);
    }

    // ====================================================================
    // SAUNG — gazebo bambu khas pemancingan
    // ====================================================================
    private static void CreateSaung(Vector3 pos, Material bamboo, Material thatch, Transform parent)
    {
        var saung = new GameObject("Saung");
        saung.transform.SetParent(parent);
        saung.transform.position = pos;

        // Lantai
        CreateSolid("Floor", PrimitiveType.Cube, new Vector3(0, 0.15f, 0), new Vector3(4.5f, 0.3f, 3.5f), bamboo, saung.transform);
        // 4 tiang bambu
        foreach (var x in new[] { -1.8f, 1.8f })
            foreach (var z in new[] { -1.3f, 1.3f })
                CreateSolid("Pillar", PrimitiveType.Cylinder, new Vector3(x, 1.8f, z), new Vector3(0.14f, 1.6f, 0.14f), bamboo, saung.transform);
        // Atap ilalang/jerami
        CreateSolid("Roof", PrimitiveType.Cube, new Vector3(0, 3.6f, 0), new Vector3(5.2f, 0.25f, 4.2f), thatch, saung.transform, removeCollider: true);
        // Atap miring (apex)
        var apex = CreateSolid("RoofApex", PrimitiveType.Cube, new Vector3(0, 3.95f, 0), new Vector3(4.2f, 0.2f, 3.0f), thatch, saung.transform, removeCollider: true);
        apex.transform.localRotation = Quaternion.Euler(0, 0, 0);
    }

    // ====================================================================
    // WARUNG MAKAN
    // ====================================================================
    private static void CreateWarung(Vector3 pos, Material wood, Material thatch, Material tarpaulin, Material terracotta, Transform parent)
    {
        var warung = new GameObject("Warung");
        warung.transform.SetParent(parent);
        warung.transform.position = pos;

        // Bangunan utama
        CreateSolid("Floor", PrimitiveType.Cube, new Vector3(0, 0.1f, 0), new Vector3(8f, 0.2f, 4f), wood, warung.transform);
        // Dinding belakang
        CreateSolid("BackWall", PrimitiveType.Cube, new Vector3(0, 1.5f, -1.8f), new Vector3(8f, 2.8f, 0.2f), wood, warung.transform);
        // Dinding samping
        CreateSolid("SideWallL", PrimitiveType.Cube, new Vector3(-3.9f, 1.5f, 0), new Vector3(0.2f, 2.8f, 3.8f), wood, warung.transform);
        CreateSolid("SideWallR", PrimitiveType.Cube, new Vector3(3.9f, 1.5f, 0), new Vector3(0.2f, 2.8f, 3.8f), wood, warung.transform);
        // Atap
        CreateSolid("Roof", PrimitiveType.Cube, new Vector3(0, 3.1f, 0.3f), new Vector3(9f, 0.2f, 5f), thatch, warung.transform, removeCollider: true);
        // Meja
        CreateSolid("Counter", PrimitiveType.Cube, new Vector3(0, 0.9f, 1.4f), new Vector3(6f, 0.15f, 1.2f), wood, warung.transform);
        // Terpal biru khas
        CreateSolid("Tarpaulin", PrimitiveType.Cube, new Vector3(0, 3.3f, 1.8f), new Vector3(8.5f, 0.05f, 2f), tarpaulin, warung.transform, removeCollider: true);
        // Pot tanaman
        CreateSolid("Pot1", PrimitiveType.Cylinder, new Vector3(-3.2f, 0.5f, 1.8f), new Vector3(0.5f, 0.5f, 0.5f), terracotta, warung.transform);
        CreateSolid("Pot2", PrimitiveType.Cylinder, new Vector3(3.2f, 0.5f, 1.8f), new Vector3(0.5f, 0.5f, 0.5f), terracotta, warung.transform);
    }

    // ====================================================================
    // POHON KELAPA
    // ====================================================================
    private static void CreateCoconutTree(Vector3 pos, Material palmLeaf, Material trunk, Transform parent)
    {
        var tree = new GameObject("CoconutTree");
        tree.transform.SetParent(parent);
        tree.transform.position = pos;
        // Batang melengkung (approx with tilted cylinder)
        var trunkObj = CreateSolid("Trunk", PrimitiveType.Cylinder, new Vector3(0, 3.5f, 0.5f),
            new Vector3(0.35f, 3.5f, 0.35f), trunk, tree.transform);
        trunkObj.transform.localRotation = Quaternion.Euler(0, 0, 5f);
        // Daun kelapa — beberapa ellipsoid tipis menyebar
        for (var i = 0; i < 5; i++)
        {
            var angle = i * 72f;
            var rad = angle * Mathf.Deg2Rad;
            var lx = Mathf.Cos(rad) * 1.5f;
            var lz = Mathf.Sin(rad) * 1.5f;
            var frond = CreateSolid("Frond", PrimitiveType.Cube, new Vector3(lx, 7.2f, lz + 0.5f),
                new Vector3(0.3f, 0.1f, 2.8f), palmLeaf, tree.transform, removeCollider: true);
            frond.transform.localRotation = Quaternion.Euler(-15f, angle, 0);
        }
        // Crown center
        CreateSolid("Crown", PrimitiveType.Sphere, new Vector3(0, 7.2f, 0.5f),
            new Vector3(1.5f, 1.0f, 1.5f), palmLeaf, tree.transform, removeCollider: true);
    }

    // ====================================================================
    // POHON RINDANG (existing style)
    // ====================================================================
    private static void CreateTree(Vector3 position, Material leaves, Material trunk, Transform parent)
    {
        var tree = new GameObject("Tree");
        tree.transform.SetParent(parent);
        tree.transform.position = position;
        CreateSolid("Trunk", PrimitiveType.Cylinder, new Vector3(0, 2.0f, 0),
            new Vector3(0.55f, 2.0f, 0.55f), trunk, tree.transform);
        CreateSolid("Crown", PrimitiveType.Sphere, new Vector3(0, 4.8f, 0),
            new Vector3(3.8f, 3.2f, 3.8f), leaves, tree.transform, removeCollider: true);
        CreateSolid("Crown2", PrimitiveType.Sphere, new Vector3(1.2f, 4.3f, 0.2f),
            new Vector3(2.4f, 2.4f, 2.4f), leaves, tree.transform, removeCollider: true);
    }

    // ====================================================================
    // BANGKU
    // ====================================================================
    private static void CreateBench(Vector3 position, Material material, Transform parent)
    {
        var bench = new GameObject("Bench");
        bench.transform.SetParent(parent);
        bench.transform.position = position;
        CreateSolid("Seat", PrimitiveType.Cube, Vector3.zero, new Vector3(2.6f, 0.22f, 0.65f), material, bench.transform);
        CreateSolid("Back", PrimitiveType.Cube, new Vector3(0, 0.75f, 0.25f), new Vector3(2.6f, 0.85f, 0.18f), material, bench.transform);
        foreach (var x in new[] { -1.0f, 1.0f })
            CreateSolid("Leg", PrimitiveType.Cube, new Vector3(x, -0.45f, 0), new Vector3(0.18f, 0.9f, 0.45f), material, bench.transform);
    }

    // ====================================================================
    // PAPAN NAMA
    // ====================================================================
    private static void CreateSign(Vector3 position, Material sign, Material post, Transform parent)
    {
        CreateSolid("SignPost", PrimitiveType.Cylinder, position + new Vector3(0, -0.85f, 0),
            new Vector3(0.12f, 0.85f, 0.12f), post, parent);
        CreateSolid("FishingPondSign", PrimitiveType.Cube, position,
            new Vector3(3.6f, 1.05f, 0.15f), sign, parent);
    }

    // ====================================================================
    // FISHING STATION
    // ====================================================================
    private static void CreateFishingStation(Vector3 position, Material bamboo, Material floatMat, Transform parent)
    {
        var station = new GameObject("FishingStation");
        station.transform.SetParent(parent);
        station.transform.position = position;
        CreateSolid("Stool", PrimitiveType.Cylinder, Vector3.zero,
            new Vector3(1.1f, 0.45f, 1.1f), bamboo, station.transform);
        var rod = CreateSolid("FishingRod", PrimitiveType.Cylinder, new Vector3(0.75f, 2.0f, 0),
            new Vector3(0.08f, 2.1f, 0.08f), bamboo, station.transform, removeCollider: true);
        rod.transform.localRotation = Quaternion.Euler(0, 0, -18);
        var line = CreateSolid("FishingLine", PrimitiveType.Cylinder, new Vector3(1.35f, 0.52f, 0),
            new Vector3(0.025f, 1.8f, 0.025f), MakeMaterial("FishingLine", new Color(0.92f, 0.92f, 0.85f)), station.transform, removeCollider: true);
        line.transform.localRotation = Quaternion.Euler(0, 0, -8);
        CreateSolid("Float", PrimitiveType.Sphere, new Vector3(1.62f, 0.30f, 0),
            new Vector3(0.18f, 0.28f, 0.18f), floatMat, station.transform, removeCollider: true);
        CreateSolid("BaitBucket", PrimitiveType.Cylinder, new Vector3(-0.85f, 0.55f, 0.35f),
            new Vector3(0.65f, 0.55f, 0.65f), bamboo, station.transform);
    }

    // ====================================================================
    // POND DETAILS (lily pads, fish)
    // ====================================================================
    private static void CreatePondDetails(Material lily, Transform parent)
    {
        var pads = new[]
        {
            new Vector3(-4.8f, 0.12f, 2.8f), new Vector3(4.6f, 0.12f, 2.1f),
            new Vector3(1.6f, 0.12f, -1.8f), new Vector3(-1.5f, 0.12f, 3.8f),
            new Vector3(-3f, 0.12f, -3f), new Vector3(5f, 0.12f, -2f),
        };
        foreach (var pos in pads)
        {
            var pad = CreateSolid("LilyPad", PrimitiveType.Cylinder, pos,
                new Vector3(0.8f, 0.02f, 0.55f), lily, parent, removeCollider: true);
            pad.transform.rotation = Quaternion.Euler(0, pos.x * 17, 0);
        }

        var fishPrefabs = new[]
        {
            "Assets/Alstra Infinite/Fish - PolyPack/Prefabs/FishV1.prefab",
            "Assets/Alstra Infinite/Fish - PolyPack/Prefabs/FishV2.prefab",
            "Assets/Alstra Infinite/Fish - PolyPack/Prefabs/FishV3.prefab",
            "Assets/Alstra Infinite/Fish - PolyPack/Prefabs/FishV4.prefab"
        };
        var fishPositions = new[]
        {
            new Vector3(-2.4f, 0.1f, 0.4f), new Vector3(3f, 0.1f, -2.6f),
            new Vector3(5.2f, 0.1f, 3.4f), new Vector3(-5.4f, 0.1f, -2.8f)
        };
        for (var i = 0; i < fishPositions.Length; i++)
            CreateFishFromPack(fishPrefabs[i % fishPrefabs.Length], fishPositions[i], i * 67f, parent);
    }

    private static void CreateFishFromPack(string prefabPath, Vector3 position, float yaw, Transform parent)
    {
        var fishPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (fishPrefab == null) return;
        var fish = PrefabUtility.InstantiatePrefab(fishPrefab) as GameObject;
        fish.name = "DecorativeFish_" + prefabPath.Substring(prefabPath.LastIndexOf('/') + 1).Replace(".prefab", "");
        fish.transform.SetParent(parent, false);
        fish.transform.localPosition = position;
        fish.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        fish.transform.localScale = Vector3.one * 0.7f;
    }

    // ====================================================================
    // BATU
    // ====================================================================
    private static void CreateRock(Vector3 position, Material material, Transform parent)
    {
        var rock = CreateSolid("Rock", PrimitiveType.Sphere, position,
            new Vector3(1.5f, 0.7f, 1.1f), material, parent);
        rock.transform.rotation = Quaternion.Euler(0, position.x * 8, position.z * 5);
    }

    // ====================================================================
    // CHARACTER with CharacterController
    // ====================================================================
    private static GameObject CreateCharacter(string name, Vector3 position, bool playable, Transform parent, string prefabPath)
    {
        var character = new GameObject(name);
        character.transform.SetParent(parent);
        character.transform.position = position;

        var characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (characterPrefab == null)
            throw new System.InvalidOperationException($"Low-poly character prefab tidak ditemukan: {prefabPath}");

        var visual = PrefabUtility.InstantiatePrefab(characterPrefab) as GameObject;
        visual.name = "LowPolyCharacter";
        visual.transform.SetParent(character.transform, false);
        visual.transform.localPosition = new Vector3(0f, -0.18f, 0f);
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        character.AddComponent<FishingLocomotionAnimator>();

        var fishPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FishPrefabPath);
        if (fishPrefab == null)
            throw new System.InvalidOperationException($"Fish PolyPack prefab tidak ditemukan: {FishPrefabPath}");
        var fishingAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(FishingAnimatorControllerPath);
        if (fishingAnimatorController == null)
            throw new System.InvalidOperationException($"Fishing animator controller tidak ditemukan: {FishingAnimatorControllerPath}");
        var animator = visual.GetComponentInChildren<Animator>();
        if (animator == null)
            animator = visual.AddComponent<Animator>();
        var modelPath = "Assets/Floreswa/Models/" + Path.GetFileNameWithoutExtension(prefabPath) + ".fbx";
        var modelAvatar = FindAvatar(modelPath);
        if (modelAvatar == null)
            throw new System.InvalidOperationException($"Avatar Humanoid tidak ditemukan pada model: {modelPath}. Reimport model dengan Rig=Humanoid.");
        animator.avatar = modelAvatar;
        animator.runtimeAnimatorController = fishingAnimatorController;

        // CharacterController for physics collision
        var cc = character.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0, 0.9f, 0);
        cc.slopeLimit = 45f;
        cc.stepOffset = 0.3f;

        var lineMaterial = MakeMaterial("FishingLine", new Color(0.92f, 0.92f, 0.85f));
        var rodMaterial = MakeMaterial("FishingRod", new Color(0.32f, 0.10f, 0.025f));
        var reelMaterial = MakeMaterial("FishingReel", new Color(0.08f, 0.08f, 0.09f));
        var rod = new GameObject("HeldFishingRod");
        rod.transform.SetParent(character.transform, false);
        rod.transform.localPosition = new Vector3(0.38f, 1.02f, 0.34f);
        rod.transform.localRotation = Quaternion.Euler(25f, 0f, -18f);
        var shaft = CreateSolid("RodShaft", PrimitiveType.Cylinder, new Vector3(0f, 0.82f, 0f),
            new Vector3(0.035f, 0.82f, 0.035f), rodMaterial, rod.transform, removeCollider: true);
        shaft.transform.localRotation = Quaternion.identity;
        CreateSolid("RodHandle", PrimitiveType.Cylinder, new Vector3(0f, 0.18f, 0f),
            new Vector3(0.075f, 0.22f, 0.075f), rodMaterial, rod.transform, removeCollider: true);
        var rightGrip = new GameObject("RodRightGrip");
        rightGrip.transform.SetParent(rod.transform, false);
        rightGrip.transform.localPosition = new Vector3(0f, 0.27f, 0f);
        var leftGrip = new GameObject("RodLeftGrip");
        leftGrip.transform.SetParent(rod.transform, false);
        leftGrip.transform.localPosition = new Vector3(0f, 0.50f, 0f);
        var reel = CreateSolid("RodReel", PrimitiveType.Cylinder, new Vector3(0f, 0.38f, 0f),
            new Vector3(0.13f, 0.055f, 0.13f), reelMaterial, rod.transform, removeCollider: true);
        reel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var tip = new GameObject("Pole_Tip");
        tip.transform.SetParent(rod.transform, false);
        tip.transform.localPosition = new Vector3(0f, 1.64f, 0f);
        var line = CreateSolid("HeldFishingLine", PrimitiveType.Cylinder, new Vector3(0.98f, 0.48f, 0.63f),
            new Vector3(0.012f, 0.95f, 0.012f), lineMaterial, character.transform, removeCollider: true);
        line.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);

        if (playable)
        {
            var controller = character.AddComponent<FishingPlayerController>();
            controller.moveSpeed = 4.5f;
            controller.fishPrefab = fishPrefab;
            controller.fishingAnimatorController = fishingAnimatorController;
        }
        else
        {
            var bot = character.AddComponent<FishingBot>();
            bot.homePosition = position;
            bot.wanderRadius = 3.0f;
            bot.fishPrefab = fishPrefab;
            bot.fishingAnimatorController = fishingAnimatorController;
        }
        return character;
    }

    // ====================================================================
    // HELPERS
    // ====================================================================
    private static Avatar FindAvatar(string modelPath)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
        for (var i = 0; i < assets.Length; i++)
            if (assets[i] is Avatar avatar && avatar.isValid && avatar.isHuman)
                return avatar;
        return null;
    }

    /// <summary>
    /// CreatePrimitive that keeps collider by default (solid obstacle).
    /// Pass removeCollider:true for decorative objects player shouldn't bump into.
    /// </summary>
    private static GameObject CreateSolid(string name, PrimitiveType type, Vector3 position,
        Vector3 scale, Material material, Transform parent, bool removeCollider = false)
    {
        var obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = position;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        if (removeCollider)
            Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj;
    }

    private static Material MakeMaterial(string name, Color color)
    {
        var path = OutputFolder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        if (name == "Water")
        {
            material.color = new Color(0.02f, 0.30f, 0.55f, 1f);
            material.SetColor("_BaseColor", new Color(0.02f, 0.30f, 0.55f, 1f));
            material.SetFloat("_Surface", 0);
            material.SetFloat("_AlphaClip", 0);
            material.renderQueue = 2000;
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/FishingPondGenerated"))
            AssetDatabase.CreateFolder("Assets", "FishingPondGenerated");
        if (!AssetDatabase.IsValidFolder("Assets/Editor"))
            AssetDatabase.CreateFolder("Assets", "Editor");
        AssetDatabase.Refresh();
    }
}
