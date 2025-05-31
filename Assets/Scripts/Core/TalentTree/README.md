# RPG Talent Tree System

A complete, professional-grade talent tree system for Unity RPG games with full integration into advanced stat systems.

## 🌟 Features

### Core System
- **Modular Architecture**: Clean separation of data, logic, and UI
- **AdvancedStatSystem Integration**: Seamless stat modification system
- **Event-Driven Design**: Real-time UI updates and notifications
- **Professional UI**: Unity UI Toolkit implementation with modern styling
- **Persistence**: Automatic save/load with JSON serialization

### Talent Features
- **5 Node Types**: Minor (1pt), Notable (2pts), Keystone (3pts), Mastery, Jewel
- **Prerequisites System**: Complex dependency trees with validation
- **Archetype Restrictions**: Class-specific talent availability
- **Stat Modifiers**: Full support for 4 modifier types (Flat, %, More, Less)
- **Conditional Effects**: Context-sensitive talent bonuses

### Balance & Progression
- **Point Economy**: Balanced progression with milestone bonuses
- **Tree Validation**: Prevents circular dependencies and orphaned nodes
- **Respec System**: Full talent reset with point refund
- **Level Requirements**: Gate powerful talents behind level progression

## 📁 File Structure

```
Scripts/Core/TalentTree/
├── RPG_TalentNodeData.cs      # Individual talent node data & effects
├── RPG_TalentTreeData.cs      # Complete talent tree configuration
├── TalentManager.cs           # Runtime talent management & logic
├── RPG_TalentTreeGenerator.cs # UI generation & interaction system
├── TalentTemplates.cs         # Pre-built talent trees for all classes
├── TalentTreeDemo.cs          # Demo script and testing utilities
└── README.md                  # This documentation
```

## 🔧 Core Components

### RPG_TalentNodeData
```csharp
[CreateAssetMenu(menuName = "TalentTree/Talent Node")]
public class RPG_TalentNodeData : ScriptableObject
```

**Key Features:**
- **Node Information**: Name, description, icon, type, cost
- **Position Data**: Ring index and angle for circular layout
- **Requirements**: Prerequisites, level, archetype restrictions
- **Effects**: Stat modifiers and special abilities
- **Methods**: ApplyToStatSystem(), GetStatsPreview()

**Node Types:**
- `Minor` - Basic talents (1 point)
- `Notable` - Significant bonuses (2 points) 
- `Keystone` - Game-changing effects (3 points)
- `Mastery` - Specialization nodes
- `Jewel` - Socketed enhancement slots

### RPG_TalentTreeData
```csharp
[CreateAssetMenu(menuName = "TalentTree/Talent Tree")]
public class RPG_TalentTreeData : ScriptableObject
```

**Key Features:**
- **Tree Configuration**: Name, archetype, layout settings
- **Node Management**: Organized node collections
- **Validation System**: Dependency checking, error detection
- **Statistics**: Node counts, total costs, balance metrics
- **Utility Methods**: GetNodesByType(), ValidateTree(), GetTreeStats()

### TalentManager
```csharp
public class TalentManager : MonoBehaviour
```

**Runtime Management:**
- **State Tracking**: Allocated nodes, available points
- **Validation Logic**: CanAllocateTalent(), CanDeallocateTalent()
- **Stat Integration**: Automatic effect application/removal
- **Events**: OnTalentAllocated, OnTalentDeallocated, OnTalentPointsChanged
- **Persistence**: Save/load talent state

### RPG_TalentTreeGenerator
```csharp
[RequireComponent(typeof(UIDocument))]
public class RPG_TalentTreeGenerator : MonoBehaviour
```

**UI Features:**
- **Modern Interface**: Unity UI Toolkit implementation
- **Interactive Nodes**: Hover effects, selection, allocation buttons
- **Information Panel**: Real-time node details and requirements
- **Navigation**: Zoom (0.3x-3x), pan, mouse controls
- **Visual Feedback**: Different styles for node states

## 🎮 Usage Guide

### Basic Setup

1. **Create Talent Trees**:
```csharp
// Use templates for quick setup
var trees = TalentTemplates.CreateAllBaseTrees();

// Or create custom trees
var customTree = ScriptableObject.CreateInstance<RPG_TalentTreeData>();
// Configure tree...
```

2. **Setup TalentManager**:
```csharp
// Add to GameObject with PlayerCharacterController
var talentManager = gameObject.AddComponent<TalentManager>();
talentManager.availableTrees = trees;
talentManager.currentLevel = 1;
talentManager.availableTalentPoints = 1;
```

3. **Create UI**:
```csharp
// Add RPG_TalentTreeGenerator to GameObject with UIDocument
var generator = gameObject.AddComponent<RPG_TalentTreeGenerator>();
generator.treeData = warriorTree;
generator.talentManager = talentManager;
```

### Runtime Operations

```csharp
// Allocate talent
bool success = talentManager.TryAllocateTalent(nodeData);

// Check availability
bool canAllocate = talentManager.CanAllocateTalent(nodeData, out string reason);

// Level up and gain points
talentManager.LevelUp();

// Reset all talents
talentManager.ResetAllTalents();

// Save/load state
talentManager.SaveTalentState();
talentManager.LoadTalentState();
```

## 🏗️ Template Trees

### Warrior Tree ("Warrior Combat")
- **Focus**: Physical damage and defense
- **Keystones**: Unwavering Stance (immunity to stun, evasion penalty)
- **Notable Talents**: Two-Handed Mastery, Shield Wall, Bloodlust
- **Archetype**: Warrior

### Mage Tree ("Arcane Arts") 
- **Focus**: Elemental magic and spellcasting
- **Keystones**: Elemental Overload (penetrating elemental damage)
- **Notable Talents**: Fire Mastery, Critical Strikes, Mana Shield
- **Archetype**: Mage

### Necromancer Tree ("Death Magic")
- **Focus**: Minion control and life manipulation
- **Keystones**: Lord of the Undead (massive minion bonuses, no life regen)
- **Notable Talents**: Skeletal Army, Life Drain
- **Archetype**: Summoner

### Utility Tree ("Utility Skills")
- **Focus**: Universal character improvements
- **Available to**: All archetypes (requiresAllArchetypes = true)
- **Notable Talents**: Student (experience bonus)

## 🔍 Integration Points

### AdvancedStatSystem
```csharp
// Automatic stat application
node.ApplyToStatSystem(playerStatSystem);

// Stat types supported
StatType.PhysicalDamage, StatType.Life, StatType.CriticalChance
// + 55 more stat types
```

### PlayerCharacterController
```csharp
// Archetype checking
if (playerController?.playerClass != null)
{
    bool available = node.IsAvailableForArchetype(
        playerController.playerClass.Archetype
    );
}
```

## 🎨 UI Customization

### Visual States
```css
/* Node state styles */
.talent-node.locked { background-color: gray; }
.talent-node.available { background-color: white; }
.talent-node.allocated { background-color: green; }
.talent-node.keystone { background-color: gold; }
```

### Custom Layouts
```csharp
// Adjust tree layout
treeData.ringSpacing = 100f;  // Distance between rings
treeData.maxRings = 7;        // Maximum rings
treeData.treeCenter = Vector2.zero;  // Center position
```

## 🧪 Testing & Demo

Use `TalentTreeDemo.cs` for comprehensive testing:

```csharp
// Add to GameObject and configure
public bool runDemoOnStart = true;
public bool logDetailedInfo = true;

// Context menu options:
// - Run Demo
// - Reset All Talents  
// - Add Talent Points
// - Level Up
```

## 📊 Performance Notes

- **Memory**: Efficient ScriptableObject-based data storage
- **Updates**: Event-driven UI updates (no polling)
- **Serialization**: JSON-based save system with error handling
- **Validation**: Cached validation results for performance

## 🔧 Extension Points

### Custom Node Types
```csharp
public enum TalentNodeType
{
    Minor, Notable, Keystone, Mastery, Jewel,
    // Add custom types here
}
```

### Custom Stat Modifiers
```csharp
public class TalentStatModifier
{
    public StatType statType;
    public StatModifier.ModifierType modifierType;
    public float value;
    public ModifierCondition condition; // For conditional effects
}
```

### Custom Abilities
```csharp
public class TalentAbility
{
    public string abilityName;
    public AbilityType type; // Passive, Active, Toggle, Triggered
    // Implement custom ability logic
}
```

## 🐛 Troubleshooting

### Common Issues

1. **"Namespace conflicts"**: Ensure no duplicate class definitions
2. **"Missing StatType"**: Check AdvancedStatSystem integration
3. **"Circular dependencies"**: Use tree validation methods
4. **"UI not updating"**: Verify event listener setup

### Debug Tools

```csharp
// Validate all trees
talentManager.ValidateAllTrees();

// Check tree statistics
var stats = treeData.GetTreeStats();
Debug.Log($"Tree validation: {stats}");

// Monitor talent allocation
talentManager.OnTalentAllocated += (node) => 
    Debug.Log($"Allocated: {node.nodeName}");
```

## 📈 Future Enhancements

- **Talent Set Bonuses**: Synergy between multiple related talents
- **Dynamic Prerequisites**: Runtime-calculated requirements
- **Talent Mutations**: Randomized talent variations
- **Cross-Tree Dependencies**: Talents requiring nodes from multiple trees
- **Talent Socketing**: Jewel system for customizable enhancements

---

*Built for professional RPG development with Unity. Fully integrated with AdvancedStatSystem and ready for production use.* 