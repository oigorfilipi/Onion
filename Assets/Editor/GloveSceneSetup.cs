#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GloveSceneSetup
{
    private const string GameScenePath = "Assets/Scenes/Jogo.unity";
    private const string PlaceholderSpritePath = "Assets/Art/Placeholders/QuadradoBranco.png";

    [MenuItem("Prototipo/Adicionar luva vermelha ao Jogo")]
    public static void AddRedGlove()
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
            if (FindInScene(gameScene, "Poder de Contato - Luva vermelha") != null)
            {
                EditorUtility.DisplayDialog("Luva ja adicionada", "A luva vermelha ja existe na cena Jogo.", "OK");
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

            PlayerPowerLoadout2D powerLoadout = player.GetComponent<PlayerPowerLoadout2D>();
            if (powerLoadout == null) powerLoadout = player.AddComponent<PlayerPowerLoadout2D>();

            PlayerMaterialPouch2D materialPouch = player.GetComponent<PlayerMaterialPouch2D>();
            if (materialPouch == null) materialPouch = player.AddComponent<PlayerMaterialPouch2D>();

            if (player.GetComponent<PlayerCombat2D>() == null) player.AddComponent<PlayerCombat2D>();

            DialogueManager2D dialogueManager = FindInScene(gameScene, "Prologue Quest Systems")?.GetComponent<DialogueManager2D>();
            if (dialogueManager == null)
            {
                dialogueManager = FindInScene(gameScene, "Power Dialogue System")?.GetComponent<DialogueManager2D>();
            }
            if (dialogueManager == null)
            {
                GameObject dialogueObject = new GameObject("Power Dialogue System");
                dialogueManager = dialogueObject.AddComponent<DialogueManager2D>();
            }

            PlayerInteractor2D interactor = player.GetComponent<PlayerInteractor2D>();
            if (interactor == null) interactor = player.AddComponent<PlayerInteractor2D>();
            SetReference(interactor, "dialogueManager", dialogueManager);

            GameObject hudObject = FindInScene(gameScene, "Combat HUD");
            CombatHUD2D combatHud;
            if (hudObject == null)
            {
                hudObject = new GameObject("Combat HUD");
                combatHud = hudObject.AddComponent<CombatHUD2D>();
            }
            else
            {
                combatHud = hudObject.GetComponent<CombatHUD2D>();
                if (combatHud == null) combatHud = hudObject.AddComponent<CombatHUD2D>();
            }
            SetReference(combatHud, "playerVitals", player.GetComponent<PlayerVitals>());
            SetReference(combatHud, "powerLoadout", powerLoadout);
            SetReference(combatHud, "materialPouch", materialPouch);

            CreateGlove(placeholderSprite, dialogueManager);

            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene);
            AssetDatabase.SaveAssets();
            Debug.Log("Luva vermelha adicionada. Recolha-a, aperte Tab para equipa-la e use F/G nas habilidades especiais.");
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

    private static void CreateGlove(Sprite sprite, DialogueManager2D dialogueManager)
    {
        GameObject glove = new GameObject("Poder de Contato - Luva vermelha");
        glove.transform.position = new Vector2(-3.6f, -3.5f);
        glove.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

        SpriteRenderer renderer = glove.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(0.86f, 0.08f, 0.12f);
        renderer.sortingOrder = 12;

        BoxCollider2D collider = glove.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;

        PowerPickup2D pickup = glove.AddComponent<PowerPickup2D>();
        pickup.PromptText = "Pegar a luva";
        SetEnumValue(pickup, "contactPower", (int)ContactPowerId.RedGloveStrength);
        SetString(pickup, "itemName", "Luva vermelha radioativa");
        SetString(pickup, "successDescription", "A luva ficou ligada a voce. Equipada, ela aumenta o dano dos ataques basicos e libera duas habilidades: soco forte com F e impacto em area com G.");
        SetReference(pickup, "dialogueManager", dialogueManager);
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

    private static void SetEnumValue(Object target, string propertyName, int enumIndex)
    {
        SerializedObject serializedTarget = new SerializedObject(target);
        SerializedProperty property = serializedTarget.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"Campo serializado '{propertyName}' nao encontrado em {target.GetType().Name}.", target);
            return;
        }

        property.enumValueIndex = enumIndex;
        serializedTarget.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetString(Object target, string propertyName, string value)
    {
        SerializedObject serializedTarget = new SerializedObject(target);
        SerializedProperty property = serializedTarget.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"Campo serializado '{propertyName}' nao encontrado em {target.GetType().Name}.", target);
            return;
        }

        property.stringValue = value;
        serializedTarget.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
