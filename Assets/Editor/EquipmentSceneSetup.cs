#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EquipmentSceneSetup
{
    private const string GameScenePath = "Assets/Scenes/Jogo.unity";
    private const string PlaceholderSpritePath = "Assets/Art/Placeholders/QuadradoBranco.png";

    [MenuItem("Prototipo/Adicionar equipamentos ao Jogo")]
    public static void AddEquipment()
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
            if (FindInScene(gameScene, "Equipment Setup Marker") != null)
            {
                EditorUtility.DisplayDialog("Equipamentos ja adicionados", "Os equipamentos provisórios ja estao na cena Jogo.", "OK");
                return;
            }

            GameObject player = FindInScene(gameScene, "Player");
            GameObject inventoryHudObject = FindInScene(gameScene, "Inventory HUD");
            Sprite placeholderSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
            if (player == null || inventoryHudObject == null || placeholderSprite == null)
            {
                EditorUtility.DisplayDialog("Inventario necessario", "Adicione primeiro o inventario e os itens com Prototipo > Adicionar inventario e itens ao Jogo.", "OK");
                return;
            }

            SceneManager.SetActiveScene(gameScene);

            PlayerEquipment2D equipment = player.GetComponent<PlayerEquipment2D>();
            if (equipment == null) equipment = player.AddComponent<PlayerEquipment2D>();
            InventoryHUD2D inventoryHud = inventoryHudObject.GetComponent<InventoryHUD2D>();
            if (inventoryHud != null) SetReference(inventoryHud, "equipment", equipment);

            GameObject marker = new GameObject("Equipment Setup Marker");
            marker.transform.position = Vector3.zero;

            CreateArmorPickup("Equipamento - Peitoral de metal", InventoryItemId.ChestArmor,
                new Vector2(8f, 2f), placeholderSprite, new Color(0.56f, 0.62f, 0.69f));
            CreateArmorPickup("Equipamento - Capacete de metal", InventoryItemId.Helmet,
                new Vector2(-9f, 6.8f), placeholderSprite, new Color(0.72f, 0.77f, 0.82f));
            CreateArmorPickup("Equipamento - Botas de metal", InventoryItemId.MetalBoots,
                new Vector2(-10f, -6.5f), placeholderSprite, new Color(0.43f, 0.49f, 0.57f));

            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene);
            AssetDatabase.SaveAssets();
            Debug.Log("Sistema de equipamento e tres pecas de armadura adicionados. O NPC do ferro-velho entrega a espada apos a missao.");
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

    private static void CreateArmorPickup(string objectName, InventoryItemId itemId, Vector2 position, Sprite sprite, Color color)
    {
        GameObject armor = new GameObject(objectName);
        armor.transform.position = position;
        armor.transform.localScale = Vector3.one * 0.72f;

        SpriteRenderer renderer = armor.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = 11;

        BoxCollider2D collider = armor.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;

        WorldItemPickup2D pickup = armor.AddComponent<WorldItemPickup2D>();
        pickup.ConfigureItem(itemId, 1);
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
