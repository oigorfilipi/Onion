using UnityEngine;

public sealed class SideQuestObjectiveHUD2D : MonoBehaviour
{
    [SerializeField] private PrologueSideQuest2D sideQuest;

    private void Awake()
    {
        if (sideQuest == null)
        {
            sideQuest = Object.FindFirstObjectByType<PrologueSideQuest2D>();
        }
    }

    private void OnGUI()
    {
        if (sideQuest == null || !sideQuest.IsTracking || Time.timeScale == 0f)
        {
            return;
        }

        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 15,
            wordWrap = true
        };

        float panelWidth = Mathf.Min(370f, Screen.width - 40f);
        float panelX = Screen.width - panelWidth - 20f;
        GUI.Box(new Rect(panelX, 94f, panelWidth, 58f), "Missao secundaria: " + sideQuest.ObjectiveText, style);
    }
}
