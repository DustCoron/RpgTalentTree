using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "Affix Database", menuName = "RPG/Affix Database")]
public class AffixDatabase : ScriptableObject
{
    [Header("Affix Collections")]
    public List<EquipmentAffix> prefixes = new List<EquipmentAffix>();
    public List<EquipmentAffix> suffixes = new List<EquipmentAffix>();
    
    [Header("Generation Settings")]
    public AnimationCurve tierWeights; // Weight distribution for different tiers
    
    public List<EquipmentAffix> GetAvailablePrefixes(EquipmentData equipment)
    {
        return prefixes.Where(affix => affix.CanRollOnItem(equipment)).ToList();
    }
    
    public List<EquipmentAffix> GetAvailableSuffixes(EquipmentData equipment)
    {
        return suffixes.Where(affix => affix.CanRollOnItem(equipment)).ToList();
    }
    
    public EquipmentAffix RollRandomPrefix(EquipmentData equipment)
    {
        var available = GetAvailablePrefixes(equipment);
        if (available.Count == 0) return null;
        
        return SelectWeightedAffix(available);
    }
    
    public EquipmentAffix RollRandomSuffix(EquipmentData equipment)
    {
        var available = GetAvailableSuffixes(equipment);
        if (available.Count == 0) return null;
        
        return SelectWeightedAffix(available);
    }
    
    private EquipmentAffix SelectWeightedAffix(List<EquipmentAffix> affixes)
    {
        // Weight by tier (higher tier = better affix = lower chance)
        float totalWeight = 0f;
        var weights = new float[affixes.Count];
        
        for (int i = 0; i < affixes.Count; i++)
        {
            float tierWeight = tierWeights.Evaluate((int)affixes[i].tier / 6f);
            weights[i] = tierWeight;
            totalWeight += tierWeight;
        }
        
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        for (int i = 0; i < affixes.Count; i++)
        {
            currentWeight += weights[i];
            if (randomValue <= currentWeight)
            {
                return affixes[i];
            }
        }
        
        return affixes[affixes.Count - 1]; // Fallback
    }
} 