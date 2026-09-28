#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PrologueQuestSceneSetup
{
    private const string GameScenePath = "Assets/Scenes/Jogo.unity";
    private const string PlaceholderSpritePath = "Assets/Art/Placeholders/QuadradoBranco.png";

    [MenuItem("Prototipo/Adicionar primeira missao ao Jogo")]
    public static void AddFirstQuestToGameScene()
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
                EditorUtility.DisplayDialog("Cena Jogo nao encontrada", "Crie primeiro as cenas com Prototipo > Criar cenas iniciais.", "OK");
                return;
            }

            gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Additive);
        }

        try
        {
            if (FindInScene(gameScene, "Prologue Quest Systems") != null)
            {
                EditorUtility.DisplayDialog("Missao ja adicionada", "A primeira missao ja existe na cena Jogo.", "OK");
                return;
            }

            Sprite placeholderSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
            GameObject player = FindInScene(gameScene, "Player");
            if (player == null)
            {
                EditorUtility.DisplayDialog("Jogador nao encontrado", "A cena Jogo precisa ter um objeto chamado Player.", "OK");
                return;
            }

            if (placeholderSprite == null)
            {
                EditorUtility.DisplayDialog("Sprite temporario nao encontrado", "Crie primeiro as cenas iniciais para gerar o sprite de placeholder.", "OK");
                return;
            }

            SceneManager.SetActiveScene(gameScene);

            GameObject systems = new GameObject("Prologue Quest Systems");
            DialogueManager2D dialogue = FindDialogueManagerInScene(gameScene);
            if (dialogue == null)
            {
                dialogue = systems.AddComponent<DialogueManager2D>();
            }
            PrologueQuest quest = systems.AddComponent<PrologueQuest>();
            QuestObjectiveHUD objectiveHud = systems.AddComponent<QuestObjectiveHUD>();
            SetReference(objectiveHud, "quest", quest);

            PlayerInteractor2D interactor = player.GetComponent<PlayerInteractor2D>();
            if (interactor == null)
            {
                interactor = player.AddComponent<PlayerInteractor2D>();
            }
            SetReference(interactor, "dialogueManager", dialogue);

            GameObject npc = CreateInteractable("NPC - Dono do ferro-velho", new Vector2(-3f, 0f), new Color(0.75f, 0.48f, 0.24f), placeholderSprite, 0.9f);
            ScrapyardQuestNPC2D npcQuest = npc.AddComponent<ScrapyardQuestNPC2D>();
            npcQuest.PromptText = "Falar com o dono";
            SetReference(npcQuest, "dialogueManager", dialogue);
            SetReference(npcQuest, "quest", quest);

            GameObject scrapOne = CreateInteractable("Peca de metal 1", new Vector2(3f, 1.25f), new Color(0.68f, 0.72f, 0.76f), placeholderSprite, 0.55f);
            ConfigureScrap(scrapOne, dialogue, quest);
            GameObject scrapTwo = CreateInteractable("Peca de metal 2", new Vector2(5f, -1.25f), new Color(0.52f, 0.58f, 0.64f), placeholderSprite, 0.55f);
            ConfigureScrap(scrapTwo, dialogue, quest);

            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene);
            AssetDatabase.SaveAssets();
            Debug.Log("Primeira missao adicionada a cena Jogo. Aproxime-se dos objetos e aperte E para interagir.");
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

    private static DialogueManager2D FindDialogueManagerInScene(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            DialogueManager2D dialogue = root.GetComponentInChildren<DialogueManager2D>(true);
            if (dialogue != null)
            {
                return dialogue;
            }
        }

        return null;
    }

    private static GameObject CreateInteractable(string objectName, Vector2 position, Color color, Sprite sprite, float size)
    {
        GameObject interactable = new GameObject(objectName);
        interactable.transform.position = position;
        interactable.transform.localScale = new Vector3(size, size, 1f);

        SpriteRenderer renderer = interactable.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = 5;

        BoxCollider2D collider = interactable.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        return interactable;
    }

    private static void ConfigureScrap(GameObject scrap, DialogueManager2D dialogue, PrologueQuest quest)
    {
        ScrapPickup2D pickup = scrap.AddComponent<ScrapPickup2D>();
        pickup.PromptText = "Pegar metal";
        SetReference(pickup, "dialogueManager", dialogue);
        SetReference(pickup, "quest", quest);
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
