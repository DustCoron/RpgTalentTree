using UnityEngine;

public class FireballSpell : MonoBehaviour
{
    public RPG_SpellStats spellStats = new RPG_SpellStats
    {
        damage = 50,
        damageType = RPG_DamageTypes.Fire,
        cooldown = 1.5f,
        manaCost = 10,
        castTime = 0.5f,
        projectile = new RPG_ProjectileStats
        {
            speed = 12f,
            lifetime = 5f,
            pierce = 0,
            bounce = 0,
            chain = 0
        }
    };

    public GameObject projectilePrefab;
    float lastCastTime = -Mathf.Infinity;

    public bool CanCast()
    {
        return Time.time >= lastCastTime + spellStats.cooldown;
    }

    public void Cast(Vector3 direction)
    {
        if (!CanCast() || projectilePrefab == null) return;
        lastCastTime = Time.time;
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.LookRotation(direction));
        var projectile = proj.AddComponent<FireballProjectile>();
        projectile.Initialize(spellStats);
    }
}

public class FireballProjectile : MonoBehaviour
{
    RPG_SpellStats stats;
    Rigidbody rb;

    public void Initialize(RPG_SpellStats s)
    {
        stats = s;
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.velocity = transform.forward * stats.projectile.speed;
        Destroy(gameObject, stats.projectile.lifetime);
    }

    void OnTriggerEnter(Collider other)
    {
        var damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(stats.damage, stats.damageType);
            Destroy(gameObject);
        }
    }
}

public interface IDamageable
{
    void TakeDamage(int amount, RPG_DamageTypes type);
}
