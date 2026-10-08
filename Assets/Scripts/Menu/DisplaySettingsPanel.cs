using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Lê e aplica resolução e modo de tela persistidos nas preferências locais.
/// </summary>
public class DisplaySettingsPanel : MonoBehaviour
{
    private const string ResolutionKey = "OnionResolution";
    private const string DisplayModeKey = "OnionDisplayMode";

    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown displayModeDropdown;

    private void OnEnable()
    {
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(new List<string> { "1920 × 1080", "1280 × 720" });
            resolutionDropdown.SetValueWithoutNotify(Mathf.Clamp(PlayerPrefs.GetInt(ResolutionKey, 0), 0, 1));
            resolutionDropdown.RefreshShownValue();
        }

        if (displayModeDropdown != null)
        {
            displayModeDropdown.ClearOptions();
            displayModeDropdown.AddOptions(new List<string> { "Tela cheia", "Sem bordas" });
            displayModeDropdown.SetValueWithoutNotify(Mathf.Clamp(PlayerPrefs.GetInt(DisplayModeKey, 1), 0, 1));
            displayModeDropdown.RefreshShownValue();
        }
    }

    public void ApplySelectedSettings()
    {
        int resolutionIndex = resolutionDropdown != null ? resolutionDropdown.value : 0;
        int displayModeIndex = displayModeDropdown != null ? displayModeDropdown.value : 1;
        PlayerPrefs.SetInt(ResolutionKey, resolutionIndex);
        PlayerPrefs.SetInt(DisplayModeKey, displayModeIndex);
        PlayerPrefs.Save();
        Apply(resolutionIndex, displayModeIndex);
    }

    public static void ApplySavedSettings()
    {
        Apply(PlayerPrefs.GetInt(ResolutionKey, 0), PlayerPrefs.GetInt(DisplayModeKey, 1));
    }

    private static void Apply(int resolutionIndex, int displayModeIndex)
    {
        int width = resolutionIndex == 1 ? 1280 : 1920;
        int height = resolutionIndex == 1 ? 720 : 1080;
        FullScreenMode mode = displayModeIndex == 0
            ? FullScreenMode.ExclusiveFullScreen
            : FullScreenMode.FullScreenWindow;
        Screen.SetResolution(width, height, mode);
    }
}
