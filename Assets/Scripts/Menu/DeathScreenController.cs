using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Apresenta telas separadas de vitória e derrota com fundo desfocado.
/// </summary>
public class DeathScreenController : MonoBehaviour
{
    [SerializeField] private GameObject deathRoot;
    [SerializeField] private RawImage blurredBackground;
    [SerializeField] private Material blurMaterial;
    [SerializeField] private TMP_Text defeatText;
    [SerializeField] private string homeSceneName = "CasaInterior";
    [SerializeField] private GameObject victoryRoot;
    [SerializeField] private RawImage victoryBackground;
    [SerializeField] private TMP_Text victoryText;
    [SerializeField] private TMP_Text victoryButtonText;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private PlayerVitals playerVitals;
    private Texture2D capturedScreenshot;
    private bool showing;
    public bool IsShowing => showing;
    public TMP_Text HeadlineTemplate => defeatText;

    private void Awake()
    {
        if (deathRoot != null) deathRoot.SetActive(false);
        if (victoryRoot != null) victoryRoot.SetActive(false);
    }

    private void Start()
    {
        playerVitals = FindAnyObjectByType<PlayerVitals>();
        if (playerVitals != null) playerVitals.Died += OnPlayerDied;
    }

    private void Update()
    {
        if (!showing && playerVitals != null && playerVitals.IsDead) OnPlayerDied();
    }

    // Salva a derrota, oculta interfaces de jogo e prepara a tela vermelha de fim da tentativa.
    private void OnPlayerDied()
    {
        if (showing || deathRoot == null) return;
        showing = true;
        GameAudio.PlayDefeat();
        MenuController inventoryMenu = FindAnyObjectByType<MenuController>();
        if (inventoryMenu != null && inventoryMenu.menuCanvas != null)
            inventoryMenu.menuCanvas.SetActive(false);
        FindAnyObjectByType<BackpackController>()?.CloseWindow();
        FindAnyObjectByType<NpcDialogueUI>()?.CloseDialogue();
        FindAnyObjectByType<PlayerVitalsHUD>()?.HideImmediately();
        FindAnyObjectByType<SaveController>()?.SaveGame();
        StartCoroutine(CaptureAndShow(false));
    }

    // Exibe a cópia verde da tela de derrota quando o tempo da partida termina.
    public void ShowVictory()
    {
        if (showing || victoryRoot == null) return;
        showing = true;
        GameAudio.PlayVictory();
        MenuController inventoryMenu = FindAnyObjectByType<MenuController>();
        if (inventoryMenu != null && inventoryMenu.menuCanvas != null)
            inventoryMenu.menuCanvas.SetActive(false);
        FindAnyObjectByType<BackpackController>()?.CloseWindow();
        FindAnyObjectByType<NpcDialogueUI>()?.CloseDialogue();
        FindAnyObjectByType<PlayerVitalsHUD>()?.HideImmediately();
        Time.timeScale = 0f;
        StartCoroutine(CaptureAndShow(true));
    }

    private IEnumerator CaptureAndShow(bool victory)
    {
        yield return new WaitForEndOfFrame();
        RawImage background = victory ? victoryBackground : blurredBackground;
        if (background != null)
        {
            capturedScreenshot = ScreenCapture.CaptureScreenshotAsTexture();
            background.texture = capturedScreenshot;
            background.raycastTarget = false;
            if (blurMaterial != null) background.material = blurMaterial;
        }

        if (victory)
        {
            if (victoryText != null)
            {
                victoryText.text = "VITÓRIA";
                victoryText.color = new Color(0.3f, 1f, 0.35f);
            }
            if (victoryButtonText != null) victoryButtonText.text = "VOLTAR AO MENU";
        }
        else if (defeatText != null)
        {
            defeatText.text = "DERROTADO";
            defeatText.color = Color.red;
        }

        Time.timeScale = 0f;
        (victory ? victoryRoot : deathRoot).SetActive(true);
    }

    public void RespawnFromBeginning()
    {
        if (!showing) return;
        string playerName = playerVitals != null ? playerVitals.PlayerName : "Jogador";
        GameSession.ChooseRestart(playerName);
        Time.timeScale = 1f;
        SceneManager.LoadScene(homeSceneName);
    }

    public void ReturnToMainMenu()
    {
        if (!showing || victoryRoot == null || !victoryRoot.activeInHierarchy) return;
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnDestroy()
    {
        if (playerVitals != null) playerVitals.Died -= OnPlayerDied;
        if (blurredBackground != null) blurredBackground.texture = null;
        if (victoryBackground != null) victoryBackground.texture = null;
        if (capturedScreenshot != null) Destroy(capturedScreenshot);
    }
}
