using UnityEngine;

[System.Serializable]
public class RPG_Basic_Stats
{
    public int strength;
    public int dexterity;
    public int intelligence;
    public int maximumLife;
    public int maximumMana;
    public int armour;
    public int evasion;
    public int energyShield;

    public static RPG_Basic_Stats operator +(RPG_Basic_Stats a, RPG_Basic_Stats b)
    {
        var result = new RPG_Basic_Stats();
        result.strength = a.strength + b.strength;
        result.dexterity = a.dexterity + b.dexterity;
        result.intelligence = a.intelligence + b.intelligence;
        result.maximumLife = a.maximumLife + b.maximumLife;
        result.maximumMana = a.maximumMana + b.maximumMana;
        result.armour = a.armour + b.armour;
        result.evasion = a.evasion + b.evasion;
        result.energyShield = a.energyShield + b.energyShield;
        return result;
    }

    public override string ToString()
    {
        return $"STR:{strength} DEX:{dexterity} INT:{intelligence} " +
               $"Life:{maximumLife} Mana:{maximumMana} Armour:{armour} " +
               $"Evasion:{evasion} ES:{energyShield}";
    }
}
