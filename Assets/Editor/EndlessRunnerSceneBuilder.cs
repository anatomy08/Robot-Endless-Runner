using EndlessRunner;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class EndlessRunnerSceneBuilder
{
    private const string RootName = "Endless Runner Root";
    private const string MaterialsFolder = "Assets/Materials/EndlessRunner";
    private const string PlayerRobotPrefabPath = "Assets/Prefabs/PlayerRobot.prefab";
    private const string TrackMaterialPath = "Assets/Materials/Material_GrassFlowers.mat";
    private const string LandingAudioPath = "Assets/SourceFiles/TimmyRobot/Sfx/Player_Land.wav";
    private const string FootstepAudioSearchFolder = "Assets/SourceFiles/TimmyRobot/Sfx";
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

        if (GameObject.Find(RootName) != null)
        {
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
        Material obstacleMaterial = GetOrCreateMaterial("Runner_Obstacle.mat", new Color(1f, 0.25f, 0.2f));

        GameObject root = new(RootName);

        RunnerGameManager gameManager = CreateGameManager(root.transform);
        Transform player = CreatePlayer(root.transform, gameManager, playerMaterial);
        CreateTrack(root.transform, gameManager, trackMaterial);
        CreateObstaclePool(root.transform, gameManager, obstacleMaterial);
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

    private static void CreateTrack(Transform parent, RunnerGameManager gameManager, Material material)
    {
        GameObject trackRoot = new("Track");
        trackRoot.transform.SetParent(parent);

        RunnerTrackManager trackManager = trackRoot.AddComponent<RunnerTrackManager>();
        trackManager.Configure(gameManager, trackRoot.transform, material);
    }

    private static void CreateObstaclePool(Transform parent, RunnerGameManager gameManager, Material material)
    {
        GameObject obstaclePool = new("Obstacle Pool");
        obstaclePool.transform.SetParent(parent);

        RunnerObstaclePool pool = obstaclePool.AddComponent<RunnerObstaclePool>();
        pool.Configure(gameManager, material);
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

        AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        Text scoreText = CreateText(canvasObject.transform, "Score Text", new Vector2(24f, -24f), TextAnchor.UpperLeft, 28);
        Text messageText = CreateText(canvasObject.transform, "Game Over Text", Vector2.zero, TextAnchor.MiddleCenter, 40);
        messageText.rectTransform.anchorMin = Vector2.zero;
        messageText.rectTransform.anchorMax = Vector2.one;
        messageText.rectTransform.offsetMin = Vector2.zero;
        messageText.rectTransform.offsetMax = Vector2.zero;

        RunnerHud hud = canvasObject.AddComponent<RunnerHud>();
        hud.Configure(gameManager, scoreText, messageText);
    }

    private static Text CreateText(Transform parent, string name, Vector2 anchoredPosition, TextAnchor alignment, int fontSize)
    {
        GameObject textObject = new(name);
        textObject.transform.SetParent(parent);

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;

        RectTransform rectTransform = text.rectTransform;
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(420f, 120f);

        return text;
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
