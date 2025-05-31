using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public enum EquipmentSlot
{
    Helmet,
    Armor,
    Boots,
    Gloves,
    Belt,
    Amulet,
    Ring,
    MainHand,
    OffHand,
    TwoHanded,
    Quiver
}

[System.Serializable]
public enum ItemRarity
{
    Normal,     // White
    Magic,      // Blue (1-2 affixes)
    Rare,       // Yellow (3-6 affixes)
    Unique,     // Dark Gold (fixed unique properties)
    Legendary   // Red (extremely rare, powerful effects)
}

[CreateAssetMenu(menuName = "RPG/Equipment/Equipment Data")]
public class EquipmentData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;
    [TextArea(2, 3)]
    public string description;
    public Sprite icon;
    
    [Header("Equipment Properties")]
    public EquipmentSlot slot;
    public ItemRarity rarity = ItemRarity.Normal;
    public int itemLevel = 1;
    public int requiredLevel = 1;
    public string itemClass = "Equipment"; // e.g., "Sword", "Heavy Armor", etc.
    
    [Header("Base Stats")]
    public List<StatModifier> baseModifiers = new List<StatModifier>();
    
    [Header("Affix Configuration")]
    public int maxPrefixes = 3;
    public int maxSuffixes = 3;
    public List<EquipmentAffix> fixedAffixes = new List<EquipmentAffix>(); // For unique items
    
    [Header("Requirements")]
    public int strengthRequirement;
    public int dexterityRequirement;
    public int intelligenceRequirement;
    
    // Runtime properties (set when item is generated)
    [System.NonSerialized]
    public List<EquipmentAffix> currentPrefixes = new List<EquipmentAffix>();
    [System.NonSerialized]
    public List<EquipmentAffix> currentSuffixes = new List<EquipmentAffix>();
    
    // Properties for equipment system
    public EquipmentSlot Slot => slot;
    public int ItemLevel => itemLevel;
    public ItemRarity Rarity => rarity;
    
    /// <summary>
    /// Get all stat modifiers from this equipment piece
    /// </summary>
    public List<StatModifier> GetAllModifiers()
    {
        var allModifiers = new List<StatModifier>();
        
        // Add base modifiers
        allModifiers.AddRange(baseModifiers);
        
        // Add prefix modifiers
        foreach (var prefix in currentPrefixes)
        {
            allModifiers.AddRange(prefix.modifiers);
        }
        
        // Add suffix modifiers
        foreach (var suffix in currentSuffixes)
        {
            allModifiers.AddRange(suffix.modifiers);
        }
        
        // Add fixed affix modifiers (for unique items)
        foreach (var affix in fixedAffixes)
        {
            allModifiers.AddRange(affix.modifiers);
        }
        
        return allModifiers;
    }
    
    /// <summary>
    /// Check if character meets requirements to equip this item
    /// </summary>
    public bool CanEquip(AdvancedStatSystem characterStats, int characterLevel)
    {
        if (characterLevel < requiredLevel)
            return false;
            
        if (characterStats.GetStat(StatType.Strength) < strengthRequirement)
            return false;
            
        if (characterStats.GetStat(StatType.Dexterity) < dexterityRequirement)
            return false;
            
        if (characterStats.GetStat(StatType.Intelligence) < intelligenceRequirement)
            return false;
            
        return true;
    }
    
    /// <summary>
    /// Get formatted item name with rarity prefix
    /// </summary>
    public string GetDisplayName()
    {
        string rarityPrefix = rarity switch
        {
            ItemRarity.Magic => "Magic ",
            ItemRarity.Rare => "Rare ",
            ItemRarity.Unique => "Unique ",
            ItemRarity.Legendary => "Legendary ",
            _ => ""
        };
        
        return rarityPrefix + itemName;
    }
    
    /// <summary>
    /// Generate affix text for display
    /// </summary>
    public string GetAffixText()
    {
        string text = "";
        
        // Base modifiers
        foreach (var modifier in baseModifiers)
        {
            if (!string.IsNullOrEmpty(text)) text += "\n";
            text += FormatModifier(modifier);
        }
        
        // Prefix affixes
        foreach (var prefix in currentPrefixes)
        {
            if (!string.IsNullOrEmpty(text)) text += "\n";
            text += prefix.GetAffixText();
        }
        
        // Suffix affixes
        foreach (var suffix in currentSuffixes)
        {
            if (!string.IsNullOrEmpty(text)) text += "\n";
            text += suffix.GetAffixText();
        }
        
        // Fixed affixes (for unique items)
        foreach (var affix in fixedAffixes)
        {
            if (!string.IsNullOrEmpty(text)) text += "\n";
            text += affix.GetAffixText();
        }
        
        return text;
    }
    
    private string FormatModifier(StatModifier modifier)
    {
        string valueText = modifier.modifierType switch
        {
            StatModifier.ModifierType.Flat => modifier.value >= 0 ? $"+{modifier.value:F0}" : $"{modifier.value:F0}",
            StatModifier.ModifierType.PercentIncrease => $"{modifier.value:F0}% increased",
            StatModifier.ModifierType.PercentMore => $"{modifier.value:F0}% more",
            StatModifier.ModifierType.PercentLess => $"{modifier.value:F0}% less",
            _ => modifier.value.ToString("F0")
        };
        
        return $"{valueText} {GetStatDisplayName(modifier.statType)}";
    }
    
    private string GetStatDisplayName(StatType statType)
    {
        return statType switch
        {
            StatType.Life => "to maximum Life",
            StatType.Mana => "to maximum Mana",
            StatType.EnergyShield => "to maximum Energy Shield",
            StatType.Armour => "Armour",
            StatType.Evasion => "Evasion Rating",
            StatType.FireResistance => "Fire Resistance",
            StatType.ColdResistance => "Cold Resistance",
            StatType.LightningResistance => "Lightning Resistance",
            StatType.MovementSpeed => "Movement Speed",
            _ => statType.ToString()
        };
    }
} 