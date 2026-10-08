using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Apresenta vitória ou derrota com fundo desfocado e reinicia a tentativa na casa.
/// </summary>
public class DeathScreenController : MonoBehaviour
{
    [SerializeField] private GameObject deathRoot;
    [SerializeField] private RawImage blurredBackground;
    [SerializeField] private Material blurMaterial;
    [SerializeField] private TMP_Text defeatText;
    [SerializeField] private TMP_Text actionButtonText;
    [SerializeField] private string homeSceneName = "CasaInterior";

    private PlayerVitals playerVitals;
    private Texture2D capturedScreenshot;
    private bool showing;
    public bool IsShowing => showing;
    public TMP_Text HeadlineTemplate => defeatText;

    private void Awake()
    {
        if (deathRoot != null) deathRoot.SetActive(false);
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
        MenuController inventoryMenu = FindAnyObjectByType<MenuController>();
        if (inventoryMenu != null && inventoryMenu.menuCanvas != null)
            inventoryMenu.menuCanvas.SetActive(false);
        FindAnyObjectByType<BackpackController>()?.CloseWindow();
        FindAnyObjectByType<NpcDialogueUI>()?.CloseDialogue();
        FindAnyObjectByType<PlayerVitalsHUD>()?.HideImmediately();
        FindAnyObjectByType<SaveController>()?.SaveGame();
        StartCoroutine(CaptureAndShow(false));
    }

    // Usa a mesma estrutura visual da derrota, com título verde, após o limite de tempo.
    public void ShowVictory()
    {
        if (showing || deathRoot == null) return;
        showing = true;
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
        if (blurredBackground != null)
        {
            capturedScreenshot = ScreenCapture.CaptureScreenshotAsTexture();
            blurredBackground.texture = capturedScreenshot;
            blurredBackground.raycastTarget = false;
            if (blurMaterial != null) blurredBackground.material = blurMaterial;
        }

        if (defeatText != null)
        {
            defeatText.text = victory ? "VITÓRIA" : "DERROTADO";
            defeatText.color = victory ? new Color(0.3f, 1f, 0.35f) : Color.red;
        }

        if (actionButtonText != null)
        {
            actionButtonText.text = victory ? "RECOMEÇAR" : "RENASCER";
            actionButtonText.color = victory ? new Color(0.3f, 1f, 0.35f) : Color.white;
        }

        Time.timeScale = 0f;
        deathRoot.SetActive(true);
    }

    public void RespawnFromBeginning()
    {
        if (!showing) return;
        string playerName = playerVitals != null ? playerVitals.PlayerName : "Jogador";
        GameSession.ChooseRestart(playerName);
        Time.timeScale = 1f;
        SceneManager.LoadScene(homeSceneName);
    }

    private void OnDestroy()
    {
        if (playerVitals != null) playerVitals.Died -= OnPlayerDied;
        if (blurredBackground != null) blurredBackground.texture = null;
        if (capturedScreenshot != null) Destroy(capturedScreenshot);
    }
}
