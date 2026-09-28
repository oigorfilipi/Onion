using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class PrologueTownExit2D : MonoBehaviour
{
    [SerializeField] private PrologueStorySequence2D prologueStory;

    private bool exitCompleted;

    private void Awake()
    {
        Collider2D exitTrigger = GetComponent<Collider2D>();
        exitTrigger.isTrigger = true;

        if (prologueStory == null)
        {
            prologueStory = Object.FindFirstObjectByType<PrologueStorySequence2D>();
        }
    }

    public void ConfigureStory(PrologueStorySequence2D story)
    {
        prologueStory = story;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (exitCompleted || prologueStory == null || !prologueStory.CanLeaveTown ||
            other.GetComponentInParent<PlayerMovement2D>() == null)
        {
            return;
        }

        exitCompleted = prologueStory.MarkTownExited();
    }
}
