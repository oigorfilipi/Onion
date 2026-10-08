using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Acompanha qual dos dois slots de poder está selecionado e alterna com Tab quando ambos estão ocupados.
/// </summary>
public class PowerController : MonoBehaviour
{
    private PlayerControls playerControls;
    private EquipmentController equipmentController;
    private int activePowerIndex;

    public event Action<Item> ActivePowerChanged;

    public EquipmentSlot ActivePowerSlot
    {
        get
        {
            EquipmentSlot[] powerSlots = equipmentController != null
                ? equipmentController.GetPowerSlots()
                : Array.Empty<EquipmentSlot>();

            return activePowerIndex < powerSlots.Length ? powerSlots[activePowerIndex] : null;
        }
    }

    public Item ActivePowerItem
    {
        get
        {
            GameObject itemObject = ActivePowerSlot != null ? ActivePowerSlot.CurrentItem : null;
            return itemObject != null ? itemObject.GetComponent<Item>() : null;
        }
    }

    private void Awake()
    {
        equipmentController = GetComponent<EquipmentController>();
        playerControls = new PlayerControls();
        playerControls.Equipment.SwitchPower.performed += OnSwitchPower;

        if (equipmentController != null)
        {
            equipmentController.EquipmentChanged += RefreshActivePower;
        }
    }

    private void OnEnable()
    {
        playerControls?.Equipment.Enable();
    }

    private void Start()
    {
        RefreshActivePower();
    }

    private void OnDisable()
    {
        playerControls?.Equipment.Disable();
    }

    private void OnDestroy()
    {
        if (equipmentController != null)
        {
            equipmentController.EquipmentChanged -= RefreshActivePower;
        }

        if (playerControls != null)
        {
            playerControls.Equipment.SwitchPower.performed -= OnSwitchPower;
            playerControls.Dispose();
        }
    }

    public bool RemoveAbsorbablePower(EquipmentSlot slot)
    {
        if (equipmentController == null || !equipmentController.RemoveAbsorbablePower(slot))
        {
            return false;
        }

        RefreshActivePower();
        return true;
    }

    // Tab não muda nada com menos de dois poderes; com ambos, alterna a seleção e notifica a UI.
    private void OnSwitchPower(InputAction.CallbackContext context)
    {
        if (Time.timeScale <= 0f || equipmentController == null)
        {
            return;
        }

        EquipmentSlot[] slots = equipmentController.GetPowerSlots();
        if (slots.Length < 2 || slots[0] == null || slots[1] == null ||
            slots[0].CurrentItem == null || slots[1].CurrentItem == null)
        {
            return;
        }

        activePowerIndex = 1 - activePowerIndex;
        ActivePowerChanged?.Invoke(ActivePowerItem);
    }

    private void RefreshActivePower()
    {
        if (equipmentController == null)
        {
            return;
        }

        EquipmentSlot[] slots = equipmentController.GetPowerSlots();
        if (slots.Length < 2)
        {
            activePowerIndex = 0;
            ActivePowerChanged?.Invoke(null);
            return;
        }

        bool firstHasItem = slots[0] != null && slots[0].CurrentItem != null;
        bool secondHasItem = slots[1] != null && slots[1].CurrentItem != null;

        if (!firstHasItem && secondHasItem)
        {
            activePowerIndex = 1;
        }
        else if (firstHasItem && !secondHasItem)
        {
            activePowerIndex = 0;
        }
        else if (!firstHasItem && !secondHasItem)
        {
            activePowerIndex = 0;
        }

        ActivePowerChanged?.Invoke(ActivePowerItem);
    }
}
