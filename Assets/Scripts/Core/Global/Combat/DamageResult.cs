[System.Serializable]
public struct DamageResult
{
    public float baseDamage;
    public int finalDamage;
    public bool wasCritical;
    public bool missed;
    public bool blocked; // Renamed from wasBlocked for clarity, as 'blocked' already implies past tense in this context.
    public RPG_DamageTypes damageType; // Using existing RPG_DamageTypes enum
} 