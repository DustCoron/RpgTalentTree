using UnityEngine;

[System.Serializable]
public class RPG_SpellStats
{
    public int damage;
    public RPG_DamageTypes damageType;
    public float cooldown;
    public int manaCost;
    public float castTime;

    public override string ToString()
    {
        return $"Damage:{damage} Type:{damageType} CD:{cooldown}s Mana:{manaCost} Cast:{castTime}s";
    }
}
