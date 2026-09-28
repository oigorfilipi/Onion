using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerInteractor2D : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float interactRadius = 1.25f;
    [SerializeField] private DialogueManager2D dialogueManager;

    private Interactable2D currentTarget;
    private PlayerInventory2D inventory;

    public DialogueManager2D DialogueManager => dialogueManager;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory2D>();
    }

    private void Update()
    {
        if ((dialogueManager != null && dialogueManager.BlocksWorldInput) || (inventory != null && inventory.IsOpen))
        {
            currentTarget = null;
            return;
        }

        currentTarget = FindClosestInteractable();

        Keyboard keyboard = Keyboard.current;
        if (currentTarget != null && keyboard != null && keyboard.eKey.wasPressedThisFrame)
        {
            currentTarget.Interact(this);
        }
    }

    private Interactable2D FindClosestInteractable()
    {
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, interactRadius);
        Interactable2D closest = null;
        float closestDistance = float.MaxValue;

        foreach (Collider2D nearbyCollider in nearbyColliders)
        {
            Interactable2D candidate = nearbyCollider.GetComponentInParent<Interactable2D>();
            if (candidate == null || !candidate.isActiveAndEnabled)
            {
                continue;
            }

            float distance = ((Vector2)candidate.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (distance < closestDistance)
            {
                closest = candidate;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private void OnGUI()
    {
        if (currentTarget == null || (dialogueManager != null && dialogueManager.BlocksWorldInput) || (inventory != null && inventory.IsOpen))
        {
            return;
        }

        GUIStyle promptStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18
        };

        Rect promptRect = new Rect(Screen.width * 0.5f - 170f, Screen.height - 110f, 340f, 44f);
        GUI.Box(promptRect, $"E - {currentTarget.PromptText}", promptStyle);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
