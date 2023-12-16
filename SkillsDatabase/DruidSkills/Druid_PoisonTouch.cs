using System.Text;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;

namespace MagicHeim.SkillsDatabase.DruidSkills;

public sealed class Druid_PoisonTouch : MH_Skill
{
    public Druid_PoisonTouch()
    {
        _definition._InternalName = "Druid_PoisonTouch";
        _definition.Name = "$mh_druid_poisontouch";
        _definition.Description = "$mh_druid_poisontouch_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Poison Damage", 5f,
            "Value amount (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Poison Damage", 50f,
            "Value amount (Max Lvl)");
        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 10,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn",
            1, "Required Level");

        _definition.ExternalValues = new()
        {
            MagicHeim.config($"{_definition._InternalName}", "MIN Lvl Chance", 10f, "Chance"),
            MagicHeim.config($"{_definition._InternalName}", "MAX Lvl Chance", 100f, "Chance")
        };
        
        
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_PoisonTouch_Icon");
        CachedKey = _definition.Key;

        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 1,
            "Leveling Step"); 

        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
        this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
        this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);
    }

    private static int CachedKey;

    public override void Execute(Func<bool> Cond){}
 
    public override bool CanExecute()
    {
        return false;
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Passive, Damage bonus with chance</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine("\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentValue = this.CalculateSkillValue(forLevel);
        float externalValue = this.CalculateSkillExternalValue(0,forLevel);
        float chance = this.CalculateSkillExternalValue(0,forLevel);
        
        builder.AppendLine($"Damage bonus: {Math.Round(currentValue, 1)}");
        builder.AppendLine($"Chance: {Math.Round(chance, 1)}%");

        if (Level < maxLevel && Level > 0)
        {
            float nextValue = this.CalculateSkillValue(forLevel + 1);
            float nextExternalValue = this.CalculateSkillExternalValue(0, forLevel + 1);
            float valueDiff = nextValue - currentValue;
            float externalValueDiff = nextExternalValue - externalValue;
 
            double roundedValueDiff = Math.Round(valueDiff, 1);

            builder.AppendLine("\nNext Level:");
            builder.AppendLine($"Damage bonus: {Math.Round(nextValue, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
            builder.AppendLine($"Chance: {Math.Round(nextExternalValue, 1)}% <color=green>({(roundedValueDiff > 0 ? "+" : "")}{Math.Round(externalValueDiff, 1)})</color>");
        }

        return builder.ToString();
    }
 

    [HarmonyPatch(typeof(Character),nameof(Character.Damage))]
    static class Character_Damage_Patch
    {
        private static readonly HashSet<Skills.SkillType> INCLUDE = 
            [
                Skills.SkillType.Swords, Skills.SkillType.Axes, Skills.SkillType.Clubs, Skills.SkillType.Polearms, 
                Skills.SkillType.Pickaxes, Skills.SkillType.Crossbows, Skills.SkillType.Spears, Skills.SkillType.Unarmed
            ];
        
        static void Prefix(ref HitData hit)
        {
            if (ClassManager.CurrentClass == Class.None) return;
            if (!Player.m_localPlayer || hit.m_attacker != Player.m_localPlayer.GetZDOID()) return; 
            if (!INCLUDE.Contains(hit.m_skill)) return;
            var skill = ClassManager.CurrentClassDef.GetSkill(CachedKey);
            if (skill is not { Level: > 0 }) return;
            float chance = skill.CalculateSkillExternalValue(0, skill.Level);
            if (UnityEngine.Random.Range(0f, 100f) > chance) return;
            float dmg = skill.CalculateSkillValue(skill.Level);
            hit.m_damage.m_poison = dmg;
        }
    }


    public override bool CanRightClickCast => false;
    public override bool IsPassive => true;
    public override CostType _costType => CostType.None;
    public override Color SkillColor => new Color(0.33f, 1f, 0.38f);
}