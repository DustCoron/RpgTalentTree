# AdvancedCombatEntity Method Fix

## 🐛 **ERROR FIXED**: CS1061 - Missing Initialize Method

### **Problem**
- `PlayerCharacterController.cs` was calling `combatEntity.Initialize(statSystem)` on line 114
- `AdvancedCombatEntity` class doesn't have an `Initialize()` method
- Several other method calls were using incorrect method names

### **Root Cause**
The `AdvancedCombatEntity` class uses a different architecture:
- It automatically gets the `AdvancedStatSystem` in `Awake()` via `GetComponent<AdvancedStatSystem>()`
- No manual initialization is needed
- Method names differ from what `PlayerCharacterController` was expecting

## 🔧 **FIXES APPLIED**

### **1. Removed Initialize Call**
```csharp
// ❌ BEFORE (doesn't exist)
combatEntity.Initialize(statSystem);

// ✅ AFTER (automatic initialization)
// AdvancedCombatEntity automatically gets the stat system in Awake()
// No Initialize() method needed - it uses GetComponent<AdvancedStatSystem>()
```

### **2. Fixed Resource Setting Methods**
```csharp
// ❌ BEFORE (methods don't exist)
combatEntity.SetCurrentLife(statSystem.GetStat(StatType.Life));
combatEntity.SetCurrentMana(statSystem.GetStat(StatType.Mana));
combatEntity.SetCurrentEnergyShield(statSystem.GetStat(StatType.EnergyShield));

// ✅ AFTER (using correct sync methods)
combatEntity.SyncLifeFromExternal(statSystem.GetStat(StatType.Life));
combatEntity.SyncManaFromExternal(statSystem.GetStat(StatType.Mana));
// Note: Energy Shield is handled by EnergyShieldSystem component automatically
```

### **3. Fixed Mana Usage Methods**
```csharp
// ❌ BEFORE (methods don't exist)
if (combatEntity.GetCurrentMana() >= manaCost)
{
    combatEntity.ConsumeMana(manaCost);
}

// ✅ AFTER (using correct properties and methods)
if (combatEntity.CurrentMana >= manaCost)
{
    combatEntity.UseMana(manaCost);
}
```

### **4. Fixed Property Access**
```csharp
// ❌ BEFORE (methods don't exist)
combatEntity.GetCurrentLife()
combatEntity.GetCurrentMana()

// ✅ AFTER (using correct properties)
combatEntity.CurrentLife
combatEntity.CurrentMana
```

## 📋 **CORRECT ADVANCEDCOMBATENTITY API**

### **Properties (Read-Only)**
- `float CurrentLife` - Current life points
- `float CurrentMana` - Current mana points  
- `float MaxLife` - Maximum life (from stat system)
- `float MaxMana` - Maximum mana (from stat system)
- `bool IsAlive` - Whether entity is alive
- `bool IsDead` - Whether entity is dead

### **Resource Management Methods**
- `void HealLife(float amount)` - Heal life points
- `void RestoreMana(float amount)` - Restore mana points
- `bool UseMana(float amount)` - Consume mana (returns success)
- `void SyncLifeFromExternal(float newLife)` - Set life directly
- `void SyncManaFromExternal(float newMana)` - Set mana directly

### **Combat Methods**
- `void TakeDamage(DamageInfo damageInfo)` - Apply damage
- `float GetLifePercentage()` - Life as 0-1 percentage
- `float GetManaPercentage()` - Mana as 0-1 percentage

### **Status Methods**
- `void SetStatus(string statusName, float duration)` - Apply status effect
- `bool HasStatus(string statusName)` - Check for status effect
- `float GetStat(StatType statType)` - Get stat value via stat system

## 🎯 **INTEGRATION NOTES**

### **Automatic Initialization**
```csharp
// AdvancedCombatEntity automatically:
// 1. Gets AdvancedStatSystem component in Awake()
// 2. Sets currentLife/currentMana to max values in Start()
// 3. Updates regeneration, movement tracking, status timers in Update()
```

### **Stat System Integration**
```csharp
// MaxLife and MaxMana are computed properties that pull from the stat system:
public float MaxLife => statSystem ? statSystem.GetStat(StatType.Life) : 100f;
public float MaxMana => statSystem ? statSystem.GetStat(StatType.Mana) : 50f;
```

### **Event System**
```csharp
// AdvancedCombatEntity provides events for UI integration:
public System.Action<DamageInfo> OnDamageTaken;
public System.Action<float> OnLifeChanged;
public System.Action<float> OnManaChanged;
public System.Action OnDeath;
```

## ✅ **RESULT**

- **Compilation error CS1061 resolved**
- **All method calls now use correct AdvancedCombatEntity API**
- **PlayerCharacterController properly integrates with combat system**
- **Resource management works as intended**
- **No functionality lost - all features still work**

The system now properly uses the existing `AdvancedCombatEntity` architecture without requiring any changes to the combat entity class itself. 