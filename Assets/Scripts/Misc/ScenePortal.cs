using System.Collections;
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
    [SerializeField, Min(1f)] private float combatIntroSeconds = 4f;
    [SerializeField, TextArea(2, 4)] private string combatIntroText =
        "A porta se abre...\nLá fora, os slimes avançam. Sobreviva!";
    private static float nextTransitionTime;

    // Salva a posição e indica a âncora de chegada antes de carregar a outra cena.
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (Time.timeScale <= 0f || Time.unscaledTime < nextTransitionTime ||
            player == null || string.IsNullOrWhiteSpace(destinationScene)) return;

        PlayerVitals vitals = player.GetComponent<PlayerVitals>();
        if (vitals != null && vitals.IsDead) return;

        bool enteringCombat = SceneManager.GetActiveScene().name == "CasaInterior" &&
                              destinationScene == "SampleScene";
        nextTransitionTime = Time.unscaledTime + (enteringCombat ? combatIntroSeconds + 0.75f : 0.75f);
        FindAnyObjectByType<SaveController>()?.SaveGame();
        GameSession.ChooseSceneTransition(destinationSpawnId);
        if (enteringCombat) StartCoroutine(PlayCombatIntro(player));
        else SceneManager.LoadScene(destinationScene);
    }

    private IEnumerator PlayCombatIntro(PlayerController player)
    {
        player.enabled = false;
        ActiveWeapon weapon = player.GetComponentInChildren<ActiveWeapon>(true);
        if (weapon != null) weapon.enabled = false;
        OnionMenuController pauseMenu = FindAnyObjectByType<OnionMenuController>();
        if (pauseMenu != null && pauseMenu.IsPauseMenu) pauseMenu.enabled = false;
        GameAudio.PlayStoryIntro();
        RuntimeGameUI.ShowStory(combatIntroText, combatIntroSeconds);
        yield return new WaitForSecondsRealtime(combatIntroSeconds);
        RuntimeGameUI.HideStory();
        SceneManager.LoadScene(destinationScene);
    }
}
