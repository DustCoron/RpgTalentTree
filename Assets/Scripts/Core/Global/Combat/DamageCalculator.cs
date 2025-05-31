using UnityEngine;
using System;

public static class DamageCalculator
{
    // Damage calculation similar to Path of Exile
    public static DamageResult CalculateDamage(DamageInput input)
    {
        var result = new DamageResult();
        
        // Calculate base damage
        float baseDamage = CalculateBaseDamage(input);
        
        // Apply critical strike
        bool isCritical = RollCriticalStrike(input.attacker);
        if (isCritical)
        {
            float critMultiplier = input.attacker.GetStat(StatType.CriticalMultiplier) / 100f;
            baseDamage *= critMultiplier;
        }
        
        // Check for hit/miss
        bool hits = RollAccuracy(input.attacker, input.defender);
        if (!hits)
        {
            result.missed = true;
            result.finalDamage = 0;
            return result;
        }
        
        // Apply damage mitigation
        float mitigatedDamage = ApplyDamageMitigation(baseDamage, input);
        
        // Check for block
        bool blocked = RollBlock(input.defender, input.damageType);
        if (blocked)
        {
            result.blocked = true;
            mitigatedDamage *= 0.5f; // Blocked damage is reduced by 50%
        }
        
        result.baseDamage = baseDamage;
        result.finalDamage = Mathf.RoundToInt(mitigatedDamage);
        result.wasCritical = isCritical;
        result.wasBlocked = blocked;
        
        return result;
    }
    
    private static float CalculateBaseDamage(DamageInput input)
    {
        float damage = 0f;
        
        switch (input.damageType)
        {
            case RPG_DamageTypes.Physical:
                damage = input.attacker.GetStat(StatType.PhysicalDamage);
                break;
                
            case RPG_DamageTypes.Fire:
            case RPG_DamageTypes.Cold:
            case RPG_DamageTypes.Lightning:
                damage = input.attacker.GetStat(StatType.ElementalDamage);
                break;
                
            case RPG_DamageTypes.Chaos:
                damage = input.attacker.GetStat(StatType.ChaosDamage);
                break;
        }
        
        // Add weapon damage if applicable
        damage += input.weaponDamage;
        
        // Random variance (±10%)
        float variance = UnityEngine.Random.Range(0.9f, 1.1f);
        damage *= variance;
        
        return damage;
    }
    
    private static bool RollCriticalStrike(AdvancedStatSystem attacker)
    {
        float critChance = attacker.GetStat(StatType.CriticalChance);
        return UnityEngine.Random.Range(0f, 100f) < critChance;
    }
    
    private static bool RollAccuracy(AdvancedStatSystem attacker, AdvancedStatSystem defender)
    {
        float accuracy = attacker.GetStat(StatType.AccuracyRating);
        float evasion = defender.GetStat(StatType.Evasion);
        
        // Accuracy formula: Accuracy / (Accuracy + Evasion/4)
        float hitChance = accuracy / (accuracy + evasion / 4f);
        hitChance = Mathf.Clamp(hitChance, 0.05f, 0.95f); // 5% min, 95% max
        
        return UnityEngine.Random.Range(0f, 1f) < hitChance;
    }
    
    private static bool RollBlock(AdvancedStatSystem defender, RPG_DamageTypes damageType)
    {
        float blockChance = 0f;
        
        if (damageType == RPG_DamageTypes.Physical)
        {
            blockChance = defender.GetStat(StatType.BlockChance);
        }
        else
        {
            blockChance = defender.GetStat(StatType.SpellBlock);
        }
        
        return UnityEngine.Random.Range(0f, 100f) < blockChance;
    }
    
    private static float ApplyDamageMitigation(float damage, DamageInput input)
    {
        float mitigatedDamage = damage;
        
        // Apply resistances for elemental damage
        if (input.damageType != RPG_DamageTypes.Physical)
        {
            float resistance = GetResistance(input.defender, input.damageType);
            mitigatedDamage *= (1f - resistance / 100f);
        }
        
        // Apply armour for physical damage
        if (input.damageType == RPG_DamageTypes.Physical)
        {
            float armour = input.defender.GetStat(StatType.Armour);
            mitigatedDamage = ApplyArmourReduction(mitigatedDamage, armour);
        }
        
        return mitigatedDamage;
    }
    
    private static float GetResistance(AdvancedStatSystem defender, RPG_DamageTypes damageType)
    {
        switch (damageType)
        {
            case RPG_DamageTypes.Fire:
                return defender.GetStat(StatType.FireResistance);
            case RPG_DamageTypes.Cold:
                return defender.GetStat(StatType.ColdResistance);
            case RPG_DamageTypes.Lightning:
                return defender.GetStat(StatType.LightningResistance);
            case RPG_DamageTypes.Chaos:
                return defender.GetStat(StatType.ChaosResistance);
            default:
                return 0f;
        }
    }
    
    private static float ApplyArmourReduction(float damage, float armour)
    {
        // Armour formula: Damage * Armour / (Armour + 5 * Damage)
        float reduction = armour / (armour + 5f * damage);
        return damage * (1f - reduction);
    }
} 