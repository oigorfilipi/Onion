#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PrologueBossSceneSetup
{
    private const string GameScenePath = "Assets/Scenes/Jogo.unity";
    private const int BossHealth = 500;
    private const float RescueHealthRatio = 0.6f;

    [MenuItem("Prototipo/Configurar chefe do prologo")]
    public static void ConfigureBoss()
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
            GameObject enemyObject = FindInScene(gameScene, "Inimigo de fogo - prototipo");
            GameObject playerObject = FindInScene(gameScene, "Player");
            GameObject storyObject = FindInScene(gameScene, "Personagem misterioso - sequencia da colher");
            if (enemyObject == null || playerObject == null || storyObject == null)
            {
                EditorUtility.DisplayDialog(
                    "Sequencia da colher necessaria",
                    "Aplique primeiro o combate e a sequencia narrativa da colher antes de configurar o chefe.",
                    "OK");
                return;
            }

            EnemyHealth2D health = enemyObject.GetComponent<EnemyHealth2D>();
            EnemyFireShooter2D shooter = enemyObject.GetComponent<EnemyFireShooter2D>();
            PrologueStorySequence2D story = storyObject.GetComponent<PrologueStorySequence2D>();
            if (health == null || shooter == null || story == null)
            {
                EditorUtility.DisplayDialog("Componentes faltando", "O inimigo ou a sequencia narrativa perdeu um componente necessario.", "OK");
                return;
            }

            SceneManager.SetActiveScene(gameScene);

            health.ConfigureBoss(BossHealth, "Inimigo de fogo");
            EditorUtility.SetDirty(health);

            EnemyDashPunch2D dashPunch = enemyObject.GetComponent<EnemyDashPunch2D>();
            if (dashPunch == null) dashPunch = enemyObject.AddComponent<EnemyDashPunch2D>();
            dashPunch.ConfigureTarget(playerObject.transform);
            dashPunch.enabled = false;
            SetInt(dashPunch, "punchDamage", 25);
            EditorUtility.SetDirty(dashPunch);

            BossCombatPattern2D attackPattern = enemyObject.GetComponent<BossCombatPattern2D>();
            if (attackPattern == null) attackPattern = enemyObject.AddComponent<BossCombatPattern2D>();
            attackPattern.enabled = false;
            SetReference(attackPattern, "fireShooter", shooter);
            SetReference(attackPattern, "dashPunch", dashPunch);
            SetFloat(attackPattern, "fireVolleyDuration", 3.6f);
            SetFloat(attackPattern, "pauseBeforeDash", 0.7f);
            SetFloat(attackPattern, "dashWindowDuration", 1.4f);
            SetFloat(attackPattern, "recoveryDuration", 1.3f);
            EditorUtility.SetDirty(attackPattern);

            SetFloat(health, "bossRegenerationDelayAfterDamage", 6f);
            SetFloat(health, "bossRegenerationInterval", 20f);
            SetInt(health, "bossRegenerationAmount", 1);

            Rigidbody2D body = enemyObject.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                body.useFullKinematicContacts = true;
                EditorUtility.SetDirty(body);
            }

            shooter.ConfigureDashPunch(dashPunch);
            shooter.enabled = false;
            SetInt(shooter, "projectileDamage", 12);
            SetFloat(shooter, "secondsBetweenShots", 1.8f);
            SetReference(shooter, "target", playerObject.transform);
            SetReference(shooter, "dashPunch", dashPunch);
            SetFloat(shooter, "projectileMaxDistance", 9f);
            SetFloat(shooter, "aimLeadTime", 0.12f);
            SetReference(story, "fireEnemy", health);
            SetReference(story, "fireShooter", shooter);
            SetReference(story, "fireDashPunch", dashPunch);
            SetReference(story, "fireCombatPattern", attackPattern);
            SetFloat(story, "fireEnemyRescueHealthRatio", RescueHealthRatio);
            enemyObject.SetActive(false);

            EditorUtility.SetDirty(shooter);
            EditorUtility.SetDirty(story);
            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene);
            AssetDatabase.SaveAssets();
            Debug.Log("Chefe do prologo configurado com 500 de vida, limite narrativo de 60%, rajada de fogo, investida, dano ampliado e regeneracao lenta.");
            EditorUtility.DisplayDialog("Chefe configurado", "Inimigo de fogo: 500 de vida, cena continua aos 300, rajada de bolas de fogo, pausa e investida em padrao, dano ampliado e regeneracao de 1 ponto a cada 20 segundos apos 6 segundos sem dano.", "OK");
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
}
#endif
