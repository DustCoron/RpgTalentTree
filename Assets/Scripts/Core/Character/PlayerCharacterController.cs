using UnityEngine;

/// <summary>
/// Главный контроллер персонажа игрока, объединяющий новую систему статов с игровым объектом
/// </summary>
public class PlayerCharacterController : MonoBehaviour
{
    [Header("Character Setup")]
    public PlayerClass playerClass;
    public RaceData raceData;
    
    [Header("Stats Integration")]
    public AdvancedStatSystem statSystem;
    public AdvancedCombatEntity combatEntity;
    
    [Header("UI References")]
    public Canvas characterUI;
    public AdvancedMechanicsTester mechanicsTester;
    
    void Awake()
    {
        // Инициализация системы статов
        if (statSystem == null)
            statSystem = new AdvancedStatSystem();
            
        // Инициализация боевой сущности
        if (combatEntity == null)
        {
            combatEntity = gameObject.GetComponent<AdvancedCombatEntity>();
            if (combatEntity == null)
                combatEntity = gameObject.AddComponent<AdvancedCombatEntity>();
        }
    }
    
    void Start()
    {
        InitializeFromSelection();
        SetupCombatEntity();
        
        // Настройка тестера механик если есть
        if (mechanicsTester == null)
            mechanicsTester = FindObjectOfType<AdvancedMechanicsTester>();
            
        if (mechanicsTester != null)
            mechanicsTester.SetTargetStatSystem(statSystem);
    }
    
    void InitializeFromSelection()
    {
        // Загружаем данные из PlayerPrefs (установленные в CharacterSelectionScreen)
        string selectedClassName = PlayerPrefs.GetString("SelectedClass", "Warrior");
        string selectedRaceName = PlayerPrefs.GetString("SelectedRace", "Human");
        
        // Находим и применяем выбранный класс
        playerClass = GetPlayerClassByName(selectedClassName);
        if (playerClass != null)
        {
            Debug.Log($"Applying class: {playerClass.ClassName} ({playerClass.Archetype})");
            playerClass.ApplyToStatSystem(statSystem);
        }
        
        // Применяем расовые бонусы
        ApplyRacialBonuses(selectedRaceName);
        
        Debug.Log($"Character initialized with {statSystem.GetStat(StatType.Life)} Life, {statSystem.GetStat(StatType.Mana)} Mana");
    }
    
    PlayerClass GetPlayerClassByName(string className)
    {
        switch (className)
        {
            case "Paladin": return new PlayerClass_Paladin();
            case "Mage": return new PlayerClass_Mage();
            case "Necromancer": return new PlayerClass_Necromancer();
            case "Warrior": return new PlayerClass_Warrior();
            case "Druid": return new PlayerClass_Druid();
            case "Elementalist": return new PlayerClass_Elementalist();
            case "Ranger": return new PlayerClass_Ranger();
            case "Rogue": return new PlayerClass_Rogue();
            case "Priest": return new PlayerClass_Priest();
            case "Berserker": return new PlayerClass_Berserker();
            default: return new PlayerClass_Warrior(); // Класс по умолчанию
        }
    }
    
    void ApplyRacialBonuses(string raceName)
    {
        // Простые расовые бонусы (можно расширить или загрузить из ScriptableObject)
        switch (raceName)
        {
            case "Human":
                statSystem.AddModifier(new StatModifier(StatType.ExperienceGain, StatModifier.ModifierType.PercentIncrease, 10f, "Human Adaptability"));
                break;
            case "Elf":
                statSystem.AddModifier(new StatModifier(StatType.Intelligence, StatModifier.ModifierType.Flat, 2f, "Elven Wisdom"));
                statSystem.AddModifier(new StatModifier(StatType.Dexterity, StatModifier.ModifierType.Flat, 1f, "Elven Grace"));
                break;
            case "Dwarf":
                statSystem.AddModifier(new StatModifier(StatType.Strength, StatModifier.ModifierType.Flat, 2f, "Dwarven Might"));
                statSystem.AddModifier(new StatModifier(StatType.Vitality, StatModifier.ModifierType.Flat, 1f, "Dwarven Constitution"));
                break;
            case "Orc":
                statSystem.AddModifier(new StatModifier(StatType.Strength, StatModifier.ModifierType.Flat, 3f, "Orcish Strength"));
                statSystem.AddModifier(new StatModifier(StatType.PhysicalDamage, StatModifier.ModifierType.Flat, 5f, "Orcish Ferocity"));
                break;
        }
    }
    
    void SetupCombatEntity()
    {
        if (combatEntity == null) return;
        
        // AdvancedCombatEntity automatically gets the stat system in Awake()
        // No Initialize() method needed - it uses GetComponent<AdvancedStatSystem>()
        
        // Set current resources to maximum using sync methods
        combatEntity.SyncLifeFromExternal(statSystem.GetStat(StatType.Life));
        combatEntity.SyncManaFromExternal(statSystem.GetStat(StatType.Mana));
        
        // Note: Energy Shield is handled by EnergyShieldSystem component automatically
    }
    
    // === PUBLIC МЕТОДЫ ДЛЯ ВЗАИМОДЕЙСТВИЯ ===
    
    public void GainExperience(float amount)
    {
        float bonus = statSystem.GetStat(StatType.ExperienceGain);
        float finalAmount = amount * (1f + bonus / 100f);
        Debug.Log($"Gained {finalAmount} experience (base: {amount}, bonus: {bonus}%)");
        // Здесь логика набора опыта
    }
    
    public void LevelUp()
    {
        Debug.Log($"Level up! Class: {playerClass?.ClassName}");
        // Увеличиваем базовые статы согласно классу
        if (playerClass != null)
        {
            // Пример роста статов по архетипу
            switch (playerClass.Archetype)
            {
                case ClassArchetype.Warrior:
                    statSystem.SetBaseStat(StatType.Strength, statSystem.GetStat(StatType.Strength) + 2);
                    statSystem.SetBaseStat(StatType.Vitality, statSystem.GetStat(StatType.Vitality) + 2);
                    statSystem.SetBaseStat(StatType.Life, statSystem.GetStat(StatType.Life) + 15);
                    break;
                case ClassArchetype.Mage:
                    statSystem.SetBaseStat(StatType.Intelligence, statSystem.GetStat(StatType.Intelligence) + 3);
                    statSystem.SetBaseStat(StatType.Mana, statSystem.GetStat(StatType.Mana) + 20);
                    break;
                case ClassArchetype.Hybrid:
                    statSystem.SetBaseStat(StatType.Strength, statSystem.GetStat(StatType.Strength) + 1);
                    statSystem.SetBaseStat(StatType.Intelligence, statSystem.GetStat(StatType.Intelligence) + 1);
                    statSystem.SetBaseStat(StatType.Vitality, statSystem.GetStat(StatType.Vitality) + 1);
                    statSystem.SetBaseStat(StatType.Life, statSystem.GetStat(StatType.Life) + 10);
                    statSystem.SetBaseStat(StatType.Mana, statSystem.GetStat(StatType.Mana) + 10);
                    break;
            }
        }
        
        // Обновляем боевую сущность
        SetupCombatEntity();
    }
    
    public bool CastSpell(SpellData spellData)
    {
        int manaCost = spellData.GetEffectiveManaCost(statSystem);
        
        if (combatEntity.CurrentMana >= manaCost)
        {
            combatEntity.UseMana(manaCost);
            float damage = spellData.GetEffectiveDamage(statSystem);
            Debug.Log($"Cast {spellData.spellName} for {damage} damage, cost {manaCost} mana");
            return true;
        }
        
        Debug.Log($"Not enough mana to cast {spellData.spellName} (need {manaCost}, have {combatEntity.CurrentMana})");
        return false;
    }
    
    /// <summary>
    /// Equip an item and apply its stat modifiers
    /// </summary>
    public bool EquipItem(EquipmentData equipment)
    {
        // Check if player meets requirements
        if (!equipment.CanEquip(statSystem, 1)) // Assuming level 1 for now
        {
            Debug.LogWarning($"Cannot equip {equipment.itemName}: requirements not met");
            return false;
        }
        
        // Apply all modifiers from the equipment
        var modifiers = equipment.GetAllModifiers();
        foreach (var modifier in modifiers)
        {
            // Create a new modifier with equipment source
            var equipmentModifier = new StatModifier(
                modifier.statType,
                modifier.modifierType,
                modifier.value,
                $"Equipment: {equipment.itemName}"
            );
            
            statSystem.AddModifier(equipmentModifier);
        }
        
        Debug.Log($"Equipped {equipment.GetDisplayName()}");
        return true;
    }
    
    /// <summary>
    /// Unequip an item and remove its stat modifiers
    /// </summary>
    public void UnequipItem(EquipmentData equipment)
    {
        // Remove all modifiers from this equipment piece
        string sourcePrefix = $"Equipment: {equipment.itemName}";
        statSystem.RemoveModifiersWithSource(sourcePrefix);
        
        Debug.Log($"Unequipped {equipment.GetDisplayName()}");
    }
    
    // === ГЕТТЕРЫ ДЛЯ UI ===
    
    public string GetCharacterInfo()
    {
        if (playerClass == null) return "Unknown Character";
        
        return $"{playerClass.ClassName} ({playerClass.Archetype})\n" +
               $"Level: 1 | Life: {combatEntity.CurrentLife:F0}/{statSystem.GetStat(StatType.Life):F0} | " +
               $"Mana: {combatEntity.CurrentMana:F0}/{statSystem.GetStat(StatType.Mana):F0}";
    }
    
    public float GetLifePercentage()
    {
        float maxLife = statSystem.GetStat(StatType.Life);
        return maxLife > 0 ? combatEntity.CurrentLife / maxLife : 0f;
    }
    
    public float GetManaPercentage()
    {
        float maxMana = statSystem.GetStat(StatType.Mana);
        return maxMana > 0 ? combatEntity.CurrentMana / maxMana : 0f;
    }
    
    void Update()
    {
        // Обновляем боевую сущность
        if (combatEntity != null)
            combatEntity.Update();
    }
    
    void OnGUI()
    {
        // Простой отладочный интерфейс
        if (playerClass != null)
        {
            GUI.Label(new Rect(10, 10, 400, 20), GetCharacterInfo());
            
            // Полоски здоровья и маны
            float lifePercent = GetLifePercentage();
            float manaPercent = GetManaPercentage();
            
            GUI.color = Color.red;
            GUI.DrawTexture(new Rect(10, 40, 200 * lifePercent, 20), Texture2D.whiteTexture);
            GUI.color = Color.blue;
            GUI.DrawTexture(new Rect(10, 70, 200 * manaPercent, 20), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
} 