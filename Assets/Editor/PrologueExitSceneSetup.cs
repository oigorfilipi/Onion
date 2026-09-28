#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PrologueExitSceneSetup
{
    private const string GameScenePath = "Assets/Scenes/Jogo.unity";
    private const string PlaceholderSpritePath = "Assets/Art/Placeholders/QuadradoBranco.png";

    [MenuItem("Prototipo/Adicionar saida continua da cidade ao Jogo")]
    public static void AddContinuousTownExit()
    {
        Scene originalActiveScene = SceneManager.GetActiveScene();
        Scene gameScene = SceneManager.GetSceneByPath(GameScenePath);
        bool wasAlreadyLoaded = gameScene.IsValid() && gameScene.isLoaded;

        if (!wasAlreadyLoaded)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GameScenePath) == null)
            {
                EditorUtility.DisplayDialog("Cena Jogo nao encontrada", "Crie primeiro as cenas iniciais com Prototipo > Criar cenas iniciais.", "OK");
                return;
            }

            gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Additive);
        }

        try
        {
            GameObject ground = FindInScene(gameScene, "Chao temporario");
            GameObject player = FindInScene(gameScene, "Player");
            GameObject ratObject = FindInScene(gameScene, "Rato mutante");
            PrologueStorySequence2D story = FindComponentInScene<PrologueStorySequence2D>(gameScene);
            Sprite placeholderSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);

            if (ground == null || player == null || ratObject == null || story == null || placeholderSprite == null)
            {
                EditorUtility.DisplayDialog(
                    "Componentes do prologo faltando",
                    "Antes, aplique a abertura, a missao, o inventario com o rato e a sequencia narrativa da colher.",
                    "OK");
                return;
            }

            SceneManager.SetActiveScene(gameScene);

            ground.transform.position = new Vector3(0f, 0f, ground.transform.position.z);
            ground.transform.localScale = new Vector3(24f, 16f, 1f);

            SpriteRenderer groundRenderer = ground.GetComponent<SpriteRenderer>();
            Color groundColor = groundRenderer != null ? groundRenderer.color : new Color(0.24f, 0.38f, 0.25f);
            GameObject groundExtension = FindInScene(gameScene, "Extensao de chao leste - placeholder");
            if (groundExtension == null) groundExtension = new GameObject("Extensao de chao leste - placeholder");
            groundExtension.transform.position = new Vector3(23f, 0f, ground.transform.position.z);
            groundExtension.transform.localScale = new Vector3(22.2f, 16f, 1f);
            SpriteRenderer extensionRenderer = GetOrAdd<SpriteRenderer>(groundExtension);
            extensionRenderer.sprite = placeholderSprite;
            extensionRenderer.color = groundColor;
            extensionRenderer.sortingOrder = groundRenderer != null ? groundRenderer.sortingOrder : -10;

            SetWall(gameScene, "Limite norte", new Vector2(11f, 8.25f), new Vector2(46.5f, 0.5f));
            SetWall(gameScene, "Limite sul", new Vector2(11f, -8.25f), new Vector2(46.5f, 0.5f));
            SetWall(gameScene, "Limite oeste", new Vector2(-12.25f, 0f), new Vector2(0.5f, 16.5f));

            GameObject oldEastWall = FindInScene(gameScene, "Limite leste");
            if (oldEastWall != null)
            {
                oldEastWall.SetActive(false);
            }

            SetWall(gameScene, "Limite leste - norte da passagem", new Vector2(12.25f, 2f), new Vector2(0.5f, 12.5f));
            SetWall(gameScene, "Limite leste - sul da passagem", new Vector2(12.25f, -7f), new Vector2(0.5f, 2.5f));
            SetWall(gameScene, "Limite leste - fim do trecho", new Vector2(34.25f, 0f), new Vector2(0.5f, 16.5f));

            GameObject road = FindInScene(gameScene, "Estrada leste - placeholder");
            if (road == null) road = new GameObject("Estrada leste - placeholder");
            road.transform.position = new Vector3(23f, -5f, 0f);
            road.transform.localScale = new Vector3(22f, 2.2f, 1f);
            SpriteRenderer roadRenderer = GetOrAdd<SpriteRenderer>(road);
            roadRenderer.sprite = placeholderSprite;
            roadRenderer.color = new Color(0.48f, 0.36f, 0.23f);
            roadRenderer.sortingOrder = -9;

            ratObject.transform.position = new Vector3(12.25f, -5f, 0f);
            story.ConfigureMutantRat(ratObject.GetComponent<EnemyHealth2D>());

            GameObject exitObject = FindInScene(gameScene, "Trigger - saida leste da cidade");
            if (exitObject == null) exitObject = new GameObject("Trigger - saida leste da cidade");
            exitObject.transform.position = new Vector3(15.5f, -5f, 0f);
            exitObject.transform.localScale = Vector3.one;
            BoxCollider2D exitCollider = GetOrAdd<BoxCollider2D>(exitObject);
            exitCollider.size = new Vector2(1.2f, 1.5f);
            exitCollider.isTrigger = true;
            PrologueTownExit2D exit = GetOrAdd<PrologueTownExit2D>(exitObject);
            exit.ConfigureStory(story);

            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene);
            AssetDatabase.SaveAssets();
            Debug.Log("A saida leste foi aberta e conectada a uma estrada provisoria na mesma cena. O rato bloqueia a passagem ate ser derrotado.");
        }
        finally
        {
            if (originalActiveScene.IsValid() && originalActiveScene.isLoaded)
            {
                SceneManager.SetActiveScene(originalActiveScene);
            }

            if (!wasAlreadyLoaded && gameScene.IsValid() && gameScene.isLoaded)
            {
                EditorSceneManager.CloseScene(gameScene, true);
            }
        }
    }

    private static void SetWall(Scene scene, string objectName, Vector2 position, Vector2 size)
    {
        GameObject wall = FindInScene(scene, objectName);
        if (wall == null) wall = new GameObject(objectName);
        wall.transform.position = position;
        BoxCollider2D collider = GetOrAdd<BoxCollider2D>(wall);
        collider.size = size;
        collider.isTrigger = false;
        wall.SetActive(true);
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static GameObject FindInScene(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == objectName) return root;
            Transform found = root.transform.Find(objectName);
            if (found != null) return found.gameObject;
        }

        return null;
    }

    private static T FindComponentInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null) return component;
        }

        return null;
    }
}
#endif
