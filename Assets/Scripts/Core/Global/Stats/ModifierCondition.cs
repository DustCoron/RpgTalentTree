using UnityEngine;
using System;

[Serializable]
public class ModifierCondition
{
    public ConditionType conditionType;
    public float threshold;
    public StatType referenceStat;
    
    public enum ConditionType
    {
        None,
        LowLife,        // When life < 35%
        FullLife,       // When life = 100%
        LowMana,        // When mana < 25%
        FullMana,       // When mana = 100%
        RecentlyHit,    // Within last 4 seconds
        Stationary,     // Not moved for 1 second
        Moving,         // Currently moving
        StatAbove,      // Referenced stat above threshold
        StatBelow       // Referenced stat below threshold
    }
    
    public bool IsConditionMet(GameObject target)
    {
        var combatEntity = target.GetComponent<AdvancedCombatEntity>();
        if (combatEntity == null) return false;
        
        switch (conditionType)
        {
            case ConditionType.None:
                return true;
                
            case ConditionType.LowLife:
                return combatEntity.GetLifePercentage() < 0.35f;
                
            case ConditionType.FullLife:
                return combatEntity.GetLifePercentage() >= 1.0f;
                
            case ConditionType.LowMana:
                return combatEntity.GetManaPercentage() < 0.25f;
                
            case ConditionType.FullMana:
                return combatEntity.GetManaPercentage() >= 1.0f;
                
            case ConditionType.RecentlyHit:
                return combatEntity.TimeSinceLastHit() < 4.0f;
                
            case ConditionType.Stationary:
                return combatEntity.GetTimeSinceLastMove() > 1.0f;
                
            case ConditionType.Moving:
                return combatEntity.IsMoving();
                
            case ConditionType.StatAbove:
                return combatEntity.GetStat(referenceStat) > threshold;
                
            case ConditionType.StatBelow:
                return combatEntity.GetStat(referenceStat) < threshold;
                
            default:
                return false;
        }
    }
} 