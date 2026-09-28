#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class StarterSceneCreator
{
    private const string ScenesFolder = "Assets/Scenes";
    private const string ArtFolder = "Assets/Art/Placeholders";
    private const string MenuScenePath = ScenesFolder + "/Menu.unity";
    private const string GameScenePath = ScenesFolder + "/Jogo.unity";
    private const string PlaceholderSpritePath = ArtFolder + "/QuadradoBranco.png";

    [MenuItem("Prototipo/Criar cenas iniciais")]
    public static void CreateStarterScenes()
    {
        Directory.CreateDirectory(ScenesFolder);
        Directory.CreateDirectory(ArtFolder);
        CreatePlaceholderSprite();
        AssetDatabase.Refresh();

        Sprite squareSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
        if (squareSprite == null)
        {
            Debug.LogError("Nao consegui importar o sprite temporario.");
            return;
        }

        CreateMenuScene();
        CreateGameScene(squareSprite);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true)
        };

        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        Debug.Log("Cenas Menu e Jogo criadas. O sprite e o personagem sao placeholders.");
    }

    private static void CreateMenuScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera(new Color(0.08f, 0.1f, 0.16f));
        new GameObject("Menu Screen").AddComponent<MainMenuScreen>();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), MenuScenePath);
    }

    private static void CreateGameScene(Sprite squareSprite)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera mainCamera = CreateCamera(new Color(0.16f, 0.24f, 0.18f));
        CreateGlobalLight2D();

        GameObject ground = new GameObject("Chao temporario");
        SpriteRenderer groundRenderer = ground.AddComponent<SpriteRenderer>();
        groundRenderer.sprite = squareSprite;
        groundRenderer.color = new Color(0.24f, 0.38f, 0.25f);
        groundRenderer.sortingOrder = -10;
        ground.transform.localScale = new Vector3(24f, 16f, 1f);

        CreateWorldBounds();
        GameObject player = CreatePlayer(squareSprite);

        CameraFollow2D cameraFollow = mainCamera.gameObject.AddComponent<CameraFollow2D>();
        SerializedObject serializedCameraFollow = new SerializedObject(cameraFollow);
        serializedCameraFollow.FindProperty("target").objectReferenceValue = player.transform;
        serializedCameraFollow.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), GameScenePath);
    }

    private static Camera CreateCamera(Color background)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = background;
        cameraObject.AddComponent<AudioListener>();
        return camera;
    }

    private static void CreateGlobalLight2D()
    {
        GameObject lightObject = new GameObject("Luz global 2D");
        Light2D light = lightObject.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;
        light.color = Color.white;
    }

    private static GameObject CreatePlayer(Sprite squareSprite)
    {
        GameObject player = new GameObject("Player");
        SpriteRenderer spriteRenderer = player.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = squareSprite;
        spriteRenderer.color = new Color(0.95f, 0.76f, 0.25f);
        spriteRenderer.sortingOrder = 10;

        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D collider = player.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.7f, 0.7f);

        player.AddComponent<PlayerMovement2D>();
        player.AddComponent<PlayerVitals>();
        return player;
    }

    private static void CreateWorldBounds()
    {
        CreateWall("Limite norte", new Vector2(0f, 8.25f), new Vector2(24.5f, 0.5f));
        CreateWall("Limite sul", new Vector2(0f, -8.25f), new Vector2(24.5f, 0.5f));
        CreateWall("Limite leste", new Vector2(12.25f, 0f), new Vector2(0.5f, 16.5f));
        CreateWall("Limite oeste", new Vector2(-12.25f, 0f), new Vector2(0.5f, 16.5f));
    }

    private static void CreateWall(string objectName, Vector2 position, Vector2 size)
    {
        GameObject wall = new GameObject(objectName);
        wall.transform.position = position;
        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;
    }

    private static void CreatePlaceholderSprite()
    {
        if (!File.Exists(PlaceholderSpritePath))
        {
            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[16 * 16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(PlaceholderSpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        AssetDatabase.ImportAsset(PlaceholderSpritePath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(PlaceholderSpritePath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
#endif
