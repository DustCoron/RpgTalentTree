using UnityEngine;
using System.Collections; 

public class EnergyShieldSystem : MonoBehaviour
{
    [Header("Energy Shield Settings")]
    [SerializeField] private float rechargeDelay = 2f; // Delay before recharge starts
    [SerializeField] private float rechargeRate = 20f; // ES per second during recharge
    [SerializeField] private bool canRechargeWhileTakingDamage = false;
    
    private AdvancedStatSystem statSystem;
    private AdvancedCombatEntity combatEntity;
    
    private float currentEnergyShield;
    private float lastDamageTime;
    private bool isRecharging;
    private Coroutine rechargeCoroutine;
    
    // Events
    public System.Action<float, float> OnEnergyShieldChanged; // current, max
    public System.Action OnRechargeStarted;
    public System.Action OnRechargeStopped;
    
    private void Awake()
    {
        statSystem = GetComponent<AdvancedStatSystem>();
        combatEntity = GetComponent<AdvancedCombatEntity>();
        
        if (combatEntity != null)
        {
            // Assuming DamageInfo will be defined elsewhere, or we can define a simple one here.
            combatEntity.OnDamageTaken += OnExternalDamageTaken; 
        }
    }
    
    private void Start()
    {
        currentEnergyShield = GetMaxEnergyShield();
        OnEnergyShieldChanged?.Invoke(currentEnergyShield, GetMaxEnergyShield());
    }
    
    private void Update()
    {
        // Check if we should start recharging
        if (!isRecharging && CanStartRecharge())
        {
            StartRecharge();
        }
    }
    
    private bool CanStartRecharge()
    {
        if (currentEnergyShield >= GetMaxEnergyShield()) return false;
        if (Time.time - lastDamageTime < rechargeDelay) return false;
        // Ensure combatEntity is not null before accessing its properties
        if (!canRechargeWhileTakingDamage && combatEntity != null && combatEntity.IsRecentlyDamaged()) return false;
        
        return true;
    }
    
    private void StartRecharge()
    {
        if (rechargeCoroutine != null)
        {
            StopCoroutine(rechargeCoroutine);
        }
        
        rechargeCoroutine = StartCoroutine(RechargeCoroutine());
        isRecharging = true;
        OnRechargeStarted?.Invoke();
    }
    
    private void StopRecharge()
    {
        if (rechargeCoroutine != null)
        {
            StopCoroutine(rechargeCoroutine);
            rechargeCoroutine = null;
        }
        
        isRecharging = false;
        OnRechargeStopped?.Invoke();
    }
    
    private IEnumerator RechargeCoroutine()
    {
        while (currentEnergyShield < GetMaxEnergyShield())
        {
            float rechargeAmount = GetEffectiveRechargeRate() * Time.deltaTime;
            currentEnergyShield = Mathf.Min(currentEnergyShield + rechargeAmount, GetMaxEnergyShield());
            
            OnEnergyShieldChanged?.Invoke(currentEnergyShield, GetMaxEnergyShield());
            
            yield return null;
        }
        
        isRecharging = false;
        OnRechargeStopped?.Invoke();
        rechargeCoroutine = null; // Ensure coroutine reference is cleared after completion
    }
    
    public float TakeDamage(float damage)
    {
        lastDamageTime = Time.time;
        
        if (isRecharging)
        {
            StopRecharge();
        }
        
        if (currentEnergyShield > 0)
        {
            float damageToES = Mathf.Min(damage, currentEnergyShield);
            currentEnergyShield -= damageToES;
            damage -= damageToES;
            
            OnEnergyShieldChanged?.Invoke(currentEnergyShield, GetMaxEnergyShield());
        }
        
        return damage; // Remaining damage that goes to life
    }
    
    public void RestoreEnergyShield(float amount)
    {
        float oldES = currentEnergyShield;
        currentEnergyShield = Mathf.Min(currentEnergyShield + amount, GetMaxEnergyShield());
        
        if (currentEnergyShield != oldES)
        {
            OnEnergyShieldChanged?.Invoke(currentEnergyShield, GetMaxEnergyShield());
        }
    }
    
    public float GetMaxEnergyShield()
    {
        return statSystem ? statSystem.GetStat(StatType.EnergyShield) : 0f;
    }
    
    public float GetCurrentEnergyShield() => currentEnergyShield;
    
    public float GetEnergyShieldPercentage()
    {
        float max = GetMaxEnergyShield();
        return max > 0 ? currentEnergyShield / max : 0f;
    }
    
    private float GetEffectiveRechargeRate()
    {
        float baseRate = rechargeRate;
        
        // Apply recharge rate modifiers from stats if any
        if (statSystem != null)
        {
            float regenBonus = statSystem.GetStat(StatType.EnergyShieldRegeneration);
            baseRate += regenBonus; // Assuming this is flat bonus, adjust if it's % based
        }
        
        return baseRate;
    }
    
    public bool IsRecharging() => isRecharging;
    
    // This was GetRechargeProgress, but it was identical to GetEnergyShieldPercentage.
    // Kept GetEnergyShieldPercentage as it's more descriptive of the value returned.

    // Renamed OnDamageTaken to OnExternalDamageTaken to avoid naming conflict with the method parameter.
    private void OnExternalDamageTaken(DamageInfo damageInfo) 
    {
        lastDamageTime = Time.time;
        if (isRecharging && !canRechargeWhileTakingDamage) // Stop recharge if taking damage and not allowed to recharge while doing so
        {
            StopRecharge();
        }
    }
    
    private void OnDestroy()
    {
        if (combatEntity != null)
        {
            combatEntity.OnDamageTaken -= OnExternalDamageTaken;
        }
        if (rechargeCoroutine != null) // Stop coroutine if object is destroyed
        {
            StopCoroutine(rechargeCoroutine);
        }
    }
} 