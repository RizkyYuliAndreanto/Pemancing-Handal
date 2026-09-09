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

        var grass = MakeMaterial("Grass", new Color(0.18f, 0.38f, 0.14f));
        var earth = MakeMaterial("Earth", new Color(0.35f, 0.20f, 0.10f));
        var water = MakeMaterial("Water", new Color(0.04f, 0.35f, 0.52f, 0.82f));
        var wood = MakeMaterial("Wood", new Color(0.32f, 0.13f, 0.055f));
        var roof = MakeMaterial("Roof", new Color(0.45f, 0.10f, 0.045f));
        var stone = MakeMaterial("Stone", new Color(0.28f, 0.30f, 0.27f));
        var leaf = MakeMaterial("Leaves", new Color(0.08f, 0.30f, 0.10f));
        var sand = MakeMaterial("Path", new Color(0.64f, 0.48f, 0.28f));
        var white = MakeMaterial("Sign", new Color(0.92f, 0.78f, 0.48f));
        var lily = MakeMaterial("LilyPad", new Color(0.08f, 0.45f, 0.12f));
        var floatMaterial = MakeMaterial("Float", new Color(0.95f, 0.08f, 0.03f));

        CreatePrimitive("Ground", PrimitiveType.Cube, new Vector3(0, -0.35f, 0),
            new Vector3(60, 0.5f, 46), grass, root.transform);
        // The bank is kept below the water surface so it cannot hide the pond.
        CreatePrimitive("PondBasin", PrimitiveType.Cylinder, new Vector3(0, -0.24f, 0),
            new Vector3(25.0f, 0.25f, 17.5f), earth, root.transform);
        CreatePrimitive("WaterSurface", PrimitiveType.Cylinder, new Vector3(0, 0.12f, 0),
            new Vector3(24.0f, 0.06f, 16.3f), water, root.transform);
        CreatePondRim(stone, root.transform);
        CreatePondDetails(lily, root.transform);

        CreatePath(new Vector3(0, -0.08f, -15.0f), 4.0f, 10.0f, sand, root.transform);
        CreateDock(new Vector3(0, 0.38f, -20.0f), wood, root.transform);
        CreateGazebo(new Vector3(28.0f, 0.0f, 22.0f), wood, roof, root.transform);
        CreateSign(new Vector3(0, 1.55f, -21.0f), white, wood, root.transform);

        var rocks = new[]
        {
            new Vector3(-10.1f, 0.22f, -3.8f), new Vector3(-8.7f, 0.18f, 4.6f),
            new Vector3(-3.5f, 0.12f, 7.3f), new Vector3(4.2f, 0.18f, 7.1f),
            new Vector3(9.4f, 0.22f, 1.4f), new Vector3(8.2f, 0.16f, -4.4f)
        };
        foreach (var position in rocks)
            CreateRock(position, stone, root.transform);

        CreateTree(new Vector3(-19.0f, 0.0f, 11.0f), leaf, wood, root.transform);
        CreateTree(new Vector3(-19.0f, 0.0f, -12.0f), leaf, wood, root.transform);
        CreateTree(new Vector3(19.0f, 0.0f, -12.0f), leaf, wood, root.transform);
        CreateTree(new Vector3(19.0f, 0.0f, 8.0f), leaf, wood, root.transform);
        CreateBench(new Vector3(-25.0f, 0.35f, -18.0f), wood, root.transform);
        CreateBench(new Vector3(25.0f, 0.35f, -18.0f), wood, root.transform);
        CreateFishingStation(new Vector3(-25.0f, 0.35f, -14.0f), wood, floatMaterial, root.transform);
        CreateFishingStation(new Vector3(25.0f, 0.35f, -14.0f), wood, floatMaterial, root.transform);
        var player = CreateCharacter("Player", new Vector3(0, 0.15f, -21.0f), true, root.transform, "Assets/Floreswa/Prefabs/male01_1.prefab");
        CreateCharacter("Bot_A", new Vector3(-22.0f, 0.15f, -13.0f), false, root.transform, "Assets/Floreswa/Prefabs/male02_1.prefab");
        CreateCharacter("Bot_B", new Vector3(22.0f, 0.15f, -13.0f), false, root.transform, "Assets/Floreswa/Prefabs/male03_1.prefab");
        CreateCharacter("Bot_C", new Vector3(-22.0f, 0.15f, 13.0f), false, root.transform, "Assets/Floreswa/Prefabs/male01_2.prefab");
        CreateCharacter("Bot_D", new Vector3(22.0f, 0.15f, 13.0f), false, root.transform, "Assets/Floreswa/Prefabs/male02_2.prefab");
        root.AddComponent<FishingLeaderboardController>();

        var lightObject = new GameObject("Sun");
        lightObject.transform.SetParent(root.transform);
        lightObject.transform.rotation = Quaternion.Euler(48, -32, 0);
        var sun = lightObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.2f;
        sun.shadows = LightShadows.Soft;

        var cameraObject = new GameObject("PondCamera");
        cameraObject.transform.SetParent(root.transform);
        cameraObject.transform.position = new Vector3(28.0f, 22.0f, -32.0f);
        cameraObject.transform.rotation = Quaternion.Euler(32, 40, 0);
        var camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 55;
        camera.tag = "MainCamera";
        var followCamera = cameraObject.AddComponent<FishingThirdPersonCamera>();
        followCamera.target = player.transform;
        followCamera.offset = new Vector3(0, 5.5f, -8.5f);
        followCamera.lookHeight = 1.35f;

        RenderSettings.ambientLight = new Color(0.48f, 0.55f, 0.62f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.55f, 0.68f, 0.72f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 35;
        RenderSettings.fogEndDistance = 90;

        EditorSceneManager.SaveScene(scene, ScenePath);
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));
        Debug.Log("Fishing pond generated at " + ScenePath);
    }

    private static void CreateDock(Vector3 position, Material material, Transform parent)
    {
        var dock = new GameObject("FishingDock");
        dock.transform.SetParent(parent);
        dock.transform.position = position;
        CreatePrimitive("Platform", PrimitiveType.Cube, Vector3.zero, new Vector3(4.2f, 0.35f, 4.8f), material, dock.transform);
        for (var x = -1.5f; x <= 1.5f; x += 1.5f)
            CreatePrimitive("Post", PrimitiveType.Cylinder, new Vector3(x, -0.9f, -1.65f), new Vector3(0.18f, 1.5f, 0.18f), material, dock.transform);
        CreatePrimitive("RailLeft", PrimitiveType.Cube, new Vector3(-1.95f, 0.85f, 0.5f), new Vector3(0.15f, 1.1f, 3.2f), material, dock.transform);
        CreatePrimitive("RailRight", PrimitiveType.Cube, new Vector3(1.95f, 0.85f, 0.5f), new Vector3(0.15f, 1.1f, 3.2f), material, dock.transform);
    }

    private static void CreateGazebo(Vector3 position, Material wood, Material roof, Transform parent)
    {
        var gazebo = new GameObject("FishingGazebo");
        gazebo.transform.SetParent(parent);
        gazebo.transform.position = position;
        CreatePrimitive("Floor", PrimitiveType.Cube, Vector3.zero, new Vector3(5.0f, 0.3f, 4.2f), wood, gazebo.transform);
        for (var x = -2.0f; x <= 2.0f; x += 4.0f)
            for (var z = -1.55f; z <= 1.55f; z += 3.1f)
                CreatePrimitive("Post", PrimitiveType.Cylinder, new Vector3(x, 1.8f, z), new Vector3(0.16f, 1.8f, 0.16f), wood, gazebo.transform);
        CreatePrimitive("Roof", PrimitiveType.Cube, new Vector3(0, 4.0f, 0), new Vector3(5.8f, 0.35f, 5.0f), roof, gazebo.transform);
    }

    private static void CreateSign(Vector3 position, Material sign, Material post, Transform parent)
    {
        CreatePrimitive("SignPost", PrimitiveType.Cylinder, position + new Vector3(0, -0.85f, 0),
            new Vector3(0.12f, 0.85f, 0.12f), post, parent);
        CreatePrimitive("FishingPondSign", PrimitiveType.Cube, position,
            new Vector3(3.6f, 1.05f, 0.15f), sign, parent);
    }

    private static void CreateTree(Vector3 position, Material leaves, Material trunk, Transform parent)
    {
        var tree = new GameObject("Tree");
        tree.transform.SetParent(parent);
        tree.transform.position = position;
        CreatePrimitive("Trunk", PrimitiveType.Cylinder, new Vector3(0, 2.0f, 0),
            new Vector3(0.55f, 2.0f, 0.55f), trunk, tree.transform);
        CreatePrimitive("Crown", PrimitiveType.Sphere, new Vector3(0, 4.8f, 0),
            new Vector3(3.8f, 3.2f, 3.8f), leaves, tree.transform);
        CreatePrimitive("Crown", PrimitiveType.Sphere, new Vector3(1.2f, 4.3f, 0.2f),
            new Vector3(2.4f, 2.4f, 2.4f), leaves, tree.transform);
    }

    private static void CreateBench(Vector3 position, Material material, Transform parent)
    {
        var bench = new GameObject("Bench");
        bench.transform.SetParent(parent);
        bench.transform.position = position;
        CreatePrimitive("Seat", PrimitiveType.Cube, Vector3.zero, new Vector3(2.6f, 0.22f, 0.65f), material, bench.transform);
        CreatePrimitive("Back", PrimitiveType.Cube, new Vector3(0, 0.75f, 0.25f), new Vector3(2.6f, 0.85f, 0.18f), material, bench.transform);
        foreach (var x in new[] { -1.0f, 1.0f })
            CreatePrimitive("Leg", PrimitiveType.Cube, new Vector3(x, -0.45f, 0), new Vector3(0.18f, 0.9f, 0.45f), material, bench.transform);
    }

    private static void CreatePondDetails(Material lily, Transform parent)
    {
        var pads = new[]
        {
            new Vector3(-4.8f, 0.30f, 2.8f), new Vector3(4.6f, 0.31f, 2.1f),
            new Vector3(1.6f, 0.30f, -1.8f), new Vector3(-1.5f, 0.30f, 3.8f)
        };
        foreach (var position in pads)
        {
            var pad = CreatePrimitive("LilyPad", PrimitiveType.Cylinder, position,
                new Vector3(1.0f, 0.025f, 0.65f), lily, parent);
            pad.transform.rotation = Quaternion.Euler(0, position.x * 13, 0);
        }

        var fishPositions = new[]
        {
            new Vector3(-2.4f, 0.34f, 0.4f),
            new Vector3(3.0f, 0.34f, -2.6f),
            new Vector3(5.2f, 0.34f, 3.4f),
            new Vector3(-5.4f, 0.34f, -2.8f)
        };
        var fishPrefabs = new[]
        {
            "Assets/Alstra Infinite/Fish - PolyPack/Prefabs/FishV1.prefab",
            "Assets/Alstra Infinite/Fish - PolyPack/Prefabs/FishV2.prefab",
            "Assets/Alstra Infinite/Fish - PolyPack/Prefabs/FishV3.prefab",
            "Assets/Alstra Infinite/Fish - PolyPack/Prefabs/FishV4.prefab"
        };
        for (var i = 0; i < fishPositions.Length; i++)
            CreateFishFromPack(fishPrefabs[i % fishPrefabs.Length], fishPositions[i], i * 67f, parent);
    }

    private static void CreateFishFromPack(string prefabPath, Vector3 position, float yaw, Transform parent)
    {
        var fishPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (fishPrefab == null)
            throw new System.InvalidOperationException($"Fish PolyPack prefab tidak ditemukan: {prefabPath}");

        var fish = PrefabUtility.InstantiatePrefab(fishPrefab) as GameObject;
        fish.name = "DecorativeFish_" + prefabPath.Substring(prefabPath.LastIndexOf('/') + 1).Replace(".prefab", string.Empty);
        fish.transform.SetParent(parent, false);
        fish.transform.localPosition = position;
        fish.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        fish.transform.localScale = Vector3.one * 0.7f;
    }

    private static void CreatePondRim(Material stone, Transform parent)
    {
        var rim = new[]
        {
            new Vector3(-12.5f, 0.13f, -12.2f), new Vector3(0, 0.13f, -15.2f),
            new Vector3(12.5f, 0.13f, -12.2f), new Vector3(18.5f, 0.13f, 0),
            new Vector3(12.5f, 0.13f, 12.2f), new Vector3(0, 0.13f, 15.2f),
            new Vector3(-12.5f, 0.13f, 12.2f), new Vector3(-18.5f, 0.13f, 0)
        };
        foreach (var position in rim)
            CreateRock(position, stone, parent);
    }

    private static void CreateFishingStation(Vector3 position, Material wood, Material floatMaterial, Transform parent)
    {
        var station = new GameObject("FishingStation");
        station.transform.SetParent(parent);
        station.transform.position = position;
        CreatePrimitive("Stool", PrimitiveType.Cylinder, Vector3.zero,
            new Vector3(1.1f, 0.45f, 1.1f), wood, station.transform);
        var rod = CreatePrimitive("FishingRod", PrimitiveType.Cylinder, new Vector3(0.75f, 2.0f, 0),
            new Vector3(0.08f, 2.1f, 0.08f), wood, station.transform);
        rod.transform.localRotation = Quaternion.Euler(0, 0, -18);
        var line = CreatePrimitive("FishingLine", PrimitiveType.Cylinder, new Vector3(1.35f, 0.52f, 0),
            new Vector3(0.025f, 1.8f, 0.025f), whiteMaterial(), station.transform);
        line.transform.localRotation = Quaternion.Euler(0, 0, -8);
        CreatePrimitive("Float", PrimitiveType.Sphere, new Vector3(1.62f, 0.30f, 0),
            new Vector3(0.18f, 0.28f, 0.18f), floatMaterial, station.transform);
        CreatePrimitive("BaitBucket", PrimitiveType.Cylinder, new Vector3(-0.85f, 0.55f, 0.35f),
            new Vector3(0.65f, 0.55f, 0.65f), wood, station.transform);
    }

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
        // Lower the imported model so the soles meet the generated ground plane.
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
            throw new System.InvalidOperationException($"Avatar Humanoid tidak ditemukan pada model: {modelPath}. Reimport model dengan Rig=Humanoid terlebih dahulu.");
        animator.avatar = modelAvatar;
        animator.runtimeAnimatorController = fishingAnimatorController;

        var lineMaterial = MakeMaterial("FishingLine", new Color(0.92f, 0.92f, 0.85f));

        CreateAnchor("ArmLeft", new Vector3(-0.53f, 1.35f, 0), character.transform);
        CreateAnchor("ArmRight", new Vector3(0.53f, 1.35f, 0), character.transform);
        var rodMaterial = MakeMaterial("FishingRod", new Color(0.32f, 0.10f, 0.025f));
        var reelMaterial = MakeMaterial("FishingReel", new Color(0.08f, 0.08f, 0.09f));
        var rod = new GameObject("HeldFishingRod");
        rod.transform.SetParent(character.transform, false);
        rod.transform.localPosition = new Vector3(0.38f, 1.02f, 0.34f);
        rod.transform.localRotation = Quaternion.Euler(25f, 0f, -18f);
        var shaft = CreatePrimitive("RodShaft", PrimitiveType.Cylinder, new Vector3(0f, 0.82f, 0f),
            new Vector3(0.035f, 0.82f, 0.035f), rodMaterial, rod.transform);
        shaft.transform.localRotation = Quaternion.identity;
        CreatePrimitive("RodHandle", PrimitiveType.Cylinder, new Vector3(0f, 0.18f, 0f),
            new Vector3(0.075f, 0.22f, 0.075f), rodMaterial, rod.transform);
        var rightGrip = new GameObject("RodRightGrip");
        rightGrip.transform.SetParent(rod.transform, false);
        rightGrip.transform.localPosition = new Vector3(0f, 0.27f, 0f);
        var leftGrip = new GameObject("RodLeftGrip");
        leftGrip.transform.SetParent(rod.transform, false);
        leftGrip.transform.localPosition = new Vector3(0f, 0.50f, 0f);
        var reel = CreatePrimitive("RodReel", PrimitiveType.Cylinder, new Vector3(0f, 0.38f, 0f),
            new Vector3(0.13f, 0.055f, 0.13f), reelMaterial, rod.transform);
        reel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var tip = new GameObject("Pole_Tip");
        tip.transform.SetParent(rod.transform, false);
        tip.transform.localPosition = new Vector3(0f, 1.64f, 0f);
        var line = CreatePrimitive("HeldFishingLine", PrimitiveType.Cylinder, new Vector3(0.98f, 0.48f, 0.63f),
            new Vector3(0.012f, 0.95f, 0.012f), lineMaterial, character.transform);
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

    private static Avatar FindAvatar(string modelPath)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
        for (var i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Avatar avatar && avatar.isValid && avatar.isHuman)
                return avatar;
        }

        return null;
    }

    private static GameObject CreateAnchor(string name, Vector3 position, Transform parent)
    {
        var anchor = new GameObject(name);
        anchor.transform.SetParent(parent);
        anchor.transform.localPosition = position;
        anchor.transform.localRotation = Quaternion.identity;
        return anchor;
    }

    private static Material whiteMaterial()
    {
        return MakeMaterial("FishingLine", new Color(0.92f, 0.92f, 0.85f));
    }

    private static void CreateRock(Vector3 position, Material material, Transform parent)
    {
        var rock = CreatePrimitive("Rock", PrimitiveType.Sphere, position,
            new Vector3(1.5f, 0.7f, 1.1f), material, parent);
        rock.transform.rotation = Quaternion.Euler(0, position.x * 8, position.z * 5);
    }

    private static void CreatePath(Vector3 position, float width, float length, Material material, Transform parent)
    {
        CreatePrimitive("EntryPath", PrimitiveType.Cube, position, new Vector3(width, 0.08f, length), material, parent);
    }

    private static GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position,
        Vector3 scale, Material material, Transform parent)
    {
        var objectInstance = GameObject.CreatePrimitive(type);
        objectInstance.name = name;
        objectInstance.transform.SetParent(parent);
        objectInstance.transform.localPosition = position;
        objectInstance.transform.localScale = scale;
        objectInstance.GetComponent<Renderer>().sharedMaterial = material;
        return objectInstance;
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
