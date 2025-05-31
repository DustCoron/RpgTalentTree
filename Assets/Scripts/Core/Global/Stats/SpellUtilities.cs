// File: Assets/Scripts/Stats/SpellUtilities.cs
// Utility classes for working with spells using the new StatType system
using UnityEngine;

[System.Serializable]
public class SpellData
{
    [Header("Spell Info")]
    public string spellName;
    public RPG_DamageTypes damageType;
    
    [Header("Base Values")]
    public int baseDamage;
    public float baseCooldown;
    public int baseManaCost;
    public float baseCastTime;
    
    [Header("Projectile Data")]
    public bool isProjectile = false;
    public ProjectileData projectileData = new ProjectileData();
    
    public float GetEffectiveDamage(AdvancedStatSystem statSystem)
    {
        float damage = baseDamage;
        
        // Apply spell damage modifiers
        damage += statSystem.GetStat(StatType.SpellDamage);
        
        // Apply damage type specific modifiers
        switch (damageType)
        {
            case RPG_DamageTypes.Physical:
                damage += statSystem.GetStat(StatType.PhysicalDamage);
                break;
            case RPG_DamageTypes.Fire:
            case RPG_DamageTypes.Cold:
            case RPG_DamageTypes.Lightning:
                damage += statSystem.GetStat(StatType.ElementalDamage);
                break;
            case RPG_DamageTypes.Chaos:
                damage += AdvancedStatSystem.Instance.GetStatValue(RPG_StatType.ChaosDamage);
                break;
        }
        
        return damage;
    }
    
    public float GetEffectiveCooldown(AdvancedStatSystem statSystem)
    {
        float cooldown = baseCooldown;
        float reduction = statSystem.GetStat(StatType.SpellCooldownReduction);
        return cooldown * (1f - reduction / 100f);
    }
    
    public int GetEffectiveManaCost(AdvancedStatSystem statSystem)
    {
        float manaCost = baseManaCost;
        float reduction = statSystem.GetStat(StatType.ManaCostReduction);
        return Mathf.RoundToInt(manaCost * (1f - reduction / 100f));
    }
    
    public float GetEffectiveCastTime(AdvancedStatSystem statSystem)
    {
        float castTime = baseCastTime;
        float reduction = statSystem.GetStat(StatType.CastTimeReduction);
        float speedBonus = statSystem.GetStat(StatType.CastSpeed);
        
        castTime *= (1f - reduction / 100f); // Reduction
        castTime /= (1f + speedBonus / 100f); // Speed increase
        
        return Mathf.Max(0.1f, castTime); // Minimum cast time
    }
}

[System.Serializable]
public class ProjectileData
{
    [Header("Movement")]
    public float baseSpeed = 10f;
    public float baseLifetime = 5f;
    
    [Header("Interaction")]
    public int basePierce = 0;
    public int baseBounce = 0;
    public int baseChain = 0;
    
    public float GetEffectiveSpeed(AdvancedStatSystem statSystem)
    {
        return baseSpeed + statSystem.GetStat(StatType.ProjectileSpeed);
    }
    
    public float GetEffectiveLifetime(AdvancedStatSystem statSystem)
    {
        return baseLifetime + statSystem.GetStat(StatType.ProjectileLifetime);
    }
    
    public int GetEffectivePierce(AdvancedStatSystem statSystem)
    {
        return basePierce + Mathf.RoundToInt(statSystem.GetStat(StatType.ProjectilePierce));
    }
    
    public int GetEffectiveBounce(AdvancedStatSystem statSystem)
    {
        return baseBounce + Mathf.RoundToInt(statSystem.GetStat(StatType.ProjectileBounce));
    }
    
    public int GetEffectiveChain(AdvancedStatSystem statSystem)
    {
        return baseChain + Mathf.RoundToInt(statSystem.GetStat(StatType.ProjectileChain));
    }
}

[System.Serializable]
public class MinionData
{
    [Header("Base Stats")]
    public int baseLife = 100;
    public int baseDamage = 10;
    
    [Header("Combat")]
    public float baseAttackSpeed = 1f;
    public float baseMovementSpeed = 3f;
    
    [Header("Defenses")]
    public int baseArmour = 0;
    public int baseEvasion = 0;
    public int baseEnergyShield = 0;
    
    public int GetEffectiveLife(AdvancedStatSystem statSystem)
    {
        return baseLife + Mathf.RoundToInt(statSystem.GetStat(StatType.MinionLife));
    }
    
    public int GetEffectiveDamage(AdvancedStatSystem statSystem)
    {
        return baseDamage + Mathf.RoundToInt(statSystem.GetStat(StatType.MinionDamage));
    }
    
    public float GetEffectiveAttackSpeed(AdvancedStatSystem statSystem)
    {
        float speed = baseAttackSpeed;
        float bonus = statSystem.GetStat(StatType.MinionAttackSpeed);
        return speed * (1f + bonus / 100f);
    }
    
    public float GetEffectiveMovementSpeed(AdvancedStatSystem statSystem)
    {
        float speed = baseMovementSpeed;
        float bonus = statSystem.GetStat(StatType.MinionMovementSpeed);
        return speed * (1f + bonus / 100f);
    }
    
    public int GetEffectiveArmour(AdvancedStatSystem statSystem)
    {
        return baseArmour + Mathf.RoundToInt(statSystem.GetStat(StatType.MinionArmour));
    }
    
    public int GetEffectiveEvasion(AdvancedStatSystem statSystem)
    {
        return baseEvasion + Mathf.RoundToInt(statSystem.GetStat(StatType.MinionEvasion));
    }
    
    public int GetEffectiveEnergyShield(AdvancedStatSystem statSystem)
    {
        return baseEnergyShield + Mathf.RoundToInt(statSystem.GetStat(StatType.MinionEnergyShield));
    }
} 