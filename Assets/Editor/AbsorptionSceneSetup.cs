#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AbsorptionSceneSetup
{
    private const string GameScenePath = "Assets/Scenes/Jogo.unity";
    private const string PlaceholderSpritePath = "Assets/Art/Placeholders/QuadradoBranco.png";

    [MenuItem("Prototipo/Adicionar maca e remocao de poder ao Jogo")]
    public static void AddPoisonAppleAndRemoval()
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
            if (FindInScene(gameScene, "Poder de Absorcao - Maca radioativa") != null)
            {
                EditorUtility.DisplayDialog("Maca ja adicionada", "A maca radioativa ja existe na cena Jogo.", "OK");
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

            CreatePoisonApple(placeholderSprite, dialogueManager);
            CreateRemovalToilet(placeholderSprite, dialogueManager);

            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene);
            AssetDatabase.SaveAssets();
            Debug.Log("Maca radioativa e interacao do vaso adicionadas. O botao direito passa a lancar veneno enquanto a maca estiver ativa.");
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

    private static void CreatePoisonApple(Sprite sprite, DialogueManager2D dialogueManager)
    {
        GameObject apple = new GameObject("Poder de Absorcao - Maca radioativa");
        apple.transform.position = new Vector2(0f, -5.2f);
        apple.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

        SpriteRenderer renderer = apple.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(0.95f, 0.18f, 0.22f);
        renderer.sortingOrder = 12;

        BoxCollider2D collider = apple.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;

        PowerPickup2D pickup = apple.AddComponent<PowerPickup2D>();
        pickup.PromptText = "Comer a maca";
        SetEnumValue(pickup, "category", (int)PowerPickupCategory.Absorption);
        SetEnumValue(pickup, "absorptionPower", (int)AbsorptionPowerId.PoisonApple);
        SetString(pickup, "itemName", "Maca radioativa");
        SetString(pickup, "successDescription", "Voce comeu a maca radioativa. Seu ataque pesado agora cospe bolhas de veneno; cada acerto envenena o inimigo.");
        SetReference(pickup, "dialogueManager", dialogueManager);
    }

    private static void CreateRemovalToilet(Sprite sprite, DialogueManager2D dialogueManager)
    {
        GameObject toilet = new GameObject("Vaso sanitario - remover poder de maca");
        toilet.transform.position = new Vector2(-1.25f, -1.7f);
        toilet.transform.localScale = new Vector3(0.85f, 0.65f, 1f);

        SpriteRenderer renderer = toilet.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(0.82f, 0.91f, 0.96f);
        renderer.sortingOrder = 7;

        BoxCollider2D collider = toilet.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;

        AbsorptionPowerRemoval2D removal = toilet.AddComponent<AbsorptionPowerRemoval2D>();
        removal.PromptText = "Usar o vaso";
        SetReference(removal, "dialogueManager", dialogueManager);
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
