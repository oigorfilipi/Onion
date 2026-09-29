using UnityEngine;

public sealed class QuestObjectiveHUD : MonoBehaviour
{
    [SerializeField] private PrologueQuest quest;
    [SerializeField] private PrologueStorySequence2D prologueStory;

    private void Awake()
    {
        if (prologueStory == null)
        {
            prologueStory = Object.FindFirstObjectByType<PrologueStorySequence2D>();
        }
    }

    private void OnGUI()
    {
        if (quest == null || Time.timeScale == 0f)
        {
            return;
        }

        GUIStyle objectiveStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 16,
            wordWrap = true
        };

        string objective = quest.CurrentPhase == PrologueQuest.ScrapQuestPhase.Completed &&
            prologueStory != null && prologueStory.HasPrologueObjective
            ? prologueStory.PrologueObjectiveText
            : quest.ObjectiveText;
        float panelWidth = Mathf.Min(370f, Screen.width - 40f);
        float panelX = Screen.width - panelWidth - 20f;
        GUI.Box(new Rect(panelX, 20f, panelWidth, 66f), "Objetivo: " + objective, objectiveStyle);
    }
}
