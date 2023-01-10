using MagicHeim.MH_Classes;
using MagicHeim.MH_Enums;
using MagicHeim.SkillsDatabase.MageSkills;

namespace MagicHeim.MH_Interfaces;

public abstract class MH_Skill
{
    [Flags]
    public enum CostType
    {
        None = 0,
        Eitr = 1,
        Stamina = 2,
        Health = 4,
    }

    protected internal MH_Skill Clone() => (MH_Skill)MemberwiseClone();

    protected readonly MH_SkillDefinition _definition = new();
    public MH_SkillDefinition Definition => _definition;
    public virtual string Name => Localization.instance.Localize(_definition.Name);
    public string Animation => _definition.Animation;
    public float AnimationTime => _definition.AnimationTime;
    public int Key => _definition.Key;
    public Sprite Icon => _definition.Icon;
    public string Video => _definition.Video;
    public string Description => Localization.instance.Localize(_definition.Description);
    public int Level;
    public int RequiredLevel => _definition.RequiredLevel.Value;
    public int LevelingStep => _definition.LevelingStep != null ? _definition.LevelingStep.Value + 1 : 1;
    public float Value => this.CalculateSkillValue();
    public int MaxLevel => _definition.MaxLevel.Value;

    public string RequiredItemToUpgrade => _definition.RequiredItemFirstHalfToUpgrade != null
        ? _definition.RequiredItemFirstHalfToUpgrade.Value
        : string.Empty;

    public int RequiredItemAmountToUpgrade => _definition.RequiredItemFirstHalfAmountToUpgrade != null
        ? _definition.RequiredItemFirstHalfAmountToUpgrade.Value
        : 0;

    public float RequiredItemAmountToUpgrade_Step => _definition.RequiredItemFirstHalfAmountToUpgrade_Step != null
        ? _definition.RequiredItemFirstHalfAmountToUpgrade_Step.Value
        : 0;
    
    public string RequiredItemToUpgradeSecondHalf => _definition.RequiredItemSecondHalfToUpgrade != null 
        ? _definition.RequiredItemSecondHalfToUpgrade.Value 
        : string.Empty;
    
    public int RequiredItemAmountToUpgradeSecondHalf => _definition.RequiredItemSecondHalfAmountToUpgrade != null
        ? _definition.RequiredItemSecondHalfAmountToUpgrade.Value
        : 0;
    
    public float RequiredItemAmountToUpgradeSecondHalf_Step => _definition.RequiredItemSecondHalfAmountToUpgrade_Step != null
        ? _definition.RequiredItemSecondHalfAmountToUpgrade_Step.Value
        : 0;
    
    public string RequiredItemToUpgradeFinal =>  _definition.RequiredItemFinalToUpgrade != null
        ? _definition.RequiredItemFinalToUpgrade.Value
        : string.Empty;
    
    public int RequiredItemAmountToUpgradeFinal => _definition.RequiredItemFinalAmountToUpgrade != null
        ? _definition.RequiredItemFinalAmountToUpgrade.Value
        : 0;
    
    
    public string InternalName => _definition._InternalName;

    public string ExternalDescription()
    {
        string result = $"\nMax Level: <color=yellow>{MaxLevel}</color>";
        if (this.Level >= this.MaxLevel) return result;
        var requiredLevel = this.RequiredLevel + (this.LevelingStep * this.Level);
        bool canUpgrade = ClassManager.Level >= requiredLevel;
        result += $"\n\nLeveling Step: <color=yellow>{this.LevelingStep}</color>\n(Can be {(this.Level > 0 ? "upgraded" : "learned")} on LVL: <color={(canUpgrade ? "green" : "red")}>{requiredLevel}</color>)";
        return result;
    }


    public void SetLevel(int level)
    {
        Level = level;
    }

    public bool TryUseCost()
    {
        if (Player.m_debugMode) return true;
        Player p = Player.m_localPlayer;
        var costType = _costType;
        var cost = this.CalculateSkillManacost();
        if (costType == CostType.Eitr)
        {
            if (!p.HaveEitr(cost))
            {
                Hud.instance.EitrBarEmptyFlash();
                return false;
            }
            else
            {
                p.UseEitr(cost);
                return true;
            }
        }

        if (costType == CostType.Stamina)
        {
            if (!p.HaveStamina(cost))
            {
                Hud.instance.StaminaBarEmptyFlash();
                return false;
            }
            else
            {
                p.UseStamina(cost);
                return true;
            }
        }

        if (costType == CostType.Health)
        {
            if (!p.HaveHealth(cost))
            {
                Hud.instance.FlashHealthBar();
                return false;
            }
            else
            {
                p.UseHealth(cost);
                return true;
            }
        }

        return true;
    }


    protected float _Internal_Cooldown;
    private float _Internal_Cooldown_Max;
    private Coroutine _Internal_Cooldown_Coroutine;

    public void StartCooldown(float time, bool skipChecks = false)
    {
        if (_Internal_Cooldown_Coroutine != null) MagicHeim._thistype.StopCoroutine(_Internal_Cooldown_Coroutine);
        _Internal_Cooldown = 0;

        if (!skipChecks)
        {
            Mage_MasterOfTime.TryToCheckSkill(ref time);
        }

        if (time <= 0) return;
        _Internal_Cooldown_Coroutine = MagicHeim._thistype.StartCoroutine(Cooldown(time));
    }

    public float GetCooldown() => _Internal_Cooldown;
    public float GetMaxCooldown() => _Internal_Cooldown_Max;

    private IEnumerator Cooldown(float cooldown)
    {
        _Internal_Cooldown_Max = cooldown;
        _Internal_Cooldown = cooldown;
        while (_Internal_Cooldown > 0)
        {
            _Internal_Cooldown -= Time.deltaTime;
            yield return null;
        }

        _Internal_Cooldown = 0;
    }

    public bool Toggled = false;

    public abstract void Execute(Func<bool> Cond);
    public abstract bool CanExecute();
    public abstract string BuildDescription();
    public abstract string GetSpecialTags();
    public abstract Class PreferableClass { get; }
    public abstract Color SkillColor { get; }
    public abstract bool IsPassive { get; }
    public abstract CostType _costType { get; }
}