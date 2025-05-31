# Poison Damage Type Removal Summary

## 🔄 **CHANGE MADE**: Poison is now a **Debuff Effect**, not a **Damage Type**

### ❌ **REMOVED**: `RPG_DamageTypes.Poison`
- Poison was removed from the damage types enum
- No longer treated as a direct damage type like Physical, Fire, Cold, Lightning, Chaos

### ✅ **RETAINED**: `RPG_DebuffType.Poison`
- Poison still exists as a debuff effect that applies damage over time
- Triggered by `Chaos` damage type via the debuff system

## 🔧 **FILES UPDATED**

### 1. **`Scripts/Core/Enums/RPG_DamageTypes.cs`**
```csharp
public enum RPG_DamageTypes
{
    Physical,
    Fire, 
    Cold,
    Lightning,
    Chaos         // Poison effects now use Chaos damage
    // Poison - REMOVED
}
```

### 2. **`Scripts/Core/Global/Stats/SpellUtilities.cs`**
- Removed `case RPG_DamageTypes.Poison:`
- Poison effects now use `Chaos` damage calculations

### 3. **`Scripts/Core/Global/Combat/DamageCalculator.cs`**
- Removed `case RPG_DamageTypes.Poison:` from damage calculation
- Removed `case RPG_DamageTypes.Poison:` from resistance calculation
- Poison debuffs now use `Chaos` damage and `ChaosResistance`

## 🧩 **HOW POISON WORKS NOW**

### **As a Debuff Effect System:**
```csharp
// From Scripts/Core/Combat/RPG_Debuff.cs
public enum RPG_DebuffType 
{
    Poison    // ✅ Poison as debuff effect
}

// Poison debuffs are created from Chaos damage:
RPG_DamageTypes.Chaos → RPG_DebuffType.Poison
```

### **Damage Over Time Mechanics:**
- **Triggered by**: `Chaos` damage type
- **Duration**: 8 seconds
- **Damage**: 20% of base damage per second
- **Stacking**: Up to 10 stacks
- **Resistance**: Uses `ChaosResistance` stat
- **Damage Scaling**: Uses `ChaosDamage` stat

### **Example Flow:**
1. Spell/Attack deals `Chaos` damage
2. System checks `RPG_DebuffDatabase.DamageTypeToDebuff[RPG_DamageTypes.Chaos]`
3. Creates `RPG_DebuffType.Poison` debuff
4. Poison applies Chaos damage over time (resisted by ChaosResistance)

## 💡 **BENEFITS OF THIS CHANGE**

### **✅ Clear Separation**
- **Damage Types**: Immediate damage (Physical, Fire, Cold, Lightning, Chaos)
- **Debuff Effects**: Over-time effects (Bleed, Ignite, Chill, Freeze, Shock, Poison)

### **✅ Logical System**
- Poison is an **effect** that something **causes**, not a type of **damage** itself
- Aligns with how most RPG systems work (poison as status effect)
- Maintains mechanical depth while improving clarity

### **✅ Maintained Functionality**  
- All poison mechanics still work exactly the same
- Poison still uses Chaos damage calculations and resistances
- No gameplay impact, only improved code organization

## 🎮 **FOR DEVELOPERS**

### **Creating Poison Effects:**
```csharp
// Instead of dealing "Poison damage"
DamageInput chaosDamage = new DamageInput(attacker, target, RPG_DamageTypes.Chaos, baseDamage);

// This automatically creates poison debuff via the debuff system
var debuffInfo = RPG_DebuffDatabase.DamageTypeToDebuff[RPG_DamageTypes.Chaos];
var poisonDebuff = new RPG_Debuff(debuffInfo.type, debuffInfo.duration, debuffInfo.strength, debuffInfo.maxStacks);
```

### **Checking for Poison:**
```csharp
// Check for poison debuff effect
if (character.HasDebuff(RPG_DebuffType.Poison))
{
    // Handle poison-specific logic
}
```

## 📊 **UPDATED SYSTEM COUNTS**

- **Damage Types**: 5 (Physical, Fire, Cold, Lightning, Chaos)
- **Debuff Effects**: 6 (Bleed, Ignite, Chill, Freeze, Shock, Poison)
- **Poison Source**: Chaos damage type
- **Poison Mechanics**: Unchanged (still uses Chaos stats)

---

**✅ Result**: Cleaner, more logical system architecture while maintaining all poison functionality! 