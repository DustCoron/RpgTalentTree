using UnityEngine;
using System.Linq;

public class AdvancedMechanicsTester : MonoBehaviour
{
    [Header("Testing Keys")]
    [SerializeField] private KeyCode testStatsKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode testDamageKey = KeyCode.Alpha2;
    [SerializeField] private KeyCode testEnergyShieldKey = KeyCode.Alpha3;
    [SerializeField] private KeyCode testAffixKey = KeyCode.Alpha4;
    [SerializeField] private KeyCode showStatsKey = KeyCode.Tab;
    
    [Header("Test Data")]
    [SerializeField] private AffixDatabase affixDatabase;
    
    private AdvancedStatSystem playerStats;
    private AdvancedCombatEntity playerCombat;
    private EnergyShieldSystem playerES;
    private bool showStatsPanel = false;
    
    private void Start()
    {
        // Try to find player components by tag first, then by name
        var player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            player = GameObject.Find("Player");
        }
        
        if (player != null)
        {
            playerStats = player.GetComponent<AdvancedStatSystem>();
            playerCombat = player.GetComponent<AdvancedCombatEntity>();
            playerES = player.GetComponent<EnergyShieldSystem>();
        }
        else
        {
            Debug.LogWarning("AdvancedMechanicsTester: No player object found. Testing functions may not work.");
        }
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(testStatsKey))
        {
            TestStatModifiers();
        }
        
        if (Input.GetKeyDown(testDamageKey))
        {
            TestDamageCalculation();
        }
        
        if (Input.GetKeyDown(testEnergyShieldKey))
        {
            TestEnergyShield();
        }
        
        if (Input.GetKeyDown(testAffixKey))
        {
            TestAffixGeneration();
        }
        
        if (Input.GetKeyDown(showStatsKey))
        {
            showStatsPanel = !showStatsPanel;
        }
    }
    
    private void TestStatModifiers()
    {
        if (playerStats == null)
        {
            Debug.LogError("Player stats system not found!");
            return;
        }
        
        // Add temporary stat modifiers
        var strengthMod = new StatModifier(StatType.Strength, StatModifier.ModifierType.Flat, 25, 5f, "Test Buff");
        var damageMod = new StatModifier(StatType.PhysicalDamage, StatModifier.ModifierType.PercentIncrease, 50, 5f, "Test Buff");
        var critMod = new StatModifier(StatType.CriticalChance, StatModifier.ModifierType.Flat, 15, 5f, "Test Buff");
        
        playerStats.AddModifier(strengthMod);
        playerStats.AddModifier(damageMod);
        playerStats.AddModifier(critMod);
        
        Debug.Log("Added temporary stat modifiers for 5 seconds");
    }
    
    private void TestDamageCalculation()
    {
        if (playerCombat == null)
        {
            Debug.LogError("Player combat entity not found!");
            return;
        }
        
        // Create test damage
        var damageInfo = new DamageInfo
        {
            amount = 50,
            type = RPG_DamageTypes.Physical,
            source = null,
            isCritical = false
        };
        
        playerCombat.TakeDamage(damageInfo);
        
        Debug.Log("Applied test damage");
    }
    
    private void TestEnergyShield()
    {
        if (playerES == null || playerStats == null)
        {
            Debug.LogError("Player energy shield or stats system not found!");
            return;
        }
        
        // Add energy shield
        var esMod = new StatModifier(StatType.EnergyShield, StatModifier.ModifierType.Flat, 200, "Test ES");
        playerStats.AddModifier(esMod);
        
        // Restore energy shield
        playerES.RestoreEnergyShield(200);
        
        Debug.Log("Added 200 Energy Shield");
    }
    
    private void TestAffixGeneration()
    {
        if (affixDatabase == null)
        {
            Debug.LogError("Affix database not assigned!");
            return;
        }
        
        // Create test equipment (assuming we have EquipmentData available)
        // Note: This requires the EquipmentData class to be defined
        // For now, we'll just log that the test was called
        Debug.Log("Affix generation test called (requires EquipmentData implementation)");
        
        /*
        var testEquipment = new EquipmentData
        {
            Name = "Test Armor",
            Slot = EquipmentSlot.Chest, // Assuming this enum exists
            ItemLevel = 50
        };
        
        // Roll random affixes
        var prefix = affixDatabase.RollRandomPrefix(testEquipment);
        var suffix = affixDatabase.RollRandomSuffix(testEquipment);
        
        Debug.Log($"Rolled Prefix: {prefix?.affixName ?? "None"}");
        Debug.Log($"Rolled Suffix: {suffix?.affixName ?? "None"}");
        */
    }
    
    private void OnGUI()
    {
        if (!showStatsPanel || playerStats == null) return;
        
        // Stats Panel
        var rect = new Rect(10, 10, 400, 600);
        GUI.Box(rect, "");
        
        var labelRect = new Rect(20, 20, 380, 20);
        GUI.Label(labelRect, "<b>Character Stats</b>");
        
        int yOffset = 45;
        var allStats = playerStats.GetAllStats();
        var sortedStats = allStats.OrderBy(kvp => kvp.Key.ToString()).ToList();
        
        foreach (var stat in sortedStats)
        {
            if (stat.Value != 0) // Only show non-zero stats
            {
                var statRect = new Rect(20, yOffset, 380, 18);
                string valueText = FormatStatValue(stat.Key, stat.Value);
                GUI.Label(statRect, $"{stat.Key}: {valueText}");
                yOffset += 20;
            }
        }
        
        // Combat info
        if (playerCombat != null)
        {
            yOffset += 10;
            GUI.Label(new Rect(20, yOffset, 380, 18), "<b>Combat Status</b>");
            yOffset += 25;
            
            GUI.Label(new Rect(20, yOffset, 380, 18), $"Life: {playerCombat.CurrentLife:F0}/{playerCombat.MaxLife:F0}");
            yOffset += 20;
            
            GUI.Label(new Rect(20, yOffset, 380, 18), $"Mana: {playerCombat.CurrentMana:F0}/{playerCombat.MaxMana:F0}");
            yOffset += 20;
            
            if (playerES != null)
            {
                GUI.Label(new Rect(20, yOffset, 380, 18), $"Energy Shield: {playerES.GetCurrentEnergyShield():F0}/{playerES.GetMaxEnergyShield():F0}");
                yOffset += 20;
            }
        }
        
        // Controls
        yOffset += 20;
        GUI.Label(new Rect(20, yOffset, 380, 18), "<b>Controls:</b>");
        yOffset += 25;
        GUI.Label(new Rect(20, yOffset, 380, 18), "1 - Test Stat Modifiers");
        yOffset += 18;
        GUI.Label(new Rect(20, yOffset, 380, 18), "2 - Test Damage");
        yOffset += 18;
        GUI.Label(new Rect(20, yOffset, 380, 18), "3 - Test Energy Shield");
        yOffset += 18;
        GUI.Label(new Rect(20, yOffset, 380, 18), "4 - Test Affix Generation");
        yOffset += 18;
        GUI.Label(new Rect(20, yOffset, 380, 18), "Tab - Toggle Stats Panel");
    }
    
    private string FormatStatValue(StatType statType, float value)
    {
        switch (statType)
        {
            case StatType.CriticalChance:
            case StatType.BlockChance:
            case StatType.SpellBlock:
            case StatType.FireResistance:
            case StatType.ColdResistance:
            case StatType.LightningResistance:
            case StatType.ChaosResistance:
            case StatType.MovementSpeed:
                return $"{value:F1}%";
                
            case StatType.CriticalMultiplier:
                return $"{value:F0}%";
                
            default:
                return value.ToString("F0");
        }
    }
} 