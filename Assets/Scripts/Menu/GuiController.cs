using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mostra uma página do menu de jogo por vez e informa a aba ativa à mochila.
/// </summary>
public class TabController : MonoBehaviour
{
    public Image[] tabImages;
    public GameObject[] pages;
    public int CurrentTabIndex { get; private set; } = -1;
    public event System.Action<int> TabChanged;

    // Start is called before the first frame update
    void Start()
    {
        ActivateTab(0);
    }

    public void ActivateTab(int tabNo)
    {
        if (pages == null || pages.Length == 0)
        {
            Debug.LogError("TabController: nenhuma página foi configurada.", this);
            return;
        }

        if (tabNo < 0 || tabNo >= pages.Length)
        {
            Debug.LogError($"TabController: índice de aba inválido ({tabNo}).", this);
            return;
        }

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
            {
                pages[i].SetActive(false);
            }

            // Uma aba pode ter sido removida da cena durante o Play Mode.
            // Unity trata referências a objetos destruídos como null.
            if (tabImages != null && i < tabImages.Length && tabImages[i] != null)
            {
                tabImages[i].color = Color.grey;
            }
        }

        if (pages[tabNo] != null)
        {
            pages[tabNo].SetActive(true);
        }

        if (tabImages != null && tabNo < tabImages.Length && tabImages[tabNo] != null)
        {
            tabImages[tabNo].color = Color.white;
        }

        CurrentTabIndex = tabNo;
        TabChanged?.Invoke(tabNo);
    }
}
