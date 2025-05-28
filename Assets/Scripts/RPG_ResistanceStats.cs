using UnityEngine;

[System.Serializable]
public class RPG_ResistanceStats
{
    [Tooltip("Elemental fire resistance in percentage")] public float fire;
    [Tooltip("Elemental cold resistance in percentage")] public float cold;
    [Tooltip("Elemental lightning resistance in percentage")] public float lightning;
    [Tooltip("Chaos resistance in percentage")] public float chaos;
    [Tooltip("Additional maximum resistance above the default cap")] public float maxResistanceBonus;

    public float MaxResistance => 75f + maxResistanceBonus;

    public float GetResistance(RPG_DamageTypes type, bool ignoreCap = false)
    {
        float value = 0f;
        switch (type)
        {
            case RPG_DamageTypes.Fire: value = fire; break;
            case RPG_DamageTypes.Cold: value = cold; break;
            case RPG_DamageTypes.Lightning: value = lightning; break;
            case RPG_DamageTypes.Chaos: value = chaos; break;
            case RPG_DamageTypes.Physical: value = 0f; break;
        }
        return ignoreCap ? value : Mathf.Min(value, MaxResistance);
    }

    public float DamageAfterResistance(RPG_DamageTypes type, float damage, bool ignoreCap = false)
    {
        float res = GetResistance(type, ignoreCap);
        return damage * (1f - res / 100f);
    }

    public static RPG_ResistanceStats operator +(RPG_ResistanceStats a, RPG_ResistanceStats b)
    {
        var result = new RPG_ResistanceStats();
        result.fire = a.fire + b.fire;
        result.cold = a.cold + b.cold;
        result.lightning = a.lightning + b.lightning;
        result.chaos = a.chaos + b.chaos;
        result.maxResistanceBonus = a.maxResistanceBonus + b.maxResistanceBonus;
        return result;
    }

    public override string ToString()
    {
        return $"FireRes:{fire}% ColdRes:{cold}% LightningRes:{lightning}% ChaosRes:{chaos}% MaxRes:{MaxResistance}%";
    }
}
