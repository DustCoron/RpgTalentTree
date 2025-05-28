using UnityEngine;

[System.Serializable]
public class RPG_CurseStats
{
    [Tooltip("Maximum number of curses that can be applied")] public int maxCurses;
    [Tooltip("Percentage increase to curse effect")] public float effectIncrease;
    [Tooltip("Percentage increase to curse duration")] public float durationIncrease;

    public static RPG_CurseStats operator +(RPG_CurseStats a, RPG_CurseStats b)
    {
        var result = new RPG_CurseStats();
        result.maxCurses = a.maxCurses + b.maxCurses;
        result.effectIncrease = a.effectIncrease + b.effectIncrease;
        result.durationIncrease = a.durationIncrease + b.durationIncrease;
        return result;
    }

    public static RPG_CurseStats operator -(RPG_CurseStats a, RPG_CurseStats b)
    {
        var result = new RPG_CurseStats();
        result.maxCurses = a.maxCurses - b.maxCurses;
        result.effectIncrease = a.effectIncrease - b.effectIncrease;
        result.durationIncrease = a.durationIncrease - b.durationIncrease;
        return result;
    }

    public static RPG_CurseStats operator *(RPG_CurseStats a, float m)
    {
        var result = new RPG_CurseStats();
        result.maxCurses = Mathf.RoundToInt(a.maxCurses * m);
        result.effectIncrease = a.effectIncrease * m;
        result.durationIncrease = a.durationIncrease * m;
        return result;
    }

    public override string ToString()
    {
        return $"Curses:{maxCurses} Effect:+{effectIncrease}% Duration:+{durationIncrease}%";
    }
}
