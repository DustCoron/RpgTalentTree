using UnityEngine;

[System.Serializable]
public class RPG_Basic_Stats
{
    public int strength;
    public int dexterity;
    public int intelligence;
    public RPG_LifeStats life = new RPG_LifeStats();
    public RPG_ManaStats mana = new RPG_ManaStats();
    public int armour;
    public int evasion;
    public RPG_EnergyShieldStats energyShield = new RPG_EnergyShieldStats();

    public static RPG_Basic_Stats operator +(RPG_Basic_Stats a, RPG_Basic_Stats b)
    {
        var result = new RPG_Basic_Stats();
        result.strength = a.strength + b.strength;
        result.dexterity = a.dexterity + b.dexterity;
        result.intelligence = a.intelligence + b.intelligence;
        result.life.maximumLife = a.life.maximumLife + b.life.maximumLife;
        result.life.regeneration = a.life.regeneration + b.life.regeneration;
        result.life.rechargeRate = a.life.rechargeRate + b.life.rechargeRate;
        result.life.rechargeDelay = Mathf.Max(a.life.rechargeDelay, b.life.rechargeDelay);

        result.mana.maximumMana = a.mana.maximumMana + b.mana.maximumMana;
        result.mana.regeneration = a.mana.regeneration + b.mana.regeneration;
        result.mana.rechargeRate = a.mana.rechargeRate + b.mana.rechargeRate;
        result.mana.rechargeDelay = Mathf.Max(a.mana.rechargeDelay, b.mana.rechargeDelay);
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
               $"{life} {mana} Armour:{armour} " +
               $"Evasion:{evasion} {energyShield}";
    }
}
