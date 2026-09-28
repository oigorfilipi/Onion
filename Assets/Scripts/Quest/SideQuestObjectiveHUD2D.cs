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
        if (sideQuest == null || !sideQuest.IsTracking)
        {
            return;
        }

        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 15,
            wordWrap = true
        };

        GUI.Box(new Rect(20f, 94f, 370f, 58f), "Missao secundaria: " + sideQuest.ObjectiveText, style);
    }
}
