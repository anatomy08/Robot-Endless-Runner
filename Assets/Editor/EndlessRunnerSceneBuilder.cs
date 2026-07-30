using EndlessRunner;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class EndlessRunnerSceneBuilder
{
    private const string RootName = "Endless Runner Root";
    private const string MaterialsFolder = "Assets/Materials/EndlessRunner";
    private const string PlayerRobotPrefabPath = "Assets/Prefabs/PlayerRobot.prefab";
    private const string TrackMaterialPath = "Assets/Materials/Material_GrassFlowers.mat";
    private const string LandingAudioPath = "Assets/SourceFiles/TimmyRobot/Sfx/Player_Land.wav";
    private const string FootstepAudioSearchFolder = "Assets/SourceFiles/TimmyRobot/Sfx";
    private const string CollectAudioPath = "Assets/SFX_PositiveSound01.ogg";
    private const string EnvironmentSegmentPrefabPath = "Assets/Prefabs/RunnerEnvironmentSegment.prefab";
    private const float DefaultSegmentLength = 12f;
    private const float DefaultTrackWidth = 7f;
    private const float DefaultPlatformWidth = 72f;
    private static readonly Vector3 RobotVisualLocalPosition = new(0f, -1f, 0f);

    [InitializeOnLoadMethod]
    private static void BuildOnImport()
    {
        EditorApplication.delayCall += BuildIfMissing;
    }

    [MenuItem("Tools/Endless Runner/Rebuild MVP Scene")]
    public static void RebuildScene()
    {
        GameObject existingRoot = GameObject.Find(RootName);
        if (existingRoot != null)
        {
            Object.DestroyImmediate(existingRoot);
        }

        BuildScene();
    }

    private static void BuildIfMissing()
    {
        EditorApplication.delayCall -= BuildIfMissing;

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        if (GameObject.Find(RootName) != null)
        {
            UpgradeExistingScene();
            return;
        }

        BuildScene();
    }

    private static void BuildScene()
    {
        EnsureFolder("Assets/Materials");
        EnsureFolder(MaterialsFolder);

        Material playerMaterial = GetOrCreateMaterial("Runner_Player.mat", new Color(0.2f, 0.65f, 1f));
        Material trackMaterial = GetTrackMaterial();
        Material platformMaterial = GetOrCreateMaterial("Runner_Platform.mat", new Color(0.12f, 0.14f, 0.15f));
        Material obstacleMaterial = GetOrCreateMaterial("Runner_Obstacle.mat", new Color(0.42f, 0.38f, 0.34f));
        Material starMaterial = GetOrCreateMaterial("Runner_Star.mat", new Color(1f, 0.82f, 0.12f));
        Material[] buildingMaterials = GetBuildingMaterials();
        Material windowMaterial = GetOrCreateMaterial("Runner_Building_Windows.mat", new Color(0.95f, 0.78f, 0.28f));

        GameObject root = new(RootName);

        RunnerGameManager gameManager = CreateGameManager(root.transform);
        Transform player = CreatePlayer(root.transform, gameManager, playerMaterial);
        GameObject environmentSegmentPrefab = GetOrCreateEnvironmentSegmentPrefab(trackMaterial, platformMaterial);
        CreateTrack(root.transform, gameManager, player, trackMaterial, platformMaterial, environmentSegmentPrefab);
        CreateBuildingScenery(root.transform, gameManager, buildingMaterials, windowMaterial);
        CreateObstaclePool(root.transform, gameManager, obstacleMaterial);
        CreateCollectiblePool(root.transform, gameManager, starMaterial);
        ConfigureCamera(player);
        ConfigureLighting();
        CreateHud(root.transform, gameManager);

        Selection.activeGameObject = player.gameObject;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    private static RunnerGameManager CreateGameManager(Transform parent)
    {
        GameObject gameManagerObject = new("Runner Game Manager");
        gameManagerObject.transform.SetParent(parent);
        return gameManagerObject.AddComponent<RunnerGameManager>();
    }

    private static Transform CreatePlayer(Transform parent, RunnerGameManager gameManager, Material material)
    {
        GameObject player = new("Runner Player");
        player.transform.SetParent(parent);
        player.transform.position = new Vector3(0f, 1f, 0f);

        CharacterController characterController = player.AddComponent<CharacterController>();
        characterController.center = Vector3.zero;
        characterController.height = 2f;
        characterController.radius = 0.45f;

        RunnerPlayerController playerController = player.AddComponent<RunnerPlayerController>();
        Animator robotAnimator = CreateRobotVisual(player.transform, material);
        playerController.Configure(gameManager, robotAnimator);

        return player.transform;
    }

    private static Animator CreateRobotVisual(Transform parent, Material fallbackMaterial)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerRobotPrefabPath);
        if (prefab == null)
        {
            return CreateFallbackPlayerVisual(parent, fallbackMaterial);
        }

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        visual.name = "Robot Visual";
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = RobotVisualLocalPosition;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        StripPrefabGameplayComponents(visual);

        Animator animator = visual.GetComponentInChildren<Animator>();
        ConfigureAnimationEvents(animator);
        return animator;
    }

    private static Animator CreateFallbackPlayerVisual(Transform parent, Material material)
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Placeholder Visual";
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = Vector3.zero;

        Object.DestroyImmediate(visual.GetComponent<CapsuleCollider>());

        if (visual.TryGetComponent(out Renderer renderer))
        {
            renderer.sharedMaterial = material;
        }

        return null;
    }

    private static Material GetTrackMaterial()
    {
        Material importedMaterial = AssetDatabase.LoadAssetAtPath<Material>(TrackMaterialPath);
        if (importedMaterial != null)
        {
            return importedMaterial;
        }

        return GetOrCreateMaterial("Runner_Track.mat", new Color(0.18f, 0.2f, 0.22f));
    }

    private static GameObject GetOrCreateEnvironmentSegmentPrefab(Material trackMaterial, Material platformMaterial)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnvironmentSegmentPrefabPath);
        if (prefab != null)
        {
            return prefab;
        }

        EnsureFolder("Assets/Prefabs");

        GameObject root = new("Runner Environment Segment");
        try
        {
            CreatePrefabPart(
                root.transform,
                "Track",
                new Vector3(0f, 0f, DefaultSegmentLength * 0.5f),
                new Vector3(DefaultTrackWidth, 0.24f, DefaultSegmentLength),
                trackMaterial);

            float sideWidth = (DefaultPlatformWidth - DefaultTrackWidth) * 0.5f;
            float sideOffset = DefaultTrackWidth * 0.5f + sideWidth * 0.5f;
            CreatePrefabPart(
                root.transform,
                "Left Platform",
                new Vector3(-sideOffset, -0.18f, DefaultSegmentLength * 0.5f),
                new Vector3(sideWidth, 0.18f, DefaultSegmentLength),
                platformMaterial);
            CreatePrefabPart(
                root.transform,
                "Right Platform",
                new Vector3(sideOffset, -0.18f, DefaultSegmentLength * 0.5f),
                new Vector3(sideWidth, 0.18f, DefaultSegmentLength),
                platformMaterial);

            CreatePrefabMarker(root.transform, "Segment Start", 0f);
            CreatePrefabMarker(root.transform, "Segment End", DefaultSegmentLength);
            return PrefabUtility.SaveAsPrefabAsset(root, EnvironmentSegmentPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void CreatePrefabPart(
        Transform parent,
        string objectName,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = objectName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;

        if (material != null && part.TryGetComponent(out Renderer renderer))
        {
            renderer.sharedMaterial = material;
        }
    }

    private static void CreatePrefabMarker(Transform parent, string markerName, float localZ)
    {
        GameObject marker = new(markerName);
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = new Vector3(0f, 0f, localZ);
    }

    private static void StripPrefabGameplayComponents(GameObject visualRoot)
    {
        foreach (MonoBehaviour behaviour in visualRoot.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour != null)
            {
                Object.DestroyImmediate(behaviour);
            }
        }

        foreach (Collider collider in visualRoot.GetComponentsInChildren<Collider>(true))
        {
            Object.DestroyImmediate(collider);
        }

        foreach (CharacterController controller in visualRoot.GetComponentsInChildren<CharacterController>(true))
        {
            Object.DestroyImmediate(controller);
        }

        foreach (Camera camera in visualRoot.GetComponentsInChildren<Camera>(true))
        {
            Object.DestroyImmediate(camera);
        }

        foreach (AudioListener listener in visualRoot.GetComponentsInChildren<AudioListener>(true))
        {
            Object.DestroyImmediate(listener);
        }
    }

    private static void ConfigureAnimationEvents(Animator animator)
    {
        if (animator == null)
        {
            return;
        }

        RunnerAnimationEventReceiver receiver = animator.GetComponent<RunnerAnimationEventReceiver>();
        if (receiver == null)
        {
            receiver = animator.gameObject.AddComponent<RunnerAnimationEventReceiver>();
        }

        receiver.Configure(
            AssetDatabase.LoadAssetAtPath<AudioClip>(LandingAudioPath),
            LoadFootstepAudioClips(),
            0.65f,
            0.9f);
    }

    private static AudioClip[] LoadFootstepAudioClips()
    {
        string[] guids = AssetDatabase.FindAssets("Player_Footstep_ t:AudioClip", new[] { FootstepAudioSearchFolder });
        System.Array.Sort(guids);
        AudioClip[] clips = new AudioClip[guids.Length];

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        return clips;
    }

    private static void CreateTrack(
        Transform parent,
        RunnerGameManager gameManager,
        Transform player,
        Material material,
        Material platformMaterial,
        GameObject environmentSegmentPrefab)
    {
        GameObject trackRoot = new("Track");
        trackRoot.transform.SetParent(parent);

        RunnerTrackManager trackManager = trackRoot.AddComponent<RunnerTrackManager>();
        trackManager.Configure(
            gameManager,
            trackRoot.transform,
            material,
            platformMaterial,
            DefaultPlatformWidth,
            player,
            new[] { environmentSegmentPrefab });
    }

    private static void CreateBuildingScenery(Transform parent, RunnerGameManager gameManager, Material[] materials, Material windowMaterial)
    {
        GameObject scenery = new("Building Scenery");
        scenery.transform.SetParent(parent);

        RunnerBuildingScenery buildingScenery = scenery.AddComponent<RunnerBuildingScenery>();
        buildingScenery.Configure(gameManager, materials, windowMaterial);
    }

    private static void CreateObstaclePool(Transform parent, RunnerGameManager gameManager, Material material)
    {
        GameObject obstaclePool = new("Obstacle Pool");
        obstaclePool.transform.SetParent(parent);

        RunnerObstaclePool pool = obstaclePool.AddComponent<RunnerObstaclePool>();
        pool.Configure(gameManager, material);
    }

    private static void CreateCollectiblePool(Transform parent, RunnerGameManager gameManager, Material material)
    {
        GameObject collectiblePool = new("Collectible Pool");
        collectiblePool.transform.SetParent(parent);

        RunnerCollectiblePool pool = collectiblePool.AddComponent<RunnerCollectiblePool>();
        pool.Configure(gameManager, material, AssetDatabase.LoadAssetAtPath<AudioClip>(CollectAudioPath));
    }

    private static void ConfigureCamera(Transform player)
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
        }

        camera.transform.position = new Vector3(0f, 5f, -8f);
        camera.transform.LookAt(player.position + Vector3.up);
        EnsureSingleAudioListener(camera);

        RunnerCameraFollow follow = camera.GetComponent<RunnerCameraFollow>();
        if (follow == null)
        {
            follow = camera.gameObject.AddComponent<RunnerCameraFollow>();
        }

        follow.Configure(player);
    }

    private static void EnsureSingleAudioListener(Camera mainCamera)
    {
        AudioListener cameraListener = mainCamera.GetComponent<AudioListener>();
        if (cameraListener == null)
        {
            cameraListener = mainCamera.gameObject.AddComponent<AudioListener>();
        }

        AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
        foreach (AudioListener listener in listeners)
        {
            if (listener != cameraListener)
            {
                Object.DestroyImmediate(listener);
            }
        }
    }

    private static void ConfigureLighting()
    {
        Light light = Object.FindAnyObjectByType<Light>();
        if (light == null)
        {
            GameObject lightObject = new("Directional Light");
            light = lightObject.AddComponent<Light>();
        }

        light.type = LightType.Directional;
        light.intensity = 2f;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void CreateHud(Transform parent, RunnerGameManager gameManager)
    {
        GameObject canvasObject = new("Runner HUD");
        canvasObject.transform.SetParent(parent);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        Text scoreText = CreateText(canvasObject.transform, "Score Text", new Vector2(24f, -24f), TextAnchor.UpperLeft, 30);
        Text starText = CreateText(canvasObject.transform, "Star Text", new Vector2(-24f, -24f), TextAnchor.UpperRight, 30);
        RectTransform starRect = starText.rectTransform;
        starRect.anchorMin = new Vector2(1f, 1f);
        starRect.anchorMax = new Vector2(1f, 1f);
        starRect.pivot = new Vector2(1f, 1f);

        Text messageText = CreateText(canvasObject.transform, "Game Over Text", Vector2.zero, TextAnchor.MiddleCenter, 40);
        messageText.rectTransform.anchorMin = Vector2.zero;
        messageText.rectTransform.anchorMax = Vector2.one;
        messageText.rectTransform.offsetMin = Vector2.zero;
        messageText.rectTransform.offsetMax = Vector2.zero;

        RunnerHud hud = canvasObject.AddComponent<RunnerHud>();
        hud.Configure(gameManager, scoreText, starText, messageText);

        CreateOrUpgradeMenus(canvasObject.transform, gameManager);
        EnsureEventSystem();
    }

    private static void CreateOrUpgradeMenus(Transform canvasTransform, RunnerGameManager gameManager)
    {
        GameObject startPanel = FindOrCreatePanel(
            canvasTransform,
            "Start Menu",
            new Color(0.035f, 0.045f, 0.055f, 0.94f));
        Text title = FindOrCreateCenteredText(startPanel.transform, "Title", "ROBOT RUNNER", 64, new Vector2(0f, 100f));
        title.color = new Color(0.3f, 0.9f, 0.55f);
        Button startButton = FindOrCreateButton(startPanel.transform, "Start Button", "START", new Vector2(0f, -40f), new Vector2(260f, 72f));

        Button pauseButton = FindOrCreateButton(canvasTransform, "Pause Button", "II", new Vector2(0f, -24f), new Vector2(64f, 56f));
        RectTransform pauseButtonRect = pauseButton.GetComponent<RectTransform>();
        pauseButtonRect.anchorMin = new Vector2(0.5f, 1f);
        pauseButtonRect.anchorMax = new Vector2(0.5f, 1f);
        pauseButtonRect.pivot = new Vector2(0.5f, 1f);

        GameObject pausePanel = FindOrCreatePanel(
            canvasTransform,
            "Pause Menu",
            new Color(0.035f, 0.045f, 0.055f, 0.86f));
        FindOrCreateCenteredText(pausePanel.transform, "Pause Title", "PAUSED", 56, new Vector2(0f, 80f));
        Button resumeButton = FindOrCreateButton(pausePanel.transform, "Resume Button", "RESUME", new Vector2(0f, -35f), new Vector2(260f, 72f));

        RunnerMenuController menuController = canvasTransform.GetComponent<RunnerMenuController>();
        if (menuController == null)
        {
            menuController = canvasTransform.gameObject.AddComponent<RunnerMenuController>();
        }

        menuController.Configure(gameManager, startPanel, startButton, pauseButton, pausePanel, resumeButton);
    }

    private static GameObject FindOrCreatePanel(Transform parent, string objectName, Color color)
    {
        Transform existing = parent.Find(objectName);
        GameObject panel = existing != null ? existing.gameObject : new GameObject(objectName);
        panel.transform.SetParent(parent, false);

        Image image = panel.GetComponent<Image>();
        if (image == null)
        {
            image = panel.AddComponent<Image>();
        }

        image.color = color;
        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        return panel;
    }

    private static Text FindOrCreateCenteredText(
        Transform parent,
        string objectName,
        string content,
        int fontSize,
        Vector2 anchoredPosition)
    {
        Text text = FindChildText(parent, objectName);
        if (text == null)
        {
            text = CreateText(parent, objectName, anchoredPosition, TextAnchor.MiddleCenter, fontSize);
        }

        text.text = content;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        RectTransform rectTransform = text.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(720f, 110f);
        return text;
    }

    private static Button FindOrCreateButton(
        Transform parent,
        string objectName,
        string label,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        Transform existing = parent.Find(objectName);
        GameObject buttonObject = existing != null ? existing.gameObject : new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        if (image == null)
        {
            image = buttonObject.AddComponent<Image>();
        }

        image.color = new Color(0.16f, 0.68f, 0.38f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            button = buttonObject.AddComponent<Button>();
        }

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.88f, 1f, 0.92f);
        colors.pressedColor = new Color(0.72f, 0.86f, 0.76f);
        button.colors = colors;

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Text buttonText = FindChildText(buttonObject.transform, "Label");
        if (buttonText == null)
        {
            buttonText = CreateText(buttonObject.transform, "Label", Vector2.zero, TextAnchor.MiddleCenter, 28);
        }

        buttonText.text = label;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        RectTransform textRect = buttonText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return button;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    private static Text CreateText(Transform parent, string name, Vector2 anchoredPosition, TextAnchor alignment, int fontSize)
    {
        GameObject textObject = new(name);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;

        Shadow shadow = textObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
        shadow.effectDistance = new Vector2(2f, -2f);

        RectTransform rectTransform = text.rectTransform;
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(420f, 120f);

        return text;
    }

    private static void UpgradeExistingScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        GameObject root = GameObject.Find(RootName);
        RunnerGameManager gameManager = Object.FindAnyObjectByType<RunnerGameManager>();
        if (root == null || gameManager == null)
        {
            return;
        }

        EnsureFolder("Assets/Materials");
        EnsureFolder(MaterialsFolder);

        Material trackMaterial = GetTrackMaterial();
        Material platformMaterial = GetOrCreateMaterial("Runner_Platform.mat", new Color(0.12f, 0.14f, 0.15f));
        GameObject environmentSegmentPrefab = GetOrCreateEnvironmentSegmentPrefab(trackMaterial, platformMaterial);
        RunnerPlayerController playerController = Object.FindAnyObjectByType<RunnerPlayerController>();
        RunnerTrackManager trackManager = Object.FindAnyObjectByType<RunnerTrackManager>();
        if (trackManager != null)
        {
            trackManager.Configure(
                gameManager,
                trackManager.transform,
                trackMaterial,
                platformMaterial,
                DefaultPlatformWidth,
                playerController != null ? playerController.transform : null,
                new[] { environmentSegmentPrefab });
        }

        Material obstacleMaterial = GetOrCreateMaterial("Runner_Obstacle.mat", new Color(0.42f, 0.38f, 0.34f));
        RunnerObstaclePool obstaclePool = Object.FindAnyObjectByType<RunnerObstaclePool>();
        if (obstaclePool != null)
        {
            obstaclePool.Configure(gameManager, obstacleMaterial);
        }

        GameObject collectiblePool = GameObject.Find("Collectible Pool");
        Material starMaterial = GetOrCreateMaterial("Runner_Star.mat", new Color(1f, 0.82f, 0.12f));
        if (collectiblePool == null)
        {
            CreateCollectiblePool(root.transform, gameManager, starMaterial);
        }
        else if (collectiblePool.TryGetComponent(out RunnerCollectiblePool pool))
        {
            pool.Configure(gameManager, starMaterial, AssetDatabase.LoadAssetAtPath<AudioClip>(CollectAudioPath));
        }

        GameObject buildingScenery = GameObject.Find("Building Scenery");
        Material[] buildingMaterials = GetBuildingMaterials();
        Material windowMaterial = GetOrCreateMaterial("Runner_Building_Windows.mat", new Color(0.95f, 0.78f, 0.28f));
        if (buildingScenery == null)
        {
            CreateBuildingScenery(root.transform, gameManager, buildingMaterials, windowMaterial);
        }
        else if (buildingScenery.TryGetComponent(out RunnerBuildingScenery scenery))
        {
            scenery.Configure(gameManager, buildingMaterials, windowMaterial);
        }

        UpgradeHud(gameManager);
        EnsureEventSystem();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    private static void UpgradeHud(RunnerGameManager gameManager)
    {
        RunnerHud hud = Object.FindAnyObjectByType<RunnerHud>();
        if (hud == null)
        {
            return;
        }

        Transform hudTransform = hud.transform;
        Text scoreText = FindChildText(hudTransform, "Score Text");
        Text starText = FindChildText(hudTransform, "Star Text");
        Text messageText = FindChildText(hudTransform, "Game Over Text");

        if (starText == null)
        {
            starText = CreateText(hudTransform, "Star Text", new Vector2(-24f, -24f), TextAnchor.UpperRight, 30);
            RectTransform starRect = starText.rectTransform;
            starRect.anchorMin = new Vector2(1f, 1f);
            starRect.anchorMax = new Vector2(1f, 1f);
            starRect.pivot = new Vector2(1f, 1f);
        }

        StyleHudText(scoreText, TextAnchor.UpperLeft, 30);
        StyleHudText(starText, TextAnchor.UpperRight, 30);
        StyleHudText(messageText, TextAnchor.MiddleCenter, 42);
        hud.Configure(gameManager, scoreText, starText, messageText);
        CreateOrUpgradeMenus(hudTransform, gameManager);
    }

    private static Text FindChildText(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        return child != null ? child.GetComponent<Text>() : null;
    }

    private static void StyleHudText(Text text, TextAnchor alignment, int fontSize)
    {
        if (text == null)
        {
            return;
        }

        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;

        Shadow shadow = text.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = text.gameObject.AddComponent<Shadow>();
        }

        shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
        shadow.effectDistance = new Vector2(2f, -2f);
    }

    private static Material GetOrCreateMaterial(string fileName, Color color)
    {
        string path = $"{MaterialsFolder}/{fileName}";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = color;
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Material[] GetBuildingMaterials()
    {
        return new[]
        {
            GetOrCreateMaterial("Runner_Building_Cool.mat", new Color(0.35f, 0.43f, 0.5f)),
            GetOrCreateMaterial("Runner_Building_Glass.mat", new Color(0.18f, 0.48f, 0.62f)),
            GetOrCreateMaterial("Runner_Building_Warm.mat", new Color(0.58f, 0.5f, 0.4f)),
            GetOrCreateMaterial("Runner_Building_Dark.mat", new Color(0.22f, 0.24f, 0.27f))
        };
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
        string folderName = System.IO.Path.GetFileName(folderPath);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}
