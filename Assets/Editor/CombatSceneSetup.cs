#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CombatSceneSetup
{
    private const string GameScenePath = "Assets/Scenes/Jogo.unity";
    private const string PlaceholderSpritePath = "Assets/Art/Placeholders/QuadradoBranco.png";

    [MenuItem("Prototipo/Adicionar combate de prototipo ao Jogo")]
    public static void AddCombatPrototype()
    {
        Scene originalActiveScene = SceneManager.GetActiveScene();
        Scene gameScene = SceneManager.GetSceneByPath(GameScenePath);
        bool wasAlreadyLoaded = gameScene.IsValid() && gameScene.isLoaded;

        if (!wasAlreadyLoaded)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GameScenePath) == null)
            {
                EditorUtility.DisplayDialog("Cena Jogo nao encontrada", "Crie primeiro as cenas iniciais com Prototipo > Criar cenas iniciais.", "OK");
                return;
            }

            gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Additive);
        }

        try
        {
            if (FindInScene(gameScene, "Inimigo de fogo - prototipo") != null)
            {
                EditorUtility.DisplayDialog("Combate ja adicionado", "O inimigo de teste ja existe na cena Jogo.", "OK");
                return;
            }

            GameObject player = FindInScene(gameScene, "Player");
            Sprite placeholderSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
            if (player == null || placeholderSprite == null)
            {
                EditorUtility.DisplayDialog("Cena incompleta", "A cena Jogo precisa conter o Player e o sprite QuadradoBranco.png. Crie as cenas iniciais novamente se algum deles estiver faltando.", "OK");
                return;
            }

            SceneManager.SetActiveScene(gameScene);

            if (player.GetComponent<PlayerCombat2D>() == null)
            {
                player.AddComponent<PlayerCombat2D>();
            }

            GameObject hud = new GameObject("Combat HUD");
            CombatHUD2D combatHud = hud.AddComponent<CombatHUD2D>();
            SetReference(combatHud, "playerVitals", player.GetComponent<PlayerVitals>());

            GameObject enemy = new GameObject("Inimigo de fogo - prototipo");
            enemy.transform.position = new Vector2(7f, 4f);
            enemy.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            SpriteRenderer renderer = enemy.AddComponent<SpriteRenderer>();
            renderer.sprite = placeholderSprite;
            renderer.color = new Color(0.8f, 0.2f, 0.14f);
            renderer.sortingOrder = 8;

            BoxCollider2D collider = enemy.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            enemy.AddComponent<EnemyHealth2D>();

            EnemyFireShooter2D shooter = enemy.AddComponent<EnemyFireShooter2D>();
            SetReference(shooter, "target", player.transform);
            SetReference(shooter, "projectileSprite", placeholderSprite);

            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene);
            AssetDatabase.SaveAssets();
            Debug.Log("Combate temporario adicionado. Ataque o inimigo com o mouse e observe vida e energia na HUD.");
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

    private static GameObject FindInScene(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == objectName)
            {
                return root;
            }

            Transform found = root.transform.Find(objectName);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static void SetReference(Object target, string propertyName, Object reference)
    {
        SerializedObject serializedTarget = new SerializedObject(target);
        SerializedProperty property = serializedTarget.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"Campo serializado '{propertyName}' nao encontrado em {target.GetType().Name}.", target);
            return;
        }

        property.objectReferenceValue = reference;
        serializedTarget.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
