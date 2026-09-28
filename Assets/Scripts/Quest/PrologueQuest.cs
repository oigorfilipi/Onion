using UnityEngine;

public sealed class PrologueQuest : MonoBehaviour
{
    public enum ScrapQuestPhase
    {
        NotStarted,
        CollectingScrap,
        ReturnToScrapyard,
        Completed
    }

    [SerializeField, Min(1)] private int scrapRequired = 2;
    [SerializeField, Min(0)] private int scrapCollected;

    public ScrapQuestPhase CurrentPhase { get; private set; } = ScrapQuestPhase.NotStarted;
    public int ScrapRequired => scrapRequired;
    public int ScrapCollected => scrapCollected;

    public string ObjectiveText
    {
        get
        {
            switch (CurrentPhase)
            {
                case ScrapQuestPhase.NotStarted:
                    return "Fale com o dono do ferro-velho.";
                case ScrapQuestPhase.CollectingScrap:
                    return $"Recolha pecas de metal: {scrapCollected}/{scrapRequired}.";
                case ScrapQuestPhase.ReturnToScrapyard:
                    return "Volte ao ferro-velho e fale com o dono.";
                default:
                    return "Tarefa concluida. Continue explorando Otório.";
            }
        }
    }

    private void Awake()
    {
        scrapRequired = Mathf.Max(1, scrapRequired);
        scrapCollected = Mathf.Clamp(scrapCollected, 0, scrapRequired);
    }

    public void BeginScrapQuest()
    {
        if (CurrentPhase != ScrapQuestPhase.NotStarted)
        {
            return;
        }

        scrapCollected = 0;
        CurrentPhase = ScrapQuestPhase.CollectingScrap;
    }

    public bool RegisterScrap()
    {
        if (CurrentPhase != ScrapQuestPhase.CollectingScrap)
        {
            return false;
        }

        scrapCollected = Mathf.Min(scrapCollected + 1, scrapRequired);
        if (scrapCollected >= scrapRequired)
        {
            CurrentPhase = ScrapQuestPhase.ReturnToScrapyard;
        }

        return true;
    }

    public void CompleteScrapQuest()
    {
        if (CurrentPhase == ScrapQuestPhase.ReturnToScrapyard)
        {
            CurrentPhase = ScrapQuestPhase.Completed;
        }
    }
}
