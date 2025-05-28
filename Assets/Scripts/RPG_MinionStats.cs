using UnityEngine;

[System.Serializable]
public class RPG_MinionStats
{
    public int maxhp;

    public int damage;
    public float attackSpeed;
    public float movementSpeed;
    public int armour;
    public int evasion;
    public int energyShield;

    public override string ToString()
    {
        return $"Life:{maxhp} Damage:{damage} AS:{attackSpeed} MS:{movementSpeed} Armour:{armour} Evasion:{evasion} ES:{energyShield}";

    }
}
