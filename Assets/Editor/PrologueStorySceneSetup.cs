#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PrologueStorySceneSetup
{
    private const string GameScenePath = "Assets/Scenes/Jogo.unity";
    private const string PlaceholderSpritePath = "Assets/Art/Placeholders/QuadradoBranco.png";

    [MenuItem("Prototipo/Adicionar sequencia narrativa da colher ao Jogo")]
    public static void AddSpoonStorySequence()
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
            GameObject existingFigure = FindInScene(gameScene, "Personagem misterioso - sequencia da colher");
            if (existingFigure != null)
            {
                PrologueStorySequence2D existingStory = existingFigure.GetComponent<PrologueStorySequence2D>();
                GameObject existingRat = FindInScene(gameScene, "Rato mutante");
                if (existingStory != null && existingRat != null)
                {
                    existingStory.ConfigureMutantRat(existingRat.GetComponent<EnemyHealth2D>());
                    EditorSceneManager.MarkSceneDirty(gameScene);
                    EditorSceneManager.SaveScene(gameScene);
                    EditorUtility.DisplayDialog("Sequencia atualizada", "A sequencia ja existia; o encontro do rato foi ligado ao roteiro.", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Sequencia ja adicionada", "A sequencia narrativa da colher ja existe na cena Jogo.", "OK");
                }
                return;
            }

            GameObject player = FindInScene(gameScene, "Player");
            GameObject spoonObject = FindInScene(gameScene, "Poder de Contato - Colher brilhante");
            GameObject enemyObject = FindInScene(gameScene, "Inimigo de fogo - prototipo");
            GameObject questSystems = FindInScene(gameScene, "Prologue Quest Systems");
            Sprite placeholderSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);

            PowerPickup2D spoon = spoonObject != null ? spoonObject.GetComponent<PowerPickup2D>() : null;
            EnemyHealth2D enemy = enemyObject != null ? enemyObject.GetComponent<EnemyHealth2D>() : null;
            EnemyFireShooter2D shooter = enemyObject != null ? enemyObject.GetComponent<EnemyFireShooter2D>() : null;
            ScrapyardQuestNPC2D scrapyardNpc = FindInScene(gameScene, "NPC - Dono do ferro-velho")?.GetComponent<ScrapyardQuestNPC2D>();
            PrologueQuest quest = questSystems != null ? questSystems.GetComponent<PrologueQuest>() : null;
            DialogueManager2D dialogue = questSystems != null ? questSystems.GetComponent<DialogueManager2D>() : null;

            if (player == null || spoon == null || enemy == null || shooter == null || scrapyardNpc == null || quest == null || dialogue == null || placeholderSprite == null)
            {
                EditorUtility.DisplayDialog(
                    "Componentes do prologo faltando",
                    "Antes, aplique a primeira missao, o combate de prototipo e a colher. Depois execute este comando novamente.",
                    "OK");
                return;
            }

            SceneManager.SetActiveScene(gameScene);

            PlayerPowerLoadout2D loadout = player.GetComponent<PlayerPowerLoadout2D>();
            if (loadout == null)
            {
                loadout = player.AddComponent<PlayerPowerLoadout2D>();
            }

            GameObject figure = new GameObject("Personagem misterioso - sequencia da colher");
            figure.transform.position = enemyObject.transform.position + new Vector3(0.8f, 0.8f, 0f);
            figure.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

            SpriteRenderer figureRenderer = figure.AddComponent<SpriteRenderer>();
            figureRenderer.sprite = placeholderSprite;
            figureRenderer.color = new Color(0.2f, 0.2f, 0.3f);
            figureRenderer.sortingOrder = 18;
            figureRenderer.enabled = false;

            BoxCollider2D figureCollider = figure.AddComponent<BoxCollider2D>();
            figureCollider.isTrigger = true;
            figureCollider.enabled = false;

            PrologueStorySequence2D story = figure.AddComponent<PrologueStorySequence2D>();
            story.PromptText = "Falar com o desconhecido";
            SetReference(story, "player", player.transform);
            SetReference(story, "quest", quest);
            SetReference(story, "fireEnemy", enemy);
            SetReference(story, "fireShooter", shooter);
            SetReference(story, "playerPowers", loadout);
            SetReference(story, "dialogueManager", dialogue);
            SetReference(story, "figureRenderer", figureRenderer);
            SetReference(story, "interactionCollider", figureCollider);

            SetReference(spoon, "prologueStory", story);
            SetReference(scrapyardNpc, "prologueStory", story);
            GameObject ratObject = FindInScene(gameScene, "Rato mutante");
            if (ratObject != null)
            {
                story.ConfigureMutantRat(ratObject.GetComponent<EnemyHealth2D>());
            }
            enemyObject.SetActive(false);

            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene);
            AssetDatabase.SaveAssets();
            Debug.Log("Sequencia da colher adicionada: a emboscada inicia ao pegar a colher, e o desconhecido retorna apos a entrega da missao.");
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
