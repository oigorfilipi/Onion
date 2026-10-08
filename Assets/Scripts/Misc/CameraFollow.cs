using UnityEngine;

/// <summary>
/// Centraliza a câmera no player depois do carregamento e a acompanha suavemente.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float smoothSpeed = 5f;

    private float depthOffset;
    private bool hasCentered;

    private void Start()
    {
        if (player == null)
        {
            GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
            if (foundPlayer != null) player = foundPlayer.transform;
        }

        if (player != null) depthOffset = transform.position.z - player.position.z;
    }

    private void LateUpdate()
    {
        if (player == null) return;

        Vector3 targetPosition = new Vector3(player.position.x, player.position.y,
                                             player.position.z + depthOffset);

        // O save e os portais posicionam o Player em Start. Centralizar no primeiro
        // LateUpdate impede que a câmera herde a posição antiga da cena copiada.
        if (!hasCentered)
        {
            transform.position = targetPosition;
            hasCentered = true;
            return;
        }

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Mathf.Clamp01(smoothSpeed * Time.deltaTime)
        );
    }
}
