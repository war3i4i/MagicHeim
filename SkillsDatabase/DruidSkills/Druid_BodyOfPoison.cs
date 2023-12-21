using System.Text;
using JetBrains.Annotations;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;

namespace MagicHeim.SkillsDatabase.DruidSkills;

public sealed class Druid_BodyOfPoison : MH_Skill
{
    public Druid_BodyOfPoison()
    {
        _definition._InternalName = "Druid_BodyOfPoison";
        _definition.Name = "$mh_druid_bodyofpoison";
        _definition.Description = "$mh_druid_bodyofpoison_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Heal Percent", 5f,
            "Percentage (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Heal Percent", 25f,
            "Percentage (Max Lvl)");
        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 7,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn", 
            30, "Required Level");
        
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_BodyOfPoison_Icon");
        CachedKey = _definition.Key;

        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 5,
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
        return "<color=red>Passive, Poison damage to healing convertion</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine("\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentValue = this.CalculateSkillValue(forLevel);
        builder.AppendLine($"Poison damage heal: {Math.Round(currentValue, 1)}%");
        if (Level < maxLevel && Level > 0)
        {
            float nextValue = this.CalculateSkillValue(forLevel + 1);
            float valueDiff = nextValue - currentValue;
 
            double roundedValueDiff = Math.Round(valueDiff, 1);

            builder.AppendLine("\nNext Level:");
            builder.AppendLine($"Poison damage heal: {Math.Round(nextValue, 1)}% <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
        }
        return builder.ToString();
    }
    
    [HarmonyPatch(typeof(Character),nameof(Character.ApplyDamage))]
    static class Player_GetTotalFoodValue_Patch
    {
        static void Prefix(HitData hit)
        {
            if (ClassManager.CurrentClass == Class.None || hit.m_damage.m_poison <= 0f) return;
            MH_Skill body = ClassManager.CurrentClassDef.GetSkill(CachedKey);
            if (body is { Level: > 0 })
            {
                float poisonDmg = hit.m_damage.m_poison;
                hit.m_damage.m_poison = 0f;
                float healAmount = poisonDmg * (body.CalculateSkillValue(body.Level) / 100f);
                Player.m_localPlayer.Heal(healAmount);
            }
        }
    }

    public override bool CanRightClickCast => false;
    public override bool IsPassive => true;
    public override CostType _costType => CostType.None;
    public override Color SkillColor => new Color(0.65f, 1f, 0.59f);
}

