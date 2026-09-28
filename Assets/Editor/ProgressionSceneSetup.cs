#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ProgressionSceneSetup
{
    private const string GameScenePath = "Assets/Scenes/Jogo.unity";
    private const int MutantRatExperience = 50;

    [MenuItem("Prototipo/Adicionar experiencia e niveis ao Jogo")]
    public static void AddProgressionToGame()
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
            if (player == null)
            {
                EditorUtility.DisplayDialog("Jogador nao encontrado", "A cena Jogo precisa conter um objeto chamado Player.", "OK");
                return;
            }

            SceneManager.SetActiveScene(gameScene);

            PlayerProgression2D progression = GetOrAdd<PlayerProgression2D>(player);
            GameObject hudObject = FindInScene(gameScene, "Combat HUD");
            CombatHUD2D combatHud = hudObject != null
                ? GetOrAdd<CombatHUD2D>(hudObject)
                : new GameObject("Combat HUD").AddComponent<CombatHUD2D>();
            SetReference(combatHud, "playerVitals", player.GetComponent<PlayerVitals>());
            SetReference(combatHud, "progression", progression);

            GameObject mutantRat = FindInScene(gameScene, "Rato mutante");
            if (mutantRat != null)
            {
                EnemyHealth2D health = mutantRat.GetComponent<EnemyHealth2D>();
                if (health != null)
                {
                    EnemyExperienceReward2D reward = GetOrAdd<EnemyExperienceReward2D>(mutantRat);
                    reward.ConfigureReward(MutantRatExperience);
                }
            }

            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene);
            AssetDatabase.SaveAssets();
            Debug.Log("Progressao adicionada: niveis ate 200, barra de experiencia e recompensa de XP no rato mutante.");
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
}
#endif
