using UnityEngine;
using System.Collections.Generic;

public class AdvancedCombatEntity : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float currentLife;
    [SerializeField] private float currentMana;
    
    [Header("Regeneration")]
    [SerializeField] private float lifeRegenPerSecond = 1f;
    [SerializeField] private float manaRegenPerSecond = 2f;
    
    [Header("Damage Tracking")]
    [SerializeField] private float lastHitTime;
    [SerializeField] private float lastMoveTime;
    [SerializeField] private Vector3 lastPosition;
    
    // Components
    private AdvancedStatSystem statSystem;
    private EnergyShieldSystem energyShield;
    
    // Status tracking
    private Dictionary<string, float> statusTimers = new Dictionary<string, float>();
    
    // Events
    public System.Action<DamageInfo> OnDamageTaken;
    public System.Action<float> OnLifeChanged;
    public System.Action<float> OnManaChanged;
    public System.Action OnDeath;
    
    // Properties
    public float MaxLife => statSystem ? statSystem.GetStat(StatType.Life) : 100f;
    public float MaxMana => statSystem ? statSystem.GetStat(StatType.Mana) : 50f;
    public float CurrentLife => currentLife;
    public float CurrentMana => currentMana;
    public bool IsAlive => currentLife > 0f;
    public bool IsDead => currentLife <= 0f;
    
    private void Awake()
    {
        statSystem = GetComponent<AdvancedStatSystem>();
        energyShield = GetComponent<EnergyShieldSystem>();
        
        lastPosition = transform.position;
    }
    
    private void Start()
    {
        // Initialize health/mana to max values
        currentLife = MaxLife;
        currentMana = MaxMana;
        
        OnLifeChanged?.Invoke(currentLife);
        OnManaChanged?.Invoke(currentMana);
    }
    
    private void Update()
    {
        UpdateRegeneration();
        UpdateMovementTracking();
        UpdateStatusTimers();
    }
    
    private void UpdateRegeneration()
    {
        if (IsDead) return;
        
        // Life regeneration
        float lifeRegen = GetEffectiveLifeRegen() * Time.deltaTime;
        if (lifeRegen > 0 && currentLife < MaxLife)
        {
            HealLife(lifeRegen);
        }
        
        // Mana regeneration
        float manaRegen = GetEffectiveManaRegen() * Time.deltaTime;
        if (manaRegen > 0 && currentMana < MaxMana)
        {
            RestoreMana(manaRegen);
        }
    }
    
    private void UpdateMovementTracking()
    {
        if (Vector3.Distance(transform.position, lastPosition) > 0.1f)
        {
            lastMoveTime = Time.time;
            lastPosition = transform.position;
        }
    }
    
    private void UpdateStatusTimers()
    {
        var keysToRemove = new List<string>();
        
        foreach (var kvp in statusTimers)
        {
            statusTimers[kvp.Key] -= Time.deltaTime;
            if (statusTimers[kvp.Key] <= 0f)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var key in keysToRemove)
        {
            statusTimers.Remove(key);
        }
    }
    
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (IsDead) return;
        
        lastHitTime = Time.time;
        
        // Calculate actual damage
        var damageInput = new DamageInput
        {
            attacker = damageInfo.source?.GetComponent<AdvancedStatSystem>(),
            defender = statSystem,
            damageType = damageInfo.type,
            weaponDamage = damageInfo.amount
        };
        
        var damageResult = DamageCalculator.CalculateDamage(damageInput);
        
        if (damageResult.missed)
        {
            ShowFloatingText("MISS", Color.gray);
            return;
        }
        
        float finalDamage = damageResult.finalDamage;
        
        // Apply energy shield first
        if (energyShield != null)
        {
            finalDamage = energyShield.TakeDamage(finalDamage);
        }
        
        // Apply remaining damage to life
        if (finalDamage > 0)
        {
            currentLife = Mathf.Max(0, currentLife - finalDamage);
            OnLifeChanged?.Invoke(currentLife);
        }
        
        // Show damage number
        string damageText = damageResult.finalDamage.ToString();
        Color textColor = Color.white;
        
        if (damageResult.wasCritical)
        {
            damageText += "!";
            textColor = Color.red;
        }
        
        if (damageResult.blocked)
        {
            damageText = "BLOCKED " + damageText;
            textColor = Color.yellow;
        }
        
        ShowFloatingText(damageText, textColor);
        
        // Update damage info with final values
        damageInfo.amount = damageResult.finalDamage;
        damageInfo.isCritical = damageResult.wasCritical;
        
        OnDamageTaken?.Invoke(damageInfo);
        
        // Check for death
        if (currentLife <= 0)
        {
            Die();
        }
    }
    
    public void HealLife(float amount)
    {
        if (IsDead) return;
        
        float oldLife = currentLife;
        currentLife = Mathf.Min(MaxLife, currentLife + amount);
        
        if (currentLife != oldLife)
        {
            OnLifeChanged?.Invoke(currentLife);
            ShowFloatingText("+" + Mathf.RoundToInt(currentLife - oldLife), Color.green);
        }
    }
    
    public void RestoreMana(float amount)
    {
        float oldMana = currentMana;
        currentMana = Mathf.Min(MaxMana, currentMana + amount);
        
        if (currentMana != oldMana)
        {
            OnManaChanged?.Invoke(currentMana);
        }
    }
    
    public bool UseMana(float amount)
    {
        if (currentMana >= amount)
        {
            currentMana -= amount;
            OnManaChanged?.Invoke(currentMana);
            return true;
        }
        return false;
    }
    
    private void Die()
    {
        OnDeath?.Invoke();
        Debug.Log($"{gameObject.name} has died!");
    }
    
    private float GetEffectiveLifeRegen()
    {
        float baseRegen = lifeRegenPerSecond;
        
        if (statSystem != null)
        {
            baseRegen += statSystem.GetStat(StatType.LifeRegeneration);
        }
        
        return baseRegen;
    }
    
    private float GetEffectiveManaRegen()
    {
        float baseRegen = manaRegenPerSecond;
        
        if (statSystem != null)
        {
            baseRegen += statSystem.GetStat(StatType.ManaRegeneration);
        }
        
        return baseRegen;
    }
    
    private void ShowFloatingText(string text, Color color)
    {
        // Implementation depends on your floating text system
        // For now, just log to console
        Debug.Log($"Floating Text: {text} (Color: {color})");
        
        // Example integration with damage display system:
        // var damageDisplay = FindObjectOfType<DamageNumberDisplay>();
        // if (damageDisplay != null)
        // {
        //     damageDisplay.ShowDamageText(transform.position, text, color);
        // }
    }
    
    // Utility methods for conditions and modifiers
    public float GetLifePercentage() => MaxLife > 0 ? currentLife / MaxLife : 0f;
    public float GetManaPercentage() => MaxMana > 0 ? currentMana / MaxMana : 0f;
    public float TimeSinceLastHit() => Time.time - lastHitTime;
    public float GetTimeSinceLastMove() => Time.time - lastMoveTime;
    public bool IsMoving() => GetTimeSinceLastMove() < 0.1f;
    public bool IsRecentlyDamaged() => TimeSinceLastHit() < 4f;
    
    public void SetStatus(string statusName, float duration)
    {
        statusTimers[statusName] = duration;
    }
    
    public bool HasStatus(string statusName)
    {
        return statusTimers.ContainsKey(statusName) && statusTimers[statusName] > 0f;
    }
    
    public float GetStat(StatType statType)
    {
        return statSystem ? statSystem.GetStat(statType) : 0f;
    }
    
    // Sync methods for external systems
    public void SyncLifeFromExternal(float newLife)
    {
        currentLife = Mathf.Clamp(newLife, 0, MaxLife);
        OnLifeChanged?.Invoke(currentLife);
    }
    
    public void SyncManaFromExternal(float newMana)
    {
        currentMana = Mathf.Clamp(newMana, 0, MaxMana);
        OnManaChanged?.Invoke(currentMana);
    }
} 