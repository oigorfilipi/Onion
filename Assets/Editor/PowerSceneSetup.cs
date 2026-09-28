#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PowerSceneSetup
{
    private const string GameScenePath = "Assets/Scenes/Jogo.unity";
    private const string PlaceholderSpritePath = "Assets/Art/Placeholders/QuadradoBranco.png";

    [MenuItem("Prototipo/Adicionar colher e poder de magnetismo ao Jogo")]
    public static void AddMagneticSpoon()
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
            if (FindInScene(gameScene, "Poder de Contato - Colher brilhante") != null)
            {
                EditorUtility.DisplayDialog("Colher ja adicionada", "A colher brilhante ja existe na cena Jogo.", "OK");
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

            GameObject spoon = new GameObject("Poder de Contato - Colher brilhante");
            spoon.transform.position = new Vector2(1.5f, 2.8f);
            spoon.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

            SpriteRenderer renderer = spoon.AddComponent<SpriteRenderer>();
            renderer.sprite = placeholderSprite;
            renderer.color = new Color(0.38f, 1f, 0.48f);
            renderer.sortingOrder = 12;

            BoxCollider2D collider = spoon.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            PowerPickup2D pickup = spoon.AddComponent<PowerPickup2D>();
            pickup.PromptText = "Pegar a colher";
            SetReference(pickup, "dialogueManager", dialogueManager);

            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene);
            AssetDatabase.SaveAssets();
            Debug.Log("Colher e poder de magnetismo adicionados. Recolha pecas de metal e aperte Q para arremessar uma.");
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
