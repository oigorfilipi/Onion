using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MainMenuScreen : MonoBehaviour
{
    private const string GameSceneName = "Jogo";

    private void OnGUI()
    {
        float buttonWidth = Mathf.Min(360f, Screen.width * 0.7f);
        float buttonHeight = 64f;
        Rect buttonRect = new Rect(
            (Screen.width - buttonWidth) * 0.5f,
            Screen.height * 0.5f - buttonHeight - 8f,
            buttonWidth,
            buttonHeight);

        if (GUI.Button(buttonRect, "NOVO JOGO"))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameSceneName);
        }

        buttonRect.y += buttonHeight + 16f;
        if (GUI.Button(buttonRect, "SAIR"))
        {
            Application.Quit();
        }
    }
}
