using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class AdvancedStatSystem : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] private Dictionary<StatType, float> baseStats = new Dictionary<StatType, float>();
    
    [Header("Modifiers")]
    [SerializeField] private List<StatModifier> activeModifiers = new List<StatModifier>();
    
    // Cached calculated stats for performance
    private Dictionary<StatType, float> cachedStats = new Dictionary<StatType, float>();
    private bool statsCacheDirty = true;
    
    // Events
    public Action<StatType, float, float> OnStatChanged; // stat, oldValue, newValue
    public Action<StatModifier> OnModifierAdded;
    public Action<StatModifier> OnModifierRemoved;
    
    private void Awake()
    {
        InitializeBaseStats();
    }
    
    private void Update()
    {
        UpdateTemporaryModifiers();
        
        if (statsCacheDirty)
        {
            RecalculateStats();
        }
    }
    
    private void InitializeBaseStats()
    {
        // Initialize with default values
        baseStats[StatType.Strength] = 10;
        baseStats[StatType.Dexterity] = 10;
        baseStats[StatType.Intelligence] = 10;
        baseStats[StatType.Vitality] = 10;
        
        // Defensive base stats
        baseStats[StatType.Life] = 100;
        baseStats[StatType.Mana] = 50;
        baseStats[StatType.EnergyShield] = 0;
        baseStats[StatType.Armour] = 0;
        baseStats[StatType.Evasion] = 0;
        
        // Resistances start at 0%
        baseStats[StatType.FireResistance] = 0;
        baseStats[StatType.ColdResistance] = 0;
        baseStats[StatType.LightningResistance] = 0;
        baseStats[StatType.ChaosResistance] = 0;
        
        // Accuracy and crit
        baseStats[StatType.AccuracyRating] = 100;
        baseStats[StatType.CriticalChance] = 5; // 5% base crit
        baseStats[StatType.CriticalMultiplier] = 150; // 150% crit multiplier
        
        // Movement
        baseStats[StatType.MovementSpeed] = 100; // 100% = normal speed
        
        MarkStatsCacheDirty();
    }
    
    private void UpdateTemporaryModifiers()
    {
        bool removedAny = false;
        
        for (int i = activeModifiers.Count - 1; i >= 0; i--)
        {
            var modifier = activeModifiers[i];
            modifier.UpdateDuration(Time.deltaTime);
            
            if (modifier.IsExpired)
            {
                activeModifiers.RemoveAt(i);
                OnModifierRemoved?.Invoke(modifier);
                removedAny = true;
            }
        }
        
        if (removedAny)
        {
            MarkStatsCacheDirty();
        }
    }
    
    public void AddModifier(StatModifier modifier)
    {
        activeModifiers.Add(modifier);
        OnModifierAdded?.Invoke(modifier);
        MarkStatsCacheDirty();
    }
    
    public void RemoveModifier(StatModifier modifier)
    {
        if (activeModifiers.Remove(modifier))
        {
            OnModifierRemoved?.Invoke(modifier);
            MarkStatsCacheDirty();
        }
    }
    
    public void RemoveModifiersFromSource(string source)
    {
        bool removedAny = false;
        
        for (int i = activeModifiers.Count - 1; i >= 0; i--)
        {
            if (activeModifiers[i].source == source)
            {
                var modifier = activeModifiers[i];
                activeModifiers.RemoveAt(i);
                OnModifierRemoved?.Invoke(modifier);
                removedAny = true;
            }
        }
        
        if (removedAny)
        {
            MarkStatsCacheDirty();
        }
    }
    
    public float GetStat(StatType statType)
    {
        if (statsCacheDirty)
        {
            RecalculateStats();
        }
        
        return cachedStats.ContainsKey(statType) ? cachedStats[statType] : 0f;
    }
    
    public float GetBaseStat(StatType statType)
    {
        return baseStats.ContainsKey(statType) ? baseStats[statType] : 0f;
    }
    
    public void SetBaseStat(StatType statType, float value)
    {
        float oldValue = GetStat(statType);
        baseStats[statType] = value;
        MarkStatsCacheDirty();
        
        // Trigger event after recalculation
        if (statsCacheDirty)
        {
            RecalculateStats();
        }
        
        float newValue = GetStat(statType);
        if (Math.Abs(oldValue - newValue) > 0.01f)
        {
            OnStatChanged?.Invoke(statType, oldValue, newValue);
        }
    }
    
    private void RecalculateStats()
    {
        cachedStats.Clear();
        
        // Calculate each stat type
        foreach (var baseStat in baseStats)
        {
            cachedStats[baseStat.Key] = CalculateStat(baseStat.Key);
        }
        
        // Calculate derived stats
        CalculateDerivedStats();
        
        statsCacheDirty = false;
    }
    
    private float CalculateStat(StatType statType)
    {
        float baseValue = GetBaseStat(statType);
        
        // Get all modifiers for this stat
        var relevantModifiers = activeModifiers
            .Where(m => m.statType == statType && m.IsActive(gameObject))
            .ToList();
        
        // Apply flat modifiers first
        float flatSum = relevantModifiers
            .Where(m => m.modifierType == StatModifier.ModifierType.Flat)
            .Sum(m => m.value);
        
        // Apply increased/reduced modifiers (additive)
        float increasedSum = relevantModifiers
            .Where(m => m.modifierType == StatModifier.ModifierType.PercentIncrease)
            .Sum(m => m.value);
        
        // Apply more/less modifiers (multiplicative)
        float moreMultiplier = 1f;
        var moreModifiers = relevantModifiers
            .Where(m => m.modifierType == StatModifier.ModifierType.PercentMore || 
                       m.modifierType == StatModifier.ModifierType.PercentLess);
        
        foreach (var modifier in moreModifiers)
        {
            float multiplier = modifier.modifierType == StatModifier.ModifierType.PercentMore
                ? (1f + modifier.value / 100f)
                : (1f - modifier.value / 100f);
            moreMultiplier *= multiplier;
        }
        
        // Calculate final value: (Base + Flat) * (1 + Increased/100) * More_Multipliers
        float result = (baseValue + flatSum) * (1f + increasedSum / 100f) * moreMultiplier;
        
        // Apply stat-specific constraints
        return ApplyStatConstraints(statType, result);
    }
    
    private float ApplyStatConstraints(StatType statType, float value)
    {
        switch (statType)
        {
            case StatType.CriticalChance:
                return Mathf.Clamp(value, 0f, 95f); // Max 95% crit chance
                
            case StatType.BlockChance:
            case StatType.SpellBlock:
                return Mathf.Clamp(value, 0f, 75f); // Max 75% block chance
                
            case StatType.FireResistance:
            case StatType.ColdResistance:
            case StatType.LightningResistance:
                return Mathf.Clamp(value, -100f, 90f); // Resistances: -100% to 90%
                
            case StatType.ChaosResistance:
                return Mathf.Clamp(value, -100f, 85f); // Chaos resistance slightly lower cap
                
            case StatType.MovementSpeed:
                return Mathf.Max(value, 10f); // Minimum 10% movement speed
                
            default:
                return Mathf.Max(value, 0f); // Most stats can't go below 0
        }
    }
    
    private void CalculateDerivedStats()
    {
        // Life = Base Life + (Vitality * 5) + modifiers
        if (!cachedStats.ContainsKey(StatType.Life))
        {
            float baseLife = GetBaseStat(StatType.Life);
            float vitalityBonus = GetStat(StatType.Vitality) * 5f;
            cachedStats[StatType.Life] = baseLife + vitalityBonus;
        }
        
        // Mana = Base Mana + (Intelligence * 2) + modifiers
        if (!cachedStats.ContainsKey(StatType.Mana))
        {
            float baseMana = GetBaseStat(StatType.Mana);
            float intelligenceBonus = GetStat(StatType.Intelligence) * 2f;
            cachedStats[StatType.Mana] = baseMana + intelligenceBonus;
        }
        
        // Physical Damage gets bonus from Strength
        if (cachedStats.ContainsKey(StatType.PhysicalDamage))
        {
            float strengthBonus = GetStat(StatType.Strength) * 0.2f; // 20% of strength as flat damage
            cachedStats[StatType.PhysicalDamage] += strengthBonus;
        }
        
        // Accuracy gets bonus from Dexterity
        if (cachedStats.ContainsKey(StatType.AccuracyRating))
        {
            float dexterityBonus = GetStat(StatType.Dexterity) * 2f;
            cachedStats[StatType.AccuracyRating] += dexterityBonus;
        }
    }
    
    private void MarkStatsCacheDirty()
    {
        statsCacheDirty = true;
    }
    
    // Utility methods
    public List<StatModifier> GetActiveModifiers() => new List<StatModifier>(activeModifiers);
    
    public List<StatModifier> GetModifiersForStat(StatType statType)
    {
        return activeModifiers.Where(m => m.statType == statType).ToList();
    }
    
    public Dictionary<StatType, float> GetAllStats()
    {
        if (statsCacheDirty)
        {
            RecalculateStats();
        }
        
        return new Dictionary<StatType, float>(cachedStats);
    }
} 