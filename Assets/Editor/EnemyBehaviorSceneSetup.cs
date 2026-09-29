#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EnemyBehaviorSceneSetup
{
    private const string GameScenePath = "Assets/Scenes/Jogo.unity";
    private const string PlaceholderSpritePath = "Assets/Art/Placeholders/QuadradoBranco.png";
    private const string EnemyName = "Inimigo de patrulha - teste IA";
    private static readonly Vector2 EnemyPosition = new Vector2(21f, -5f);

    [MenuItem("Prototipo/Adicionar inimigo de teste com IA")]
    public static void AddEnemyBehaviorDemo()
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
            Sprite placeholderSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
            if (player == null || placeholderSprite == null)
            {
                EditorUtility.DisplayDialog("Cena incompleta", "A cena Jogo precisa conter o Player e o sprite QuadradoBranco.png.", "OK");
                return;
            }

            SceneManager.SetActiveScene(gameScene);

            GameObject enemy = FindInScene(gameScene, EnemyName);
            if (enemy == null)
            {
                enemy = new GameObject(EnemyName);
                enemy.transform.position = EnemyPosition;
                enemy.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

                SpriteRenderer renderer = enemy.AddComponent<SpriteRenderer>();
                renderer.sprite = placeholderSprite;
                renderer.color = new Color(0.3f, 0.55f, 0.78f);
                renderer.sortingOrder = 8;

                BoxCollider2D collider = enemy.AddComponent<BoxCollider2D>();
                collider.size = Vector2.one;
                collider.isTrigger = false;
            }
            else
            {
                enemy.transform.position = EnemyPosition;
            }

            Rigidbody2D body = GetOrAdd<Rigidbody2D>(enemy);
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.useFullKinematicContacts = true;

            EnemyHealth2D health = GetOrAdd<EnemyHealth2D>(enemy);
            health.ConfigureRegularEnemy(60);
            SetFloat(health, "fullRegenerationDelay", 10f);
            EditorUtility.SetDirty(health);

            EnemyFireShooter2D shooter = GetOrAdd<EnemyFireShooter2D>(enemy);
            SetReference(shooter, "target", player.transform);
            SetReference(shooter, "projectileSprite", placeholderSprite);
            SetString(shooter, "projectileName", "Bola de energia");
            SetColor(shooter, "projectileColor", new Color(0.45f, 0.8f, 1f));
            SetFloat(shooter, "attackRange", 6.8f);
            SetFloat(shooter, "secondsBetweenShots", 2.6f);
            SetFloat(shooter, "projectileSpeed", 4f);
            SetInt(shooter, "projectileDamage", 7);
            SetFloat(shooter, "projectileMaxDistance", 8f);
            SetFloat(shooter, "aimLeadTime", 0.12f);
            shooter.enabled = false;
            EditorUtility.SetDirty(shooter);

            EnemyBehavior2D behavior = GetOrAdd<EnemyBehavior2D>(enemy);
            SetReference(behavior, "target", player.transform);
            SetInt(behavior, "combatStyle", (int)EnemyCombatStyle2D.Hybrid);
            SetBool(behavior, "missionEnemy", false);
            SetFloat(behavior, "detectionRange", 7f);
            SetFloat(behavior, "maximumChaseDistance", 8f);
            SetFloat(behavior, "homeLeashRadius", 9f);
            SetFloat(behavior, "movementSpeed", 2.2f);
            SetFloat(behavior, "patrolSpeed", 0.7f);
            SetFloat(behavior, "patrolRadius", 1.4f);
            SetFloat(behavior, "preferredRange", 4.2f);
            SetFloat(behavior, "meleeAttackRange", 1.3f);
            SetFloat(behavior, "secondsBetweenMeleeAttacks", 1.4f);
            SetInt(behavior, "meleeDamage", 10);
            SetFloat(behavior, "retreatHealthRatio", 0.25f);
            EditorUtility.SetDirty(behavior);

            EnemyExperienceReward2D reward = GetOrAdd<EnemyExperienceReward2D>(enemy);
            reward.ConfigureReward(25);
            EditorUtility.SetDirty(reward);

            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene);
            AssetDatabase.SaveAssets();
            Debug.Log("Inimigo de demonstracao com IA hibrida adicionado na estrada leste. O rato da missao e o chefe do prologo nao foram alterados.");
            EditorUtility.DisplayDialog("Inimigo de IA adicionado", "O inimigo hibrido foi colocado na estrada leste (21, -5). Ele patrulha, persegue dentro de limites, atira a media distancia, ataca de perto e recua com pouca vida.", "OK");
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

    private static void SetFloat(Object target, string propertyName, float value)
    {
        SerializedObject serializedTarget = new SerializedObject(target);
        SerializedProperty property = serializedTarget.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"Campo serializado '{propertyName}' nao encontrado em {target.GetType().Name}.", target);
            return;
        }

        property.floatValue = value;
        serializedTarget.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetInt(Object target, string propertyName, int value)
    {
        SerializedObject serializedTarget = new SerializedObject(target);
        SerializedProperty property = serializedTarget.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"Campo serializado '{propertyName}' nao encontrado em {target.GetType().Name}.", target);
            return;
        }

        property.intValue = value;
        serializedTarget.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetBool(Object target, string propertyName, bool value)
    {
        SerializedObject serializedTarget = new SerializedObject(target);
        SerializedProperty property = serializedTarget.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"Campo serializado '{propertyName}' nao encontrado em {target.GetType().Name}.", target);
            return;
        }

        property.boolValue = value;
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

    private static void SetColor(Object target, string propertyName, Color value)
    {
        SerializedObject serializedTarget = new SerializedObject(target);
        SerializedProperty property = serializedTarget.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"Campo serializado '{propertyName}' nao encontrado em {target.GetType().Name}.", target);
            return;
        }

        property.colorValue = value;
        serializedTarget.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
