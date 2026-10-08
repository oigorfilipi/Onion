using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Abre e fecha o inventário pela tecla E quando não há pausa nem diálogo.
/// </summary>
public class MenuController : MonoBehaviour
{
    public GameObject menuCanvas;
    private BackpackController backpackController;

    private void Awake()
    {
        backpackController = FindAnyObjectByType<BackpackController>();
    }

    // Start is called before the first frame update
    void Start()
    {
        menuCanvas.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.timeScale <= 0f || NpcDialogueUI.IsDialogueOpen)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (backpackController == null)
            {
                backpackController = FindAnyObjectByType<BackpackController>();
            }

            backpackController?.CloseWindow();
            menuCanvas.SetActive(!menuCanvas.activeSelf);
        }
    }
}
