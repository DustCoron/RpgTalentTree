using UnityEngine;

[System.Serializable]
public class RPG_EnergyShieldStats
{
    public int maximumEnergyShield;
    public float regeneration;
    public float rechargeRate;
    public float rechargeDelay;

    public override string ToString()
    {
        return $"ES:{maximumEnergyShield} Regen:{regeneration}/s Recharge:{rechargeRate}/s Delay:{rechargeDelay}s";
    }
}
