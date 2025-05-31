using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EquipmentAffix
{
    [Header("Affix Info")]
    public string affixName;
    public AffixType affixType;
    public AffixTier tier;
    
    [Header("Stat Modifiers")]
    public List<StatModifier> modifiers = new List<StatModifier>();
    
    [Header("Requirements")]
    public int itemLevelRequirement = 1;
    public List<EquipmentSlot> allowedSlots = new List<EquipmentSlot>();
    
    public enum AffixType
    {
        Prefix,  // "Heavy" Armor (+Armour)
        Suffix   // Armor "of the Bear" (+Life)
    }
    
    public enum AffixTier
    {
        T1 = 1, // Highest tier (best values)
        T2 = 2,
        T3 = 3,
        T4 = 4,
        T5 = 5,
        T6 = 6  // Lowest tier
    }
    
    public bool CanRollOnItem(EquipmentData equipment)
    {
        // Check item level requirement
        if (equipment.ItemLevel < itemLevelRequirement)
            return false;
        
        // Check if allowed on this equipment slot
        if (allowedSlots.Count > 0 && !allowedSlots.Contains(equipment.Slot))
            return false;
        
        return true;
    }
    
    public string GetAffixText()
    {
        if (modifiers.Count == 0) return affixName;
        
        string text = "";
        for (int i = 0; i < modifiers.Count; i++)
        {
            var modifier = modifiers[i];
            string valueText = FormatModifierValue(modifier);
            
            if (i > 0) text += "\n";
            text += $"{valueText} {GetStatDisplayName(modifier.statType)}";
        }
        
        return text;
    }
    
    private string FormatModifierValue(StatModifier modifier)
    {
        switch (modifier.modifierType)
        {
            case StatModifier.ModifierType.Flat:
                return modifier.value >= 0 ? $"+{modifier.value:F0}" : $"{modifier.value:F0}";
                
            case StatModifier.ModifierType.PercentIncrease:
                return $"{modifier.value:F0}% increased";
                
            case StatModifier.ModifierType.PercentMore:
                return $"{modifier.value:F0}% more";
                
            case StatModifier.ModifierType.PercentLess:
                return $"{modifier.value:F0}% less";
                
            default:
                return modifier.value.ToString("F0");
        }
    }
    
    private string GetStatDisplayName(StatType statType)
    {
        switch (statType)
        {
            case StatType.Life:
                return "to maximum Life";
            case StatType.Mana:
                return "to maximum Mana";
            case StatType.EnergyShield:
                return "to maximum Energy Shield";
            case StatType.Armour:
                return "Armour";
            case StatType.Evasion:
                return "Evasion Rating";
            case StatType.FireResistance:
                return "Fire Resistance";
            case StatType.ColdResistance:
                return "Cold Resistance";
            case StatType.LightningResistance:
                return "Lightning Resistance";
            case StatType.MovementSpeed:
                return "Movement Speed";
            default:
                return statType.ToString();
        }
    }
} 