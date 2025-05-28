using System;
using System.Collections.Generic;
using UnityEngine;

public enum RPG_DebuffType
{
    Bleed,    // Physical
    Ignite,   // Fire
    Chill,    // Cold
    Freeze,   // Cold (stronger)
    Shock,    // Lightning
    Poison    // Chaos
}

[Serializable]
public class RPG_Debuff
{
    public RPG_DebuffType debuffType;
    public float duration; // seconds
    public float strength; // effect magnitude (e.g., % slow, % more damage taken)
    public int maxStacks = 1;
    public int currentStacks = 1;
    public GameObject source; // who applied it
    public float timeApplied;

    public RPG_Debuff(RPG_DebuffType type, float duration, float strength, int maxStacks = 1, GameObject source = null)
    {
        this.debuffType = type;
        this.duration = duration;
        this.strength = strength;
        this.maxStacks = maxStacks;
        this.currentStacks = 1;
        this.source = source;
        this.timeApplied = Time.time;
    }

    public bool IsExpired() => Time.time > timeApplied + duration;
}

public static class RPG_DebuffDatabase
{
    // Maps damage type to default debuff type and parameters (inspired by Path of Exile)
    public static readonly Dictionary<RPG_DamageTypes, DebuffInfo> DamageTypeToDebuff = new()
    {
        { RPG_DamageTypes.Physical, new DebuffInfo(RPG_DebuffType.Bleed, 5f, 0.6f, 1) }, // Bleed: 60% base damage over 5s
        { RPG_DamageTypes.Fire,     new DebuffInfo(RPG_DebuffType.Ignite, 4f, 0.5f, 1) }, // Ignite: 50% base damage over 4s
        { RPG_DamageTypes.Cold,     new DebuffInfo(RPG_DebuffType.Chill, 2f, 0.3f, 1) },  // Chill: 30% slow for 2s
        { RPG_DamageTypes.Lightning,new DebuffInfo(RPG_DebuffType.Shock, 2f, 0.2f, 1) },  // Shock: 20% increased damage taken for 2s
        { RPG_DamageTypes.Chaos,    new DebuffInfo(RPG_DebuffType.Poison, 8f, 0.2f, 10) } // Poison: 20% base damage over 8s, stacks
    };

    public struct DebuffInfo
    {
        public RPG_DebuffType type;
        public float duration;
        public float strength;
        public int maxStacks;
        public DebuffInfo(RPG_DebuffType type, float duration, float strength, int maxStacks)
        {
            this.type = type;
            this.duration = duration;
            this.strength = strength;
            this.maxStacks = maxStacks;
        }
    }
}

// Example usage:
// var info = RPG_DebuffDatabase.DamageTypeToDebuff[damageType];
// var debuff = new RPG_Debuff(info.type, info.duration, info.strength, info.maxStacks, attacker);
// target.ApplyDebuff(debuff); // You'd implement ApplyDebuff on your character/monster class. 