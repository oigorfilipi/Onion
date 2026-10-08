using UnityEngine;

/// <summary>
/// Localiza uma âncora nomeada para nascimento ou chegada depois de um portal.
/// </summary>
public class SceneSpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnId;

    public static bool TryFind(string id, out Vector3 position)
    {
        foreach (SceneSpawnPoint point in FindObjectsByType<SceneSpawnPoint>())
        {
            if (point.spawnId == id)
            {
                position = point.transform.position;
                return true;
            }
        }

        position = Vector3.zero;
        return false;
    }
}
