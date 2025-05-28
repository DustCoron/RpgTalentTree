using UnityEngine;

[System.Serializable]
public class RPG_LifeStats
{
    public int maximumLife;
    public float regeneration;
    public float rechargeRate;
    public float rechargeDelay;

    public override string ToString()
    {
        return $"Life:{maximumLife} Regen:{regeneration}/s Recharge:{rechargeRate}/s Delay:{rechargeDelay}s";
    }
}
