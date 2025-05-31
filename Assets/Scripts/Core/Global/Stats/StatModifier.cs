using UnityEngine;
using System;

[Serializable]
public class StatModifier
{
    [Header("Modifier Info")]
    public StatType statType;
    public ModifierType modifierType;
    public float value;
    public string source; // Equipment, skill, buff, etc.
    public bool isTemporary;
    public float duration;
    
    [Header("Conditional")]
    public bool hasCondition;
    public ModifierCondition condition;
    
    private float remainingDuration;
    
    public enum ModifierType
    {
        Flat,           // +10 to stat
        PercentIncrease, // 20% increased stat
        PercentMore,    // 20% more stat (multiplicative)
        PercentLess     // 20% less stat (multiplicative)
    }
    
    public StatModifier(StatType stat, ModifierType type, float val, string src = "")
    {
        statType = stat;
        modifierType = type;
        value = val;
        source = src;
        isTemporary = false;
        duration = 0f;
        hasCondition = false;
    }
    
    public StatModifier(StatType stat, ModifierType type, float val, float dur, string src = "")
    {
        statType = stat;
        modifierType = type;
        value = val;
        source = src;
        isTemporary = true;
        duration = dur;
        remainingDuration = dur;
        hasCondition = false;
    }
    
    public bool IsExpired => isTemporary && remainingDuration <= 0f;
    
    public void UpdateDuration(float deltaTime)
    {
        if (isTemporary)
        {
            remainingDuration -= deltaTime;
        }
    }
    
    public bool IsActive(GameObject target)
    {
        if (!hasCondition) return true;
        return condition.IsConditionMet(target);
    }
    
    public float GetRemainingDuration() => remainingDuration;
    
    public StatModifier Clone()
    {
        var clone = new StatModifier(statType, modifierType, value, source);
        clone.isTemporary = isTemporary;
        clone.duration = duration;
        clone.remainingDuration = remainingDuration;
        clone.hasCondition = hasCondition;
        clone.condition = condition;
        return clone;
    }
} 