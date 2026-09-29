#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InventorySceneSetup
{
    private const string GameScenePath = "Assets/Scenes/Jogo.unity";
    private const string PlaceholderSpritePath = "Assets/Art/Placeholders/QuadradoBranco.png";

    [MenuItem("Prototipo/Adicionar inventario e itens ao Jogo")]
    public static void AddInventoryAndItems()
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
            if (FindInScene(gameScene, "Inventory HUD") != null)
            {
                EditorUtility.DisplayDialog("Inventario ja adicionado", "O inventario e seus itens ja foram adicionados a cena Jogo.", "OK");
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

            PlayerInventory2D inventory = GetOrAdd<PlayerInventory2D>(player);
            PlayerWallet2D wallet = GetOrAdd<PlayerWallet2D>(player);
            PlayerMaterialPouch2D materialPouch = GetOrAdd<PlayerMaterialPouch2D>(player);
            PlayerPowerLoadout2D powerLoadout = GetOrAdd<PlayerPowerLoadout2D>(player);
            PlayerVitals vitals = GetOrAdd<PlayerVitals>(player);
            PlayerInteractor2D interactor = GetOrAdd<PlayerInteractor2D>(player);
            GetOrAdd<PlayerCombat2D>(player);

            DialogueManager2D dialogue = FindComponentInScene<DialogueManager2D>(gameScene);
            if (dialogue == null)
            {
                dialogue = new GameObject("Power Dialogue System").AddComponent<DialogueManager2D>();
            }
            SetReference(interactor, "dialogueManager", dialogue);

            GameObject hudObject = new GameObject("Inventory HUD");
            InventoryHUD2D inventoryHud = hudObject.AddComponent<InventoryHUD2D>();
            SetReference(inventoryHud, "inventory", inventory);
            SetReference(inventoryHud, "wallet", wallet);
            SetReference(inventoryHud, "vitals", vitals);
            SetReference(inventoryHud, "dialogueManager", dialogue);

            GameObject combatHudObject = FindInScene(gameScene, "Combat HUD");
            CombatHUD2D combatHud = combatHudObject != null
                ? GetOrAdd<CombatHUD2D>(combatHudObject)
                : new GameObject("Combat HUD").AddComponent<CombatHUD2D>();
            SetReference(combatHud, "playerVitals", vitals);
            SetReference(combatHud, "powerLoadout", powerLoadout);
            SetReference(combatHud, "materialPouch", materialPouch);

            PrologueQuest quest = FindComponentInScene<PrologueQuest>(gameScene);
            foreach (ScrapPickup2D scrapPickup in FindComponentsInScene<ScrapPickup2D>(gameScene))
            {
                SetReference(scrapPickup, "dialogueManager", dialogue);
                if (quest != null) SetReference(scrapPickup, "quest", quest);
            }

            CreateItemPickup("Coletavel - Graveto 1", InventoryItemId.Stick, 1, new Vector2(-8f, -2f), placeholderSprite, new Color(0.55f, 0.35f, 0.18f));
            CreateItemPickup("Coletavel - Graveto 2", InventoryItemId.Stick, 1, new Vector2(-6f, -6f), placeholderSprite, new Color(0.62f, 0.4f, 0.2f));
            CreateShopItemPickup("Mercearia - Pao", InventoryItemId.Bread, 1, WorldItemPickup2D.PrototypeBreadPrice, new Vector2(-8f, 5.5f), placeholderSprite);
            CreateItemPickup("Coletavel - Dedo humano", InventoryItemId.HumanFinger, 1, new Vector2(8f, 6f), placeholderSprite, new Color(0.85f, 0.58f, 0.53f));
            CreateCoinChest(new Vector2(4f, -5f), placeholderSprite);
            GameObject mutantRat = CreateMutantRat(new Vector2(10f, -5f), placeholderSprite);
            PrologueStorySequence2D story = FindComponentInScene<PrologueStorySequence2D>(gameScene);
            if (story != null)
            {
                story.ConfigureMutantRat(mutantRat.GetComponent<EnemyHealth2D>());
            }

            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene);
            AssetDatabase.SaveAssets();
            Debug.Log("Inventario de 40 espacos, carteira, itens do prologo e rato mutante adicionados a cena Jogo.");
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

    private static void CreateItemPickup(string objectName, InventoryItemId itemId, int quantity, Vector2 position, Sprite sprite, Color color)
    {
        GameObject pickupObject = CreatePickupObject(objectName, position, sprite, color, 0.55f);
        WorldItemPickup2D pickup = pickupObject.AddComponent<WorldItemPickup2D>();
        pickup.ConfigureItem(itemId, quantity);
    }

    private static void CreateShopItemPickup(string objectName, InventoryItemId itemId, int quantity, int price, Vector2 position, Sprite sprite)
    {
        GameObject shopObject = CreatePickupObject(objectName, position, sprite, new Color(0.88f, 0.69f, 0.36f), 0.65f);
        WorldItemPickup2D shop = shopObject.AddComponent<WorldItemPickup2D>();
        shop.ConfigureShopItem(itemId, quantity, price, "Mercearia");
    }

    private static void CreateCoinChest(Vector2 position, Sprite sprite)
    {
        GameObject chest = CreatePickupObject("Bau - cinco moedas", position, sprite, new Color(0.68f, 0.43f, 0.13f), 0.85f);
        WorldItemPickup2D pickup = chest.AddComponent<WorldItemPickup2D>();
        pickup.ConfigureCoins(5, "Bau");
    }

    private static GameObject CreateMutantRat(Vector2 position, Sprite sprite)
    {
        GameObject rat = CreatePickupObject("Rato mutante", position, sprite, new Color(0.42f, 0.4f, 0.37f), 0.9f);
        rat.GetComponent<BoxCollider2D>().isTrigger = false;
        rat.AddComponent<EnemyHealth2D>();

        EnemyFireShooter2D shooter = rat.AddComponent<EnemyFireShooter2D>();
        SetReference(shooter, "projectileSprite", sprite);
        SetString(shooter, "projectileName", "Bola de queijo");
        SetColor(shooter, "projectileColor", new Color(1f, 0.83f, 0.25f));

        EnemyLootDrop2D loot = rat.AddComponent<EnemyLootDrop2D>();
        loot.Configure(InventoryItemId.Cheese, 1, sprite, new Color(1f, 0.88f, 0.42f));
        EnemyExperienceReward2D experience = rat.AddComponent<EnemyExperienceReward2D>();
        experience.ConfigureReward(50);
        return rat;
    }

    private static GameObject CreatePickupObject(string objectName, Vector2 position, Sprite sprite, Color color, float size)
    {
        GameObject pickupObject = new GameObject(objectName);
        pickupObject.transform.position = position;
        pickupObject.transform.localScale = Vector3.one * size;

        SpriteRenderer renderer = pickupObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = 11;

        BoxCollider2D collider = pickupObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        return pickupObject;
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

    private static T[] FindComponentsInScene<T>(Scene scene) where T : Component
    {
        System.Collections.Generic.List<T> components = new System.Collections.Generic.List<T>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            components.AddRange(root.GetComponentsInChildren<T>(true));
        }

        return components.ToArray();
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

    private static void SetString(Object target, string propertyName, string value)
    {
        SerializedObject serializedTarget = new SerializedObject(target);
        SerializedProperty property = serializedTarget.FindProperty(propertyName);
        if (property == null) return;
        property.stringValue = value;
        serializedTarget.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetColor(Object target, string propertyName, Color value)
    {
        SerializedObject serializedTarget = new SerializedObject(target);
        SerializedProperty property = serializedTarget.FindProperty(propertyName);
        if (property == null) return;
        property.colorValue = value;
        serializedTarget.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
