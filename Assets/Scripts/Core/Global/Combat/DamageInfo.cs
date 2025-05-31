// File: Assets/Scripts/Combat/DamageInfo.cs
// This struct was implicitly used by AdvancedCombatEntity and EnergyShieldSystem.
// Defining it explicitly for clarity and modularity.

[System.Serializable]
public struct DamageInfo
{
    public float amount;
    public RPG_DamageTypes type; // Using existing RPG_DamageTypes enum
    public AdvancedCombatEntity source; // The entity that dealt the damage
    public bool isCritical;
    // Add any other relevant details about the damage event, e.g.:
    // public Vector3 hitPoint;
    // public bool isDot; // Damage over time
} 