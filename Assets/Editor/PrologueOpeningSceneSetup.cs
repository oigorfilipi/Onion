#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PrologueOpeningSceneSetup
{
    private const string GameScenePath = "Assets/Scenes/Jogo.unity";
    private const string PlaceholderSpritePath = "Assets/Art/Placeholders/QuadradoBranco.png";

    [MenuItem("Prototipo/Adicionar abertura em casa ao Jogo")]
    public static void AddOpeningAtHome()
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
            if (FindInScene(gameScene, "Madrasta - cozinha") != null)
            {
                EditorUtility.DisplayDialog("Abertura ja adicionada", "A madrasta ja esta na cena Jogo.", "OK");
                return;
            }

            GameObject player = FindInScene(gameScene, "Player");
            Sprite placeholderSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
            if (player == null || placeholderSprite == null)
            {
                EditorUtility.DisplayDialog("Cena incompleta", "A cena Jogo precisa conter o Player e o sprite QuadradoBranco.png.", "OK");
                return;
            }

            SceneManager.SetActiveScene(gameScene);

            DialogueManager2D dialogue = FindComponentInScene<DialogueManager2D>(gameScene);
            if (dialogue == null)
            {
                GameObject systems = new GameObject("Prologue Opening Systems");
                dialogue = systems.AddComponent<DialogueManager2D>();
            }

            PlayerInteractor2D interactor = player.GetComponent<PlayerInteractor2D>();
            if (interactor == null)
            {
                interactor = player.AddComponent<PlayerInteractor2D>();
            }
            SetReference(interactor, "dialogueManager", dialogue);

            GameObject homeMarker = new GameObject("Ponto inicial - casa do protagonista");
            homeMarker.transform.position = player.transform.position;

            GameObject stepmother = new GameObject("Madrasta - cozinha");
            stepmother.transform.position = player.transform.position + new Vector3(1.2f, 0.55f, 0f);
            stepmother.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

            SpriteRenderer renderer = stepmother.AddComponent<SpriteRenderer>();
            renderer.sprite = placeholderSprite;
            renderer.color = new Color(0.73f, 0.49f, 0.4f);
            renderer.sortingOrder = 12;

            BoxCollider2D collider = stepmother.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            StepmotherNPC2D npc = stepmother.AddComponent<StepmotherNPC2D>();
            npc.PromptText = "Falar com a madrasta";
            SetReference(npc, "dialogueManager", dialogue);

            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene);
            AssetDatabase.SaveAssets();
            Debug.Log("Abertura provisoria em casa adicionada. A conversa com a madrasta e opcional.");
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
