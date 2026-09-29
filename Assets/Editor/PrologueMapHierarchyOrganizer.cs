#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PrologueMapHierarchyOrganizer
{
    private const string GameScenePath = "Assets/Scenes/Jogo.unity";

    private sealed class GroupDefinition
    {
        public readonly string Name;
        public readonly string[] ObjectNames;

        public GroupDefinition(string name, params string[] objectNames)
        {
            Name = name;
            ObjectNames = objectNames;
        }
    }

    private static readonly GroupDefinition[] Groups =
    {
        new GroupDefinition("MAPA | Chao, caminhos e limites",
            "Chao temporario",
            "Extensao de chao leste - placeholder",
            "Estrada leste - placeholder",
            "Limite norte",
            "Limite sul",
            "Limite oeste",
            "Limite leste",
            "Limite leste - norte da passagem",
            "Limite leste - sul da passagem",
            "Limite leste - fim do trecho",
            "Trigger - saida leste da cidade",
            "Ponto inicial - casa do protagonista"),
        new GroupDefinition("PERSONAGENS | Jogador e moradores",
            "Player",
            "Madrasta - cozinha",
            "NPC - Dono do ferro-velho",
            "NPC - Estranho (missao do arco)",
            "Personagem misterioso - sequencia da colher",
            "Mercearia - Vendedor de pao"),
        new GroupDefinition("INIMIGOS | Encontros",
            "Inimigo de fogo - prototipo",
            "Rato mutante",
            "Inimigo de patrulha - teste IA"),
        new GroupDefinition("ITENS | Coletas, poderes e interacoes",
            "Peca de metal 1",
            "Peca de metal 2",
            "Poder de Contato - Colher brilhante",
            "Poder de Contato - Luva vermelha",
            "Poder de Absorcao - Maca radioativa",
            "Vaso sanitario - remover poder de maca",
            "Coletavel - Graveto 1",
            "Coletavel - Graveto 2",
            "Bau - cinco moedas",
            "Coletavel - Dedo humano"),
        new GroupDefinition("EQUIPAMENTOS | Drops",
            "Equipamento - Peitoral de metal",
            "Equipamento - Capacete de metal",
            "Equipamento - Botas de metal",
            "Equipamento - Escudo de metal",
            "Equipamento - Mochila"),
        new GroupDefinition("SISTEMAS | HUD e missoes",
            "Inventory HUD",
            "Combat HUD",
            "Side Quest HUD",
            "Prologue Quest Systems",
            "Equipment Setup Marker"),
        new GroupDefinition("CENA | Camera e iluminacao",
            "Main Camera",
            "Luz global 2D")
    };

    [MenuItem("Prototipo/Organizar Hierarchy do mapa")]
    public static void OrganizeHierarchy()
    {
        Scene originalActiveScene = SceneManager.GetActiveScene();
        Scene gameScene = SceneManager.GetSceneByPath(GameScenePath);
        bool wasAlreadyLoaded = gameScene.IsValid() && gameScene.isLoaded;

        if (!wasAlreadyLoaded)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GameScenePath) == null)
            {
                EditorUtility.DisplayDialog("Cena Jogo nao encontrada", "A cena esperada em Assets/Scenes/Jogo.unity nao foi encontrada.", "OK");
                return;
            }

            gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Additive);
        }

        try
        {
            SceneManager.SetActiveScene(gameScene);

            Dictionary<string, GameObject> objectsToMove = new Dictionary<string, GameObject>();
            List<string> duplicateNames = new List<string>();
            List<string> missingNames = new List<string>();

            foreach (GroupDefinition group in Groups)
            {
                foreach (string objectName in group.ObjectNames)
                {
                    List<GameObject> matches = FindObjectsByExactName(gameScene, objectName);
                    if (matches.Count == 1)
                    {
                        objectsToMove.Add(objectName, matches[0]);
                    }
                    else if (matches.Count == 0)
                    {
                        missingNames.Add(objectName);
                    }
                    else
                    {
                        duplicateNames.Add(objectName);
                    }
                }
            }

            if (duplicateNames.Count > 0)
            {
                EditorUtility.DisplayDialog(
                    "Nomes duplicados encontrados",
                    "Para evitar mover o objeto errado, nada foi alterado. Nomes repetidos: " + string.Join(", ", duplicateNames.ToArray()),
                    "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Organizar Hierarchy do mapa");
            int movedCount = 0;

            foreach (GroupDefinition definition in Groups)
            {
                GameObject groupObject = FindRootObjectByName(gameScene, definition.Name);
                if (groupObject == null)
                {
                    groupObject = new GameObject(definition.Name);
                    SceneManager.MoveGameObjectToScene(groupObject, gameScene);
                    Undo.RegisterCreatedObjectUndo(groupObject, "Criar grupo da Hierarchy");
                }

                foreach (string objectName in definition.ObjectNames)
                {
                    GameObject target;
                    if (!objectsToMove.TryGetValue(objectName, out target)) continue;
                    if (target.transform.parent == groupObject.transform) continue;

                    Undo.SetTransformParent(target.transform, groupObject.transform, "Mover objeto para grupo da Hierarchy");
                    movedCount++;
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(gameScene);
            bool saved = EditorSceneManager.SaveScene(gameScene);
            AssetDatabase.SaveAssets();

            StringBuilder summary = new StringBuilder();
            summary.Append("Hierarchy organizada: ").Append(movedCount).Append(" objetos movidos para ").Append(Groups.Length).Append(" grupos.");
            if (missingNames.Count > 0)
            {
                summary.Append(" Objetos nao encontrados e ignorados: ").Append(string.Join(", ", missingNames.ToArray())).Append('.');
            }
            summary.Append(saved ? " A cena Jogo foi salva." : " A cena nao foi salva; salve-a manualmente.");

            Debug.Log(summary.ToString());
            EditorUtility.DisplayDialog("Organizacao concluida", summary.ToString(), "OK");
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

    private static GameObject FindRootObjectByName(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == objectName) return root;
        }

        return null;
    }

    private static List<GameObject> FindObjectsByExactName(Scene scene, string objectName)
    {
        List<GameObject> matches = new List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform item in transforms)
            {
                if (item.name == objectName) matches.Add(item.gameObject);
            }
        }

        return matches;
    }
}
#endif
