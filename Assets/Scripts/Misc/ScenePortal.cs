using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Salva a partida e transporta o jogador entre a casa e o mapa externo.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ScenePortal : MonoBehaviour
{
    [SerializeField] private string destinationScene;
    [SerializeField] private string destinationSpawnId;
    private static float nextTransitionTime;

    // Salva a posição e indica a âncora de chegada antes de carregar a outra cena.
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (Time.timeScale <= 0f || Time.unscaledTime < nextTransitionTime ||
            player == null || string.IsNullOrWhiteSpace(destinationScene)) return;

        PlayerVitals vitals = player.GetComponent<PlayerVitals>();
        if (vitals != null && vitals.IsDead) return;

        nextTransitionTime = Time.unscaledTime + 0.75f;
        FindAnyObjectByType<SaveController>()?.SaveGame();
        GameSession.ChooseSceneTransition(destinationSpawnId);
        SceneManager.LoadScene(destinationScene);
    }
}
