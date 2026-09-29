#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BreadShopSceneSetup
{
    private const string GameScenePath = "Assets/Scenes/Jogo.unity";
    private const string PlaceholderSpritePath = "Assets/Art/Placeholders/QuadradoBranco.png";
    private const string ShopObjectName = "Mercearia - Vendedor de pao";
    private static readonly Vector2 ShopPosition = new Vector2(-8f, 5.5f);

    [MenuItem("Prototipo/Configurar compra de pao na mercearia")]
    public static void ConfigureBreadShop()
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
            GameObject player = FindInScene(gameScene, "Player");
            Sprite breadSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
            if (player == null || breadSprite == null)
            {
                EditorUtility.DisplayDialog("Cena incompleta", "A cena Jogo precisa conter o Player e o sprite QuadradoBranco.png.", "OK");
                return;
            }

            SceneManager.SetActiveScene(gameScene);
            PlayerInventory2D inventory = GetOrAdd<PlayerInventory2D>(player);
            PlayerWallet2D wallet = GetOrAdd<PlayerWallet2D>(player);
            PlayerInteractor2D interactor = GetOrAdd<PlayerInteractor2D>(player);
            DialogueManager2D dialogue = FindComponentInScene<DialogueManager2D>(gameScene);
            if (dialogue == null)
            {
                dialogue = new GameObject("Power Dialogue System").AddComponent<DialogueManager2D>();
            }

            SetReference(interactor, "dialogueManager", dialogue);

            GameObject shopObject = FindInScene(gameScene, ShopObjectName) ?? FindInScene(gameScene, "Mercearia - Pao");
            if (shopObject == null)
            {
                shopObject = new GameObject(ShopObjectName);
                shopObject.transform.position = ShopPosition;
                shopObject.transform.localScale = Vector3.one * 0.65f;
            }
            else
            {
                shopObject.name = ShopObjectName;
            }

            SpriteRenderer renderer = GetOrAdd<SpriteRenderer>(shopObject);
            renderer.sprite = breadSprite;
            renderer.color = new Color(0.88f, 0.69f, 0.36f);
            renderer.sortingOrder = 11;

            BoxCollider2D collider = GetOrAdd<BoxCollider2D>(shopObject);
            collider.isTrigger = true;

            WorldItemPickup2D shop = GetOrAdd<WorldItemPickup2D>(shopObject);
            shop.ConfigureShopItem(InventoryItemId.Bread, 1, WorldItemPickup2D.PrototypeBreadPrice, "Mercearia");
            EditorUtility.SetDirty(shop);
            EditorUtility.SetDirty(wallet);
            EditorUtility.SetDirty(inventory);
            EditorUtility.SetDirty(interactor);

            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene);
            AssetDatabase.SaveAssets();
            Debug.Log("Compra de pao configurada: uma unidade por tres moedas, sem consumir o vendedor.");
            EditorUtility.DisplayDialog("Mercearia configurada", "Aperte F perto do vendedor para comprar 1 pao por 3 moedas. O pao recupera 50 de vida ao ser usado no inventario.", "OK");
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
