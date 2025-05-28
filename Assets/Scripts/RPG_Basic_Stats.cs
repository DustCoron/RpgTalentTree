using UnityEngine;

[System.Serializable]
public class RPG_Basic_Stats
{
    public int strength;
    public int dexterity;
    public int intelligence;
    public int maxhp;
    public int maxmp;
    public int armour;
    public int evasion;
    public RPG_EnergyShieldStats energyShield = new RPG_EnergyShieldStats();

    public static RPG_Basic_Stats operator +(RPG_Basic_Stats a, RPG_Basic_Stats b)
    {
        var result = new RPG_Basic_Stats();
        result.strength = a.strength + b.strength;
        result.dexterity = a.dexterity + b.dexterity;
        result.intelligence = a.intelligence + b.intelligence;

        result.maxhp = a.maxhp + b.maxhp;
        result.maxmp = a.maxmp + b.maxmp;

        result.armour = a.armour + b.armour;
        result.evasion = a.evasion + b.evasion;
        result.energyShield.maximumEnergyShield = a.energyShield.maximumEnergyShield + b.energyShield.maximumEnergyShield;
        result.energyShield.regeneration = a.energyShield.regeneration + b.energyShield.regeneration;
        result.energyShield.rechargeRate = a.energyShield.rechargeRate + b.energyShield.rechargeRate;
        result.energyShield.rechargeDelay = Mathf.Max(a.energyShield.rechargeDelay, b.energyShield.rechargeDelay);
        return result;
    }

    public override string ToString()
    {
        return $"STR:{strength} DEX:{dexterity} INT:{intelligence} " +
               $"Life:{maxhp} Mana:{maxmp} Armour:{armour} " +
               $"Evasion:{evasion} ES:{energyShield}";
    }
}
