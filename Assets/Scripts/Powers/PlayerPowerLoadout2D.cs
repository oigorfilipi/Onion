using System;
using System.Collections.Generic;
using UnityEngine;

public enum ContactPowerId
{
    None,
    SpoonMagnetism,
    RedGloveStrength
}

public enum AbsorptionPowerId
{
    None,
    PoisonApple
}

public sealed class PlayerPowerLoadout2D : MonoBehaviour
{
    [SerializeField] private List<ContactPowerId> ownedContactPowers = new List<ContactPowerId>();
    [SerializeField] private List<AbsorptionPowerId> ownedAbsorptionPowers = new List<AbsorptionPowerId>();
    [SerializeField] private ContactPowerId equippedContactPower;
    [SerializeField] private AbsorptionPowerId activeAbsorptionPower;

    public ContactPowerId EquippedContactPower => equippedContactPower;
    public AbsorptionPowerId ActiveAbsorptionPower => activeAbsorptionPower;
    public IReadOnlyList<ContactPowerId> OwnedContactPowers => ownedContactPowers;
    public IReadOnlyList<AbsorptionPowerId> OwnedAbsorptionPowers => ownedAbsorptionPowers;

    public event Action PowerLoadoutChanged;

    public bool AcquireContactPower(ContactPowerId power)
    {
        if (power == ContactPowerId.None || ownedContactPowers.Contains(power))
        {
            return false;
        }

        ownedContactPowers.Add(power);
        if (equippedContactPower == ContactPowerId.None)
        {
            equippedContactPower = power;
        }

        PowerLoadoutChanged?.Invoke();
        return true;
    }

    public bool EquipContactPower(ContactPowerId power)
    {
        if (power == ContactPowerId.None || !ownedContactPowers.Contains(power))
        {
            return false;
        }

        equippedContactPower = power;
        PowerLoadoutChanged?.Invoke();
        return true;
    }

    public void UnequipContactPower()
    {
        if (equippedContactPower == ContactPowerId.None)
        {
            return;
        }

        equippedContactPower = ContactPowerId.None;
        PowerLoadoutChanged?.Invoke();
    }

    public bool RemoveContactPower(ContactPowerId power)
    {
        if (!ownedContactPowers.Remove(power))
        {
            return false;
        }

        if (equippedContactPower == power)
        {
            equippedContactPower = ContactPowerId.None;
        }

        PowerLoadoutChanged?.Invoke();
        return true;
    }

    public void CycleContactPower()
    {
        if (ownedContactPowers.Count == 0)
        {
            UnequipContactPower();
            return;
        }

        int currentIndex = ownedContactPowers.IndexOf(equippedContactPower);
        if (equippedContactPower == ContactPowerId.None)
        {
            equippedContactPower = ownedContactPowers[0];
        }
        else if (currentIndex >= 0 && currentIndex < ownedContactPowers.Count - 1)
        {
            equippedContactPower = ownedContactPowers[currentIndex + 1];
        }
        else
        {
            equippedContactPower = ContactPowerId.None;
        }

        PowerLoadoutChanged?.Invoke();
    }

    public bool AcquireAbsorptionPower(AbsorptionPowerId power)
    {
        if (power == AbsorptionPowerId.None || ownedAbsorptionPowers.Contains(power))
        {
            return false;
        }

        ownedAbsorptionPowers.Add(power);
        if (activeAbsorptionPower == AbsorptionPowerId.None)
        {
            activeAbsorptionPower = power;
        }

        PowerLoadoutChanged?.Invoke();
        return true;
    }

    public bool RemoveAbsorptionPower(AbsorptionPowerId power)
    {
        if (!ownedAbsorptionPowers.Remove(power))
        {
            return false;
        }

        if (activeAbsorptionPower == power)
        {
            activeAbsorptionPower = AbsorptionPowerId.None;
        }

        PowerLoadoutChanged?.Invoke();
        return true;
    }
}
