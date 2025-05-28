using UnityEngine;

[System.Serializable]
public class RPG_ManaStats
{
    public int maximumMana;
    public float regeneration;
    public float rechargeRate;
    public float rechargeDelay;

    public override string ToString()
    {
        return $"Mana:{maximumMana} Regen:{regeneration}/s Recharge:{rechargeRate}/s Delay:{rechargeDelay}s";
    }
}
