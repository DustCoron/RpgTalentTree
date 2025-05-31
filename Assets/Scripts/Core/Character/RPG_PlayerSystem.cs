using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// === БАЗОВЫЙ КЛАСС ИГРОКА ===
public abstract class PlayerClass
{
    public abstract string ClassName { get; }
    public abstract string Description { get; }
    public abstract ClassArchetype Archetype { get; }
    
    // Базовые характеристики класса, которые будут применены к AdvancedStatSystem
    public abstract Dictionary<StatType, float> GetBaseStats();
    
    // Стартовые модификаторы класса
    public virtual List<StatModifier> GetStartingModifiers()
    {
        return new List<StatModifier>();
    }
    
    // Применить характеристики класса к системе статов
    public virtual void ApplyToStatSystem(AdvancedStatSystem statSystem)
    {
        var baseStats = GetBaseStats();
        foreach (var stat in baseStats)
        {
            statSystem.SetBaseStat(stat.Key, stat.Value);
        }
        
        var modifiers = GetStartingModifiers();
        foreach (var modifier in modifiers)
        {
            statSystem.AddModifier(modifier);
        }
    }
}

public enum ClassArchetype
{
    Warrior,    // Ближний бой, физический урон
    Mage,       // Магия, элементальный урон
    Hybrid,     // Смешанный стиль
    Support,    // Поддержка, лечение
    Summoner    // Призыватель миньонов
}

// === КЛАССЫ ИГРОКА С ИНТЕГРАЦИЕЙ НОВОЙ СИСТЕМЫ ===
public class PlayerClass_Paladin : PlayerClass
{
    public override string ClassName => "Paladin";
    public override string Description => "A holy warrior, blending sword and magic for righteous vengeance.";
    public override ClassArchetype Archetype => ClassArchetype.Hybrid;
    
    public override Dictionary<StatType, float> GetBaseStats()
    {
        return new Dictionary<StatType, float>
        {
            { StatType.Strength, 15 },
            { StatType.Dexterity, 8 },
            { StatType.Intelligence, 12 },
            { StatType.Vitality, 15 },
            { StatType.Life, 120 },
            { StatType.Mana, 80 },
            { StatType.Armour, 20 },
            { StatType.FireResistance, 10 },
            { StatType.PhysicalDamage, 15 }
        };
    }
    
    public override List<StatModifier> GetStartingModifiers()
    {
        return new List<StatModifier>
        {
            new StatModifier(StatType.LifeRegeneration, StatModifier.ModifierType.Flat, 2f, "Paladin Blessing"),
            new StatModifier(StatType.BlockChance, StatModifier.ModifierType.Flat, 10f, "Shield Training")
        };
    }
}

public class PlayerClass_Mage : PlayerClass
{
    public override string ClassName => "Mage";
    public override string Description => "Master of the arcane arts, casting destructive and utility spells.";
    public override ClassArchetype Archetype => ClassArchetype.Mage;
    
    public override Dictionary<StatType, float> GetBaseStats()
    {
        return new Dictionary<StatType, float>
        {
            { StatType.Strength, 6 },
            { StatType.Dexterity, 10 },
            { StatType.Intelligence, 18 },
            { StatType.Vitality, 8 },
            { StatType.Life, 80 },
            { StatType.Mana, 150 },
            { StatType.EnergyShield, 50 },
            { StatType.ElementalDamage, 25 },
            { StatType.SpellDamage, 20 }
        };
    }
    
    public override List<StatModifier> GetStartingModifiers()
    {
        return new List<StatModifier>
        {
            new StatModifier(StatType.ManaRegeneration, StatModifier.ModifierType.Flat, 5f, "Arcane Knowledge"),
            new StatModifier(StatType.CastSpeed, StatModifier.ModifierType.PercentIncrease, 15f, "Spell Mastery")
        };
    }
}

public class PlayerClass_Necromancer : PlayerClass
{
    public override string ClassName => "Necromancer";
    public override string Description => "Manipulator of life and death, commanding undead minions.";
    public override ClassArchetype Archetype => ClassArchetype.Summoner;
    
    public override Dictionary<StatType, float> GetBaseStats()
    {
        return new Dictionary<StatType, float>
        {
            { StatType.Strength, 7 },
            { StatType.Dexterity, 9 },
            { StatType.Intelligence, 16 },
            { StatType.Vitality, 10 },
            { StatType.Life, 90 },
            { StatType.Mana, 130 },
            { StatType.ChaosDamage, 20 },
            { StatType.MinionLife, 50 },
            { StatType.MinionDamage, 25 }
        };
    }
    
    public override List<StatModifier> GetStartingModifiers()
    {
        return new List<StatModifier>
        {
            new StatModifier(StatType.LifeLeech, StatModifier.ModifierType.Flat, 2f, "Death Magic"),
            new StatModifier(StatType.ChaosResistance, StatModifier.ModifierType.Flat, 20f, "Undead Affinity")
        };
    }
}

public class PlayerClass_Warrior : PlayerClass
{
    public override string ClassName => "Warrior";
    public override string Description => "Expert in melee combat, strong and resilient on the battlefield.";
    public override ClassArchetype Archetype => ClassArchetype.Warrior;
    
    public override Dictionary<StatType, float> GetBaseStats()
    {
        return new Dictionary<StatType, float>
        {
            { StatType.Strength, 18 },
            { StatType.Dexterity, 12 },
            { StatType.Intelligence, 6 },
            { StatType.Vitality, 16 },
            { StatType.Life, 140 },
            { StatType.Mana, 40 },
            { StatType.Armour, 30 },
            { StatType.PhysicalDamage, 25 },
            { StatType.AttackSpeed, 10 }
        };
    }
    
    public override List<StatModifier> GetStartingModifiers()
    {
        return new List<StatModifier>
        {
            new StatModifier(StatType.CriticalChance, StatModifier.ModifierType.Flat, 5f, "Combat Training"),
            new StatModifier(StatType.AccuracyRating, StatModifier.ModifierType.Flat, 20f, "Weapon Mastery")
        };
    }
}

public class PlayerClass_Druid : PlayerClass
{
    public override string ClassName => "Druid";
    public override string Description => "Nature's shapeshifter, healer, and elemental caster.";
    public override ClassArchetype Archetype => ClassArchetype.Support;
    
    public override Dictionary<StatType, float> GetBaseStats()
    {
        return new Dictionary<StatType, float>
        {
            { StatType.Strength, 10 },
            { StatType.Dexterity, 12 },
            { StatType.Intelligence, 14 },
            { StatType.Vitality, 14 },
            { StatType.Life, 110 },
            { StatType.Mana, 100 },
            { StatType.ElementalDamage, 15 },
            { StatType.LifeRegeneration, 3 },
            { StatType.ManaRegeneration, 3 }
        };
    }
    
    public override List<StatModifier> GetStartingModifiers()
    {
        return new List<StatModifier>
        {
            new StatModifier(StatType.FireResistance, StatModifier.ModifierType.Flat, 15f, "Nature's Protection"),
            new StatModifier(StatType.ColdResistance, StatModifier.ModifierType.Flat, 15f, "Nature's Protection"),
            new StatModifier(StatType.MovementSpeed, StatModifier.ModifierType.PercentIncrease, 10f, "Wild Movement")
        };
    }
}

public class PlayerClass_Elementalist : PlayerClass
{
    public override string ClassName => "Elementalist";
    public override string Description => "Harnesses elemental forces for attack and defense.";
    public override ClassArchetype Archetype => ClassArchetype.Mage;
    
    public override Dictionary<StatType, float> GetBaseStats()
    {
        return new Dictionary<StatType, float>
        {
            { StatType.Strength, 7 },
            { StatType.Dexterity, 11 },
            { StatType.Intelligence, 17 },
            { StatType.Vitality, 9 },
            { StatType.Life, 85 },
            { StatType.Mana, 140 },
            { StatType.ElementalDamage, 30 },
            { StatType.SpellDamage, 15 }
        };
    }
    
    public override List<StatModifier> GetStartingModifiers()
    {
        return new List<StatModifier>
        {
            new StatModifier(StatType.FireResistance, StatModifier.ModifierType.Flat, 25f, "Elemental Mastery"),
            new StatModifier(StatType.ColdResistance, StatModifier.ModifierType.Flat, 25f, "Elemental Mastery"),
            new StatModifier(StatType.LightningResistance, StatModifier.ModifierType.Flat, 25f, "Elemental Mastery")
        };
    }
}

public class PlayerClass_Ranger : PlayerClass
{
    public override string ClassName => "Ranger";
    public override string Description => "Skilled with bows and traps, expert at ranged combat.";
    public override ClassArchetype Archetype => ClassArchetype.Warrior;
    
    public override Dictionary<StatType, float> GetBaseStats()
    {
        return new Dictionary<StatType, float>
        {
            { StatType.Strength, 10 },
            { StatType.Dexterity, 18 },
            { StatType.Intelligence, 8 },
            { StatType.Vitality, 12 },
            { StatType.Life, 100 },
            { StatType.Mana, 60 },
            { StatType.Evasion, 25 },
            { StatType.PhysicalDamage, 20 },
            { StatType.ProjectileSpeed, 15 }
        };
    }
    
    public override List<StatModifier> GetStartingModifiers()
    {
        return new List<StatModifier>
        {
            new StatModifier(StatType.CriticalChance, StatModifier.ModifierType.Flat, 8f, "Precise Shot"),
            new StatModifier(StatType.AttackSpeed, StatModifier.ModifierType.PercentIncrease, 20f, "Quick Draw"),
            new StatModifier(StatType.MovementSpeed, StatModifier.ModifierType.PercentIncrease, 15f, "Agility")
        };
    }
}

public class PlayerClass_Rogue : PlayerClass
{
    public override string ClassName => "Rogue";
    public override string Description => "Stealthy assassin, master of daggers and deception.";
    public override ClassArchetype Archetype => ClassArchetype.Warrior;
    
    public override Dictionary<StatType, float> GetBaseStats()
    {
        return new Dictionary<StatType, float>
        {
            { StatType.Strength, 11 },
            { StatType.Dexterity, 17 },
            { StatType.Intelligence, 9 },
            { StatType.Vitality, 10 },
            { StatType.Life, 95 },
            { StatType.Mana, 50 },
            { StatType.Evasion, 30 },
            { StatType.PhysicalDamage, 18 },
            { StatType.CriticalChance, 12 }
        };
    }
    
    public override List<StatModifier> GetStartingModifiers()
    {
        return new List<StatModifier>
        {
            new StatModifier(StatType.CriticalMultiplier, StatModifier.ModifierType.Flat, 25f, "Assassinate"),
            new StatModifier(StatType.AttackSpeed, StatModifier.ModifierType.PercentIncrease, 25f, "Swift Strikes"),
            new StatModifier(StatType.MovementSpeed, StatModifier.ModifierType.PercentIncrease, 20f, "Shadow Step")
        };
    }
}

public class PlayerClass_Priest : PlayerClass
{
    public override string ClassName => "Priest";
    public override string Description => "Healer and protector, channels holy energies for support.";
    public override ClassArchetype Archetype => ClassArchetype.Support;
    
    public override Dictionary<StatType, float> GetBaseStats()
    {
        return new Dictionary<StatType, float>
        {
            { StatType.Strength, 8 },
            { StatType.Dexterity, 9 },
            { StatType.Intelligence, 16 },
            { StatType.Vitality, 13 },
            { StatType.Life, 105 },
            { StatType.Mana, 120 },
            { StatType.EnergyShield, 30 },
            { StatType.LifeRegeneration, 5 },
            { StatType.ManaRegeneration, 4 }
        };
    }
    
    public override List<StatModifier> GetStartingModifiers()
    {
        return new List<StatModifier>
        {
            new StatModifier(StatType.SpellBlock, StatModifier.ModifierType.Flat, 15f, "Divine Protection"),
            new StatModifier(StatType.ManaCostReduction, StatModifier.ModifierType.Flat, 10f, "Divine Efficiency"),
            new StatModifier(StatType.ChaosResistance, StatModifier.ModifierType.Flat, 30f, "Holy Aura")
        };
    }
}

public class PlayerClass_Berserker : PlayerClass
{
    public override string ClassName => "Berserker";
    public override string Description => "Unleashes raw rage for devastating attacks at the cost of defense.";
    public override ClassArchetype Archetype => ClassArchetype.Warrior;
    
    public override Dictionary<StatType, float> GetBaseStats()
    {
        return new Dictionary<StatType, float>
        {
            { StatType.Strength, 20 },
            { StatType.Dexterity, 14 },
            { StatType.Intelligence, 4 },
            { StatType.Vitality, 18 },
            { StatType.Life, 160 },
            { StatType.Mana, 30 },
            { StatType.PhysicalDamage, 35 },
            { StatType.AttackSpeed, 15 }
        };
    }
    
    public override List<StatModifier> GetStartingModifiers()
    {
        return new List<StatModifier>
        {
            new StatModifier(StatType.CriticalChance, StatModifier.ModifierType.Flat, 10f, "Berserker Rage"),
            new StatModifier(StatType.CriticalMultiplier, StatModifier.ModifierType.Flat, 50f, "Devastating Blow"),
            new StatModifier(StatType.Armour, StatModifier.ModifierType.PercentLess, 20f, "Reckless Fighting"),
            new StatModifier(StatType.LifeLeech, StatModifier.ModifierType.Flat, 3f, "Blood Thirst")
        };
    }
} 