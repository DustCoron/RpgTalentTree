// File: Assets/Scripts/Combat/DamageInput.cs
[System.Serializable]
public struct DamageInput
{
    public AdvancedStatSystem attacker;
    public AdvancedStatSystem defender;
    public RPG_DamageTypes damageType; // Using existing RPG_DamageTypes enum
    public float weaponDamage;
    public bool ignoreArmour;
    public bool cannotMiss;
    public bool cannotCrit;
} 