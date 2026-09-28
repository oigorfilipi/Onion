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
        if (quest == null)
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
        GUI.Box(new Rect(20f, 20f, 370f, 66f), "Objetivo: " + objective, objectiveStyle);
    }
}
