using UnityEngine;
using UnityEngine.UIElements;

public class RPG_SpellTooltipUI : MonoBehaviour
{
    public VisualTreeAsset tooltipUXML;
    public StyleSheet tooltipUSS;
    private VisualElement root;
    private VisualElement tooltip;

    void Awake()
    {
        var uiDoc = GetComponent<UIDocument>();
        root = uiDoc.rootVisualElement;
        if (tooltipUSS != null) root.styleSheets.Add(tooltipUSS);
        if (tooltipUXML != null)
        {
            tooltip = tooltipUXML.CloneTree().Q<VisualElement>("SpellTooltip");
            tooltip.style.display = DisplayStyle.None;
            root.Add(tooltip);
        }
    }

    /// <summary>
    /// Show spell tooltip with calculated effective values
    /// </summary>
    public void ShowTooltip(SpellData spell, Vector2 screenPosition, AdvancedStatSystem statSystem = null, string overrideName = null)
    {
        if (tooltip == null) return;
        
        string spellName = overrideName ?? spell.spellName;
        
        // Use effective values if stat system is provided, otherwise use base values
        float damage = statSystem != null ? spell.GetEffectiveDamage(statSystem) : spell.baseDamage;
        float cooldown = statSystem != null ? spell.GetEffectiveCooldown(statSystem) : spell.baseCooldown;
        int manaCost = statSystem != null ? spell.GetEffectiveManaCost(statSystem) : spell.baseManaCost;
        float castTime = statSystem != null ? spell.GetEffectiveCastTime(statSystem) : spell.baseCastTime;
        
        // Update tooltip content
        tooltip.Q<Label>("SpellName").text = spellName;
        tooltip.Q<Label>("Damage").text = $"Damage: {damage:F0}";
        tooltip.Q<Label>("Type").text = $"Type: {spell.damageType}";
        tooltip.Q<Label>("Cooldown").text = $"Cooldown: {cooldown:F1}s";
        tooltip.Q<Label>("ManaCost").text = $"Mana Cost: {manaCost}";
        tooltip.Q<Label>("CastTime").text = $"Cast Time: {castTime:F1}s";
        
        // Handle projectile information
        var projectileLabel = tooltip.Q<Label>("Projectile");
        if (projectileLabel != null)
        {
            if (spell.isProjectile && spell.projectileData != null)
            {
                float projectileSpeed = statSystem != null ? 
                    spell.projectileData.GetEffectiveSpeed(statSystem) : 
                    spell.projectileData.baseSpeed;
                projectileLabel.text = $"Projectile Speed: {projectileSpeed:F0}";
                projectileLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                projectileLabel.style.display = DisplayStyle.None;
            }
        }
        
        // Position and show tooltip
        tooltip.style.left = screenPosition.x;
        tooltip.style.top = screenPosition.y;
        tooltip.style.display = DisplayStyle.Flex;
    }
    
    /// <summary>
    /// Overload for backward compatibility
    /// </summary>
    public void ShowTooltip(SpellData spell, Vector2 screenPosition, string spellName = null)
    {
        ShowTooltip(spell, screenPosition, null, spellName);
    }

    public void HideTooltip()
    {
        if (tooltip != null)
            tooltip.style.display = DisplayStyle.None;
    }
} 