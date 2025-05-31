using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class RaceData
{
    public string raceName;
    public Sprite icon;
    public Dictionary<StatType, float> racialBonuses = new Dictionary<StatType, float>();
}

[System.Serializable]
public class ClassData
{
    public string className;
    public Sprite icon;
    public PlayerClass playerClass;
    public ClassArchetype archetype;
}

[System.Serializable]
public class AppearanceOption
{
    public string optionName; // Например, "Hair", "SkinColor"
    public List<Sprite> variants; // Иконки вариантов (можно заменить на Color и т.д.)
}

[System.Serializable]
public class CharacterData
{
    public string characterName;
    public GameObject characterPrefab; // базовый префаб
    public Sprite icon;
    public PlayerClass defaultClass;
}

public class CharacterSelectionScreen : MonoBehaviour
{
    [Header("Character Data")]
    public List<CharacterData> characters = new List<CharacterData>();

    [Header("Race/Class/Gender/Appearance Data")]
    public List<RaceData> races = new List<RaceData>();
    public List<ClassData> classes = new List<ClassData>();
    public List<string> genders = new List<string> { "Male", "Female" };
    public List<AppearanceOption> appearanceOptions = new List<AppearanceOption>();

    // Доступные классы игрока
    private List<PlayerClass> availablePlayerClasses = new List<PlayerClass>();

    [Header("UI References")]
    public Transform characterButtonParent;
    public GameObject characterButtonPrefab;
    public RawImage characterRenderImage;
    public Button playButton;

    // UI для кастомизации:
    public Transform raceButtonParent;
    public GameObject raceButtonPrefab;
    public Transform classButtonParent;
    public GameObject classButtonPrefab;
    public Transform genderButtonParent;
    public GameObject genderButtonPrefab;
    public Transform appearanceParent;
    public GameObject appearanceOptionPrefab;

    [Header("Stats Preview")]
    public Transform statsPanel;
    public Text statsText;
    public GameObject statsPreviewPrefab;

    [Header("3D Viewer")]
    public Camera viewerCamera;
    public RenderTexture viewerTexture;
    public Transform characterSpawnPoint;

    // State
    private int selectedCharacterIndex = 0;
    private int selectedRace = 0;
    private int selectedClass = 0;
    private int selectedGender = 0;
    private List<int> selectedAppearanceVariants = new List<int>();
    private GameObject currentCharacterInstance;
    private Vector3 lastMousePosition;
    
    // Система статов для предпросмотра
    private AdvancedStatSystem previewStatSystem;

    void Start()
    {
        // Инициализируем систему статов для предпросмотра
        previewStatSystem = new AdvancedStatSystem();
        
        // Инициализируем доступные классы игрока
        InitializePlayerClasses();
        
        if (characterRenderImage != null && viewerTexture != null)
            characterRenderImage.texture = viewerTexture;

        SetupCharacterButtons();
        SetupRaceButtons();
        SetupClassButtons();
        SetupGenderButtons();
        SetupAppearanceOptions();

        SelectCharacter(0);

        if (playButton != null)
            playButton.onClick.AddListener(OnPlay);
    }

    void InitializePlayerClasses()
    {
        availablePlayerClasses.Clear();
        availablePlayerClasses.Add(new PlayerClass_Paladin());
        availablePlayerClasses.Add(new PlayerClass_Mage());
        availablePlayerClasses.Add(new PlayerClass_Necromancer());
        availablePlayerClasses.Add(new PlayerClass_Warrior());
        availablePlayerClasses.Add(new PlayerClass_Druid());
        availablePlayerClasses.Add(new PlayerClass_Elementalist());
        availablePlayerClasses.Add(new PlayerClass_Ranger());
        availablePlayerClasses.Add(new PlayerClass_Rogue());
        availablePlayerClasses.Add(new PlayerClass_Priest());
        availablePlayerClasses.Add(new PlayerClass_Berserker());
        
        // Синхронизируем с ClassData
        if (classes.Count == 0)
        {
            for (int i = 0; i < availablePlayerClasses.Count; i++)
            {
                var playerClass = availablePlayerClasses[i];
                classes.Add(new ClassData
                {
                    className = playerClass.ClassName,
                    playerClass = playerClass,
                    archetype = playerClass.Archetype,
                    icon = null // Иконка устанавливается в инспекторе
                });
            }
        }
    }

    void SetupCharacterButtons()
    {
        foreach (Transform child in characterButtonParent)
            Destroy(child.gameObject);

        for (int i = 0; i < characters.Count; i++)
        {
            int idx = i;
            var btnObj = Instantiate(characterButtonPrefab, characterButtonParent);
            var icon = btnObj.transform.Find("Icon")?.GetComponent<Image>();
            var txt = btnObj.transform.Find("Name")?.GetComponent<Text>();
            if (icon != null) icon.sprite = characters[i].icon;
            if (txt != null) txt.text = characters[i].characterName;
            btnObj.GetComponent<Button>().onClick.AddListener(() => SelectCharacter(idx));
        }
    }

    void SetupRaceButtons()
    {
        foreach (Transform child in raceButtonParent)
            Destroy(child.gameObject);
        for (int i = 0; i < races.Count; i++)
        {
            int idx = i;
            var btnObj = Instantiate(raceButtonPrefab, raceButtonParent);
            var icon = btnObj.transform.Find("Icon")?.GetComponent<Image>();
            var txt = btnObj.transform.Find("Name")?.GetComponent<Text>();
            if (icon != null) icon.sprite = races[i].icon;
            if (txt != null) txt.text = races[i].raceName;
            btnObj.GetComponent<Button>().onClick.AddListener(() => { 
                selectedRace = idx; 
                ShowCharacter3D(); 
                UpdateStatsPreview(); 
            });
        }
    }

    void SetupClassButtons()
    {
        foreach (Transform child in classButtonParent)
            Destroy(child.gameObject);

        for (int i = 0; i < classes.Count; i++)
        {
            int idx = i;
            var btnObj = Instantiate(classButtonPrefab, classButtonParent);
            var icon = btnObj.transform.Find("Icon")?.GetComponent<Image>();
            var txt = btnObj.transform.Find("Name")?.GetComponent<Text>();
            var descTxt = btnObj.transform.Find("Description")?.GetComponent<Text>();
            var archetypeTxt = btnObj.transform.Find("Archetype")?.GetComponent<Text>();
            
            var classData = classes[i];
            
            if (icon != null && classData.icon != null)
                icon.sprite = classData.icon;
                
            if (txt != null) 
                txt.text = classData.playerClass.ClassName;
                
            if (descTxt != null)
                descTxt.text = classData.playerClass.Description;
                
            if (archetypeTxt != null)
                archetypeTxt.text = classData.playerClass.Archetype.ToString();
                
            btnObj.GetComponent<Button>().onClick.AddListener(() => { 
                selectedClass = idx; 
                ShowCharacter3D(); 
                UpdateStatsPreview(); 
            });
        }
    }

    void SetupGenderButtons()
    {
        foreach (Transform child in genderButtonParent)
            Destroy(child.gameObject);
        for (int i = 0; i < genders.Count; i++)
        {
            int idx = i;
            var btnObj = Instantiate(genderButtonPrefab, genderButtonParent);
            var txt = btnObj.GetComponentInChildren<Text>();
            if (txt != null) txt.text = genders[i];
            btnObj.GetComponent<Button>().onClick.AddListener(() => { 
                selectedGender = idx; 
                ShowCharacter3D(); 
            });
        }
    }

    void SetupAppearanceOptions()
    {
        foreach (Transform child in appearanceParent)
            Destroy(child.gameObject);

        selectedAppearanceVariants.Clear();
        for (int i = 0; i < appearanceOptions.Count; i++)
        {
            selectedAppearanceVariants.Add(0);
            int optionIdx = i;
            var option = appearanceOptions[i];

            var optionObj = Instantiate(appearanceOptionPrefab, appearanceParent);
            var label = optionObj.transform.Find("Label")?.GetComponent<Text>();
            var variantParent = optionObj.transform.Find("Variants");

            if (label != null) label.text = option.optionName;

            if (variantParent != null)
            {
                for (int j = 0; j < option.variants.Count; j++)
                {
                    int variantIdx = j;
                    var variantObj = Instantiate(characterButtonPrefab, variantParent);
                    var icon = variantObj.transform.Find("Icon")?.GetComponent<Image>();
                    if (icon != null) icon.sprite = option.variants[j];
                    variantObj.GetComponent<Button>().onClick.AddListener(() => {
                        selectedAppearanceVariants[optionIdx] = variantIdx;
                        ShowCharacter3D();
                    });
                }
            }
        }
    }
    
    void UpdateStatsPreview()
    {
        if (previewStatSystem == null) return;
        
        // Очищаем предыдущие данные
        previewStatSystem.ClearAllStats();
        
        // Применяем базовые статы класса
        if (selectedClass >= 0 && selectedClass < classes.Count)
        {
            var playerClass = classes[selectedClass].playerClass;
            playerClass.ApplyToStatSystem(previewStatSystem);
        }
        
        // Применяем расовые бонусы
        if (selectedRace >= 0 && selectedRace < races.Count)
        {
            var race = races[selectedRace];
            foreach (var bonus in race.racialBonuses)
            {
                var modifier = new StatModifier(
                    bonus.Key, 
                    StatModifier.ModifierType.Flat, 
                    bonus.Value, 
                    $"Racial: {races[selectedRace].raceName}"
                );
                previewStatSystem.AddModifier(modifier);
            }
        }
        
        // Обновляем UI
        DisplayStatsPreview();
    }
    
    void DisplayStatsPreview()
    {
        if (statsText == null) return;
        
        var playerClass = classes[selectedClass].playerClass;
        var archetype = playerClass.Archetype;
        
        string statsDisplay = $"<b>{playerClass.ClassName}</b> ({archetype})\n\n";
        
        // Основные атрибуты
        statsDisplay += "<color=yellow>Core Attributes:</color>\n";
        statsDisplay += $"Strength: {previewStatSystem.GetStat(StatType.Strength):F0}\n";
        statsDisplay += $"Dexterity: {previewStatSystem.GetStat(StatType.Dexterity):F0}\n";
        statsDisplay += $"Intelligence: {previewStatSystem.GetStat(StatType.Intelligence):F0}\n";
        statsDisplay += $"Vitality: {previewStatSystem.GetStat(StatType.Vitality):F0}\n\n";
        
        // Ресурсы
        statsDisplay += "<color=red>Resources:</color>\n";
        statsDisplay += $"Life: {previewStatSystem.GetStat(StatType.Life):F0}\n";
        statsDisplay += $"Mana: {previewStatSystem.GetStat(StatType.Mana):F0}\n";
        if (previewStatSystem.GetStat(StatType.EnergyShield) > 0)
            statsDisplay += $"Energy Shield: {previewStatSystem.GetStat(StatType.EnergyShield):F0}\n";
        statsDisplay += "\n";
        
        // Урон по архетипу
        statsDisplay += "<color=orange>Damage:</color>\n";
        switch (archetype)
        {
            case ClassArchetype.Warrior:
                statsDisplay += $"Physical Damage: {previewStatSystem.GetStat(StatType.PhysicalDamage):F0}\n";
                if (previewStatSystem.GetStat(StatType.CriticalChance) > 0)
                    statsDisplay += $"Critical Chance: {previewStatSystem.GetStat(StatType.CriticalChance):F0}%\n";
                break;
            case ClassArchetype.Mage:
                statsDisplay += $"Spell Damage: {previewStatSystem.GetStat(StatType.SpellDamage):F0}\n";
                statsDisplay += $"Elemental Damage: {previewStatSystem.GetStat(StatType.ElementalDamage):F0}\n";
                break;
            case ClassArchetype.Summoner:
                statsDisplay += $"Minion Life: {previewStatSystem.GetStat(StatType.MinionLife):F0}\n";
                statsDisplay += $"Minion Damage: {previewStatSystem.GetStat(StatType.MinionDamage):F0}\n";
                break;
            case ClassArchetype.Hybrid:
                statsDisplay += $"Physical: {previewStatSystem.GetStat(StatType.PhysicalDamage):F0}\n";
                statsDisplay += $"Spell: {previewStatSystem.GetStat(StatType.SpellDamage):F0}\n";
                break;
            case ClassArchetype.Support:
                statsDisplay += $"Life Regen: {previewStatSystem.GetStat(StatType.LifeRegeneration):F1}/s\n";
                statsDisplay += $"Mana Regen: {previewStatSystem.GetStat(StatType.ManaRegeneration):F1}/s\n";
                break;
        }
        
        // Защита
        if (previewStatSystem.GetStat(StatType.Armour) > 0 || 
            previewStatSystem.GetStat(StatType.Evasion) > 0)
        {
            statsDisplay += "\n<color=blue>Defense:</color>\n";
            if (previewStatSystem.GetStat(StatType.Armour) > 0)
                statsDisplay += $"Armour: {previewStatSystem.GetStat(StatType.Armour):F0}\n";
            if (previewStatSystem.GetStat(StatType.Evasion) > 0)
                statsDisplay += $"Evasion: {previewStatSystem.GetStat(StatType.Evasion):F0}\n";
        }
        
        // Сопротивления (только если есть)
        var resistances = new List<(StatType, string)> {
            (StatType.FireResistance, "Fire"),
            (StatType.ColdResistance, "Cold"),
            (StatType.LightningResistance, "Lightning"),
            (StatType.ChaosResistance, "Chaos")
        };
        
        var hasResistances = false;
        string resistanceText = "";
        foreach (var (statType, name) in resistances)
        {
            var value = previewStatSystem.GetStat(statType);
            if (value > 0)
            {
                if (!hasResistances)
                {
                    resistanceText += "\n<color=green>Resistances:</color>\n";
                    hasResistances = true;
                }
                resistanceText += $"{name}: {value:F0}%\n";
            }
        }
        statsDisplay += resistanceText;
        
        statsText.text = statsDisplay;
    }

    public void SelectCharacter(int index)
    {
        selectedCharacterIndex = index;
        ShowCharacter3D();
        
        // Если у персонажа есть класс по умолчанию, выбираем его
        if (index < characters.Count && characters[index].defaultClass != null)
        {
            for (int i = 0; i < classes.Count; i++)
            {
                if (classes[i].playerClass.GetType() == characters[index].defaultClass.GetType())
                {
                    selectedClass = i;
                    break;
                }
            }
        }
        
        UpdateStatsPreview();
    }

    void ShowCharacter3D()
    {
        if (characterSpawnPoint == null || characters.Count == 0) return;

        if (currentCharacterInstance != null)
            Destroy(currentCharacterInstance);

        var character = characters[selectedCharacterIndex];
        if (character.characterPrefab != null)
        {
            currentCharacterInstance = Instantiate(character.characterPrefab, characterSpawnPoint.position, characterSpawnPoint.rotation);
            // Здесь можно применить настройки расы, пола, внешности
        }
    }

    void Update()
    {
        if (currentCharacterInstance == null) return;

        // Поворот персонажа мышью
        if (Input.GetMouseButtonDown(0))
            lastMousePosition = Input.mousePosition;

        if (Input.GetMouseButton(0))
        {
            var deltaPos = Input.mousePosition - lastMousePosition;
            currentCharacterInstance.transform.Rotate(Vector3.up, -deltaPos.x * 0.5f);
            lastMousePosition = Input.mousePosition;
        }
    }

    public void OnPlay()
    {
        // Получаем выбранные данные
        var selectedCharacter = characters[selectedCharacterIndex];
        var selectedPlayerClass = classes[selectedClass].playerClass;
        var selectedRaceData = races.Count > selectedRace ? races[selectedRace] : null;
        
        Debug.Log($"Starting game with {selectedCharacter.characterName} as {selectedPlayerClass.ClassName}");
        
        // Здесь можно сохранить выбор игрока в PlayerPrefs или GameManager
        PlayerPrefs.SetString("SelectedCharacter", selectedCharacter.characterName);
        PlayerPrefs.SetString("SelectedClass", selectedPlayerClass.ClassName);
        if (selectedRaceData != null)
            PlayerPrefs.SetString("SelectedRace", selectedRaceData.raceName);
        PlayerPrefs.SetInt("SelectedGender", selectedGender);
        
        // Создаем полную систему статов и сохраняем
        var gameStatSystem = new AdvancedStatSystem();
        selectedPlayerClass.ApplyToStatSystem(gameStatSystem);
        
        if (selectedRaceData != null)
        {
            foreach (var bonus in selectedRaceData.racialBonuses)
            {
                var modifier = new StatModifier(
                    bonus.Key, 
                    StatModifier.ModifierType.Flat, 
                    bonus.Value, 
                    $"Racial: {selectedRaceData.raceName}"
                );
                gameStatSystem.AddModifier(modifier);
            }
        }
        
        // Загрузка следующей сцены или запуск игры
        // UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }
}

