using UnityEngine;

public sealed class PlayerWallet2D : MonoBehaviour
{
    [SerializeField, Min(0)] private int coins;

    public int Coins => coins;

    public void AddCoins(int amount)
    {
        if (amount > 0)
        {
            coins = Mathf.Max(0, coins + amount);
        }
    }

    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (coins < amount)
        {
            return false;
        }

        coins -= amount;
        return true;
    }
}
