// File: Assets/Scripts/Stats/StatType.cs
public enum StatType
{
    // Core Attributes
    Strength,
    Dexterity,
    Intelligence,
    Vitality,
    
    // Offensive Stats
    PhysicalDamage,
    ElementalDamage,
    ChaosDamage,
    CriticalChance,
    CriticalMultiplier,
    AccuracyRating,
    AttackSpeed,
    CastSpeed,
    
    // Defensive Stats
    Life,
    Mana,
    EnergyShield,
    Armour,
    Evasion,
    BlockChance,
    SpellBlock,
    
    // Resistances
    FireResistance,
    ColdResistance,
    LightningResistance,
    ChaosResistance,
    
    // Regeneration & Leech
    LifeRegeneration,
    ManaRegeneration,
    EnergyShieldRegeneration,
    LifeLeech,
    ManaLeech,
    
    // Movement
    MovementSpeed,
    
    // Utility
    ItemRarity,
    ItemQuantity,
    ExperienceGain,
    
    // Spell Properties (from deleted RPG_SpellStats)
    SpellDamage,
    SpellCooldownReduction,
    ManaCostReduction,
    CastTimeReduction,
    
    // Projectile Properties (from deleted RPG_ProjectileStats)
    ProjectileSpeed,
    ProjectileLifetime,
    ProjectilePierce,
    ProjectileBounce,
    ProjectileChain,
    
    // Minion Properties (from deleted RPG_MinionStats)
    MinionLife,
    MinionDamage,
    MinionAttackSpeed,
    MinionMovementSpeed,
    MinionArmour,
    MinionEvasion,
    MinionEnergyShield
} 