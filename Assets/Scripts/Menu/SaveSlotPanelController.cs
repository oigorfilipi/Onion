using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Alterna entre o painel de dificuldade e as cinco vagas de save, incluindo confirmação para sobrescrever.
/// </summary>
public class SaveSlotPanelController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private string startingSceneName = "CasaInterior";
    [SerializeField] private Button[] slotButtons = new Button[SaveSlotService.SlotCount];
    [SerializeField] private GameObject difficultyPanel;
    [SerializeField] private Button[] difficultyButtons = new Button[4];
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_Text headingText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject saveBackButton;
    [SerializeField] private GameObject overwritePanel;
    [SerializeField] private TMP_Text overwriteText;

    private bool creatingNewGame;
    private bool choosingDifficulty;
    private GameSession.Difficulty selectedDifficulty = GameSession.Difficulty.Medium;
    private int pendingOverwriteSlot;

    private void Awake()
    {
        for (int i = 0; i < Mathf.Min(slotButtons.Length, SaveSlotService.SlotCount); i++)
        {
            int slotIndex = i + 1;
            if (slotButtons[i] != null)
            {
                slotButtons[i].onClick.AddListener(() => SelectSlot(slotIndex));
            }
        }

        for (int i = 0; i < Mathf.Min(difficultyButtons.Length, 4); i++)
        {
            int difficultyIndex = i;
            if (difficultyButtons[i] != null)
            {
                difficultyButtons[i].onClick.AddListener(() => SelectDifficulty(difficultyIndex));
            }
        }

        if (difficultyPanel != null)
        {
            difficultyPanel.SetActive(false);
        }

        if (overwritePanel != null)
        {
            overwritePanel.SetActive(false);
        }
    }

    public void OpenNewGame()
    {
        creatingNewGame = true;
        choosingDifficulty = true;
        Open();
    }

    public void OpenContinue()
    {
        creatingNewGame = false;
        choosingDifficulty = false;
        Open();
    }

    public void Close()
    {
        pendingOverwriteSlot = 0;
        choosingDifficulty = false;
        if (difficultyPanel != null)
        {
            difficultyPanel.SetActive(false);
        }
        if (overwritePanel != null)
        {
            overwritePanel.SetActive(false);
        }

        gameObject.SetActive(false);
    }

    public void ConfirmOverwrite()
    {
        if (pendingOverwriteSlot < 1 || !creatingNewGame)
        {
            return;
        }

        int slotIndex = pendingOverwriteSlot;
        pendingOverwriteSlot = 0;
        BeginNewGame(slotIndex);
    }

    public void CancelOverwrite()
    {
        pendingOverwriteSlot = 0;
        if (overwritePanel != null)
        {
            overwritePanel.SetActive(false);
        }
    }

    private void Open()
    {
        SaveSlotService.ImportStandaloneSave();
        gameObject.SetActive(true);
        pendingOverwriteSlot = 0;
        if (overwritePanel != null)
        {
            overwritePanel.SetActive(false);
        }

        if (headingText != null)
        {
            headingText.text = creatingNewGame ? "NOVO JOGO - ESCOLHA A VAGA" : "CONTINUAR";
        }

        ShowSavePanelContent(!choosingDifficulty);

        if (playerNameInput != null)
        {
            playerNameInput.gameObject.SetActive(creatingNewGame && !choosingDifficulty);
        }

        SetStatus(string.Empty);
        RefreshSlots();
        if (difficultyPanel != null)
        {
            difficultyPanel.SetActive(choosingDifficulty);
        }
    }

    private void RefreshSlots()
    {
        for (int i = 0; i < Mathf.Min(slotButtons.Length, SaveSlotService.SlotCount); i++)
        {
            Button button = slotButtons[i];
            if (button == null)
            {
                continue;
            }

            button.gameObject.SetActive(!choosingDifficulty);
            if (choosingDifficulty)
            {
                continue;
            }

            int slotIndex = i + 1;
            bool readable = SaveSlotService.TryRead(slotIndex, out SaveData saveData);
            bool exists = SaveSlotService.Exists(slotIndex);
            bool canContinue = readable && !saveData.playerDead && !saveData.runWon;
            button.interactable = creatingNewGame || canContinue;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = readable
                    ? $"VAGA {slotIndex} - {saveData.playerName} (Nível {Mathf.Max(1, saveData.playerLevel)})" +
                      (canContinue ? string.Empty : " - TENTATIVA ENCERRADA")
                    : exists ? $"VAGA {slotIndex} - SAVE INDISPONÍVEL" : $"VAGA {slotIndex} - LIVRE";
            }
        }
    }

    // Os botões do painel de dificuldade são independentes dos cinco botões de save.
    private void SelectDifficulty(int difficultyIndex)
    {
        if (!creatingNewGame || !choosingDifficulty)
        {
            return;
        }

        switch (difficultyIndex)
        {
            case 0: selectedDifficulty = GameSession.Difficulty.Easy; break;
            case 1: selectedDifficulty = GameSession.Difficulty.Medium; break;
            case 2: selectedDifficulty = GameSession.Difficulty.Hard; break;
            case 3: selectedDifficulty = GameSession.Difficulty.Insane; break;
            default: return;
        }

        choosingDifficulty = false;
        if (difficultyPanel != null) difficultyPanel.SetActive(false);
        ShowSavePanelContent(true);
        if (playerNameInput != null) playerNameInput.gameObject.SetActive(true);
        RefreshSlots();
        SetStatus("Digite seu nome e escolha uma das cinco vagas.");
    }

    private void SelectSlot(int slotIndex)
    {
        if (choosingDifficulty) return;

        if (creatingNewGame)
        {
            if (playerNameInput == null || !SaveSlotService.IsValidName(playerNameInput.text))
            {
                SetStatus("Digite um nome de até 40 caracteres para o jogador.");
                return;
            }

            if (SaveSlotService.Exists(slotIndex))
            {
                if (overwritePanel == null)
                {
                    SetStatus("Configure o painel de confirmação antes de sobrescrever um save.");
                    return;
                }

                pendingOverwriteSlot = slotIndex;
                if (overwriteText != null)
                {
                    overwriteText.text = $"Sobrescrever a vaga {slotIndex}? O progresso anterior será perdido.";
                }

                if (overwritePanel != null)
                {
                    overwritePanel.SetActive(true);
                }

                return;
            }

            BeginNewGame(slotIndex);
            return;
        }

        if (!SaveSlotService.TryRead(slotIndex, out SaveData saveData))
        {
            SetStatus("Essa vaga não contém um save válido.");
            return;
        }

        if (saveData.playerDead || saveData.runWon)
        {
            SetStatus("Essa tentativa terminou. Escolha Novo Jogo para usar essa vaga novamente.");
            return;
        }

        GameSession.ChooseContinue(slotIndex);
        Time.timeScale = 1f;
        SceneManager.LoadScene(string.IsNullOrWhiteSpace(saveData.sceneName)
            ? gameSceneName : saveData.sceneName);
    }

    // Guarda nome, vaga e dificuldade na sessão antes de carregar a casa inicial.
    private void BeginNewGame(int slotIndex)
    {
        string playerName = playerNameInput != null ? playerNameInput.text.Trim() : string.Empty;
        if (!SaveSlotService.IsValidName(playerName))
        {
            SetStatus("Digite um nome válido antes de escolher a vaga.");
            return;
        }

        GameSession.ChooseNewGame(slotIndex, playerName, selectedDifficulty);
        Time.timeScale = 1f;
        SceneManager.LoadScene(startingSceneName);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void ShowSavePanelContent(bool show)
    {
        Image background = GetComponent<Image>();
        if (background != null) background.enabled = show;
        if (headingText != null) headingText.gameObject.SetActive(show);
        if (statusText != null) statusText.gameObject.SetActive(show);
        if (saveBackButton != null) saveBackButton.SetActive(show);
    }
}
