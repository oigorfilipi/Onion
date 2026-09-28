#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BowSideQuestSceneSetup
{
    private const string GameScenePath = "Assets/Scenes/Jogo.unity";
    private const string PlaceholderSpritePath = "Assets/Art/Placeholders/QuadradoBranco.png";

    [MenuItem("Prototipo/Adicionar missao secundaria do arco ao Jogo")]
    public static void AddBowSideQuest()
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
            if (FindInScene(gameScene, "NPC - Estranho (missao do arco)") != null)
            {
                EditorUtility.DisplayDialog("Missao ja adicionada", "O estranho e a missao do arco ja existem na cena Jogo.", "OK");
                return;
            }

            GameObject player = FindInScene(gameScene, "Player");
            PlayerInventory2D inventory = player != null ? player.GetComponent<PlayerInventory2D>() : null;
            Sprite placeholderSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
            if (player == null || inventory == null || placeholderSprite == null)
            {
                EditorUtility.DisplayDialog("Inventario necessario", "Adicione primeiro o inventario com Prototipo > Adicionar inventario e itens ao Jogo.", "OK");
                return;
            }

            SceneManager.SetActiveScene(gameScene);
            DialogueManager2D dialogue = FindComponentInScene<DialogueManager2D>(gameScene);
            if (dialogue == null)
            {
                dialogue = new GameObject("Power Dialogue System").AddComponent<DialogueManager2D>();
            }

            PlayerInteractor2D interactor = player.GetComponent<PlayerInteractor2D>();
            if (interactor == null) interactor = player.AddComponent<PlayerInteractor2D>();
            SetReference(interactor, "dialogueManager", dialogue);
            if (player.GetComponent<PlayerCombat2D>() == null) player.AddComponent<PlayerCombat2D>();
            PlayerEquipment2D equipment = player.GetComponent<PlayerEquipment2D>();
            if (equipment == null) equipment = player.AddComponent<PlayerEquipment2D>();

            GameObject inventoryHudObject = FindInScene(gameScene, "Inventory HUD");
            if (inventoryHudObject != null)
            {
                InventoryHUD2D inventoryHud = inventoryHudObject.GetComponent<InventoryHUD2D>();
                if (inventoryHud != null) SetReference(inventoryHud, "equipment", equipment);
            }

            GameObject combatHudObject = FindInScene(gameScene, "Combat HUD");
            if (combatHudObject == null) combatHudObject = new GameObject("Combat HUD");
            CombatHUD2D combatHud = combatHudObject.GetComponent<CombatHUD2D>();
            if (combatHud == null) combatHud = combatHudObject.AddComponent<CombatHUD2D>();
            SetReference(combatHud, "playerVitals", player.GetComponent<PlayerVitals>());
            SetReference(combatHud, "equipment", equipment);
            SetReference(combatHud, "inventory", inventory);

            GameObject stranger = new GameObject("NPC - Estranho (missao do arco)");
            stranger.transform.position = new Vector2(10.4f, 0.5f);
            stranger.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

            SpriteRenderer renderer = stranger.AddComponent<SpriteRenderer>();
            renderer.sprite = placeholderSprite;
            renderer.color = new Color(0.28f, 0.48f, 0.78f);
            renderer.sortingOrder = 9;

            BoxCollider2D collider = stranger.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            PrologueSideQuest2D sideQuest = stranger.AddComponent<PrologueSideQuest2D>();
            sideQuest.PromptText = "Falar com o estranho";
            sideQuest.ConfigureRequest(InventoryItemId.Stick, 2, "2 gravetos");
            sideQuest.SetPlayerInventory(inventory);
            SetReference(sideQuest, "dialogueManager", dialogue);

            GameObject hud = new GameObject("Side Quest HUD");
            SideQuestObjectiveHUD2D objectiveHud = hud.AddComponent<SideQuestObjectiveHUD2D>();
            SetReference(objectiveHud, "sideQuest", sideQuest);

            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene);
            AssetDatabase.SaveAssets();
            Debug.Log("Missao secundaria do estranho adicionada. Ela pede dois gravetos e recompensa o jogador com um arco e dez flechas.");
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
