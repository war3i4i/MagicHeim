using System.Text;
using JetBrains.Annotations;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Druid_InnerControl : MH_Skill
{
    public Druid_InnerControl()
    {
        _definition._InternalName = "Druid_InnerControl";
        _definition.Name = "$mh_druid_innercontrol";
        _definition.Description = "$mh_druid_innercontrol_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MIN Stamina Bonus", 5f,
            "Stamina bonus (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MAX Stamina Bonus", 50f,
            "Stamina bonus (Max Lvl)");
        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 7,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn",
            10, "Required Level");
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_InnerControl_Icon");
        CachedKey = _definition.Key;

        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 9, 
            "Leveling Step");

        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
        this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
        this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);
    }

    private static int CachedKey;

    public override void Execute(Func<bool> Cond)
    {
    }

    public override bool CanExecute()
    {
        return false;
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Passive, Max stamina bonus</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine("\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentValue = this.CalculateSkillValue(forLevel);

        builder.AppendLine($"Max stamina bonus: {Math.Round(currentValue, 1)}");

        if (Level < maxLevel && Level > 0)
        {
            float nextValue = this.CalculateSkillValue(forLevel + 1);
            float valueDiff = nextValue - currentValue;

            double roundedValueDiff = Math.Round(valueDiff, 1);

            builder.AppendLine("\nNext Level:");
            builder.AppendLine(
                $"Max stamina bonus: {Math.Round(nextValue, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
        }


        return builder.ToString();
    }

    
    [HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
    static class Player_GetTotalFoodValue_Patch
    {
        static void Postfix(ref float stamina)
        {
            if (ClassManager.CurrentClass == Class.None) return;
            MH_Skill skill = ClassManager.CurrentClassDef.GetSkill(CachedKey);
            if (skill is not { Level: > 0 }) return;
            stamina += skill.CalculateSkillValue(skill.Level);
        }
    }

    public override bool CanRightClickCast => false;
    public override bool IsPassive => true;
    public override CostType _costType => CostType.None;
    public override Color SkillColor => new Color(0.48f, 1f, 0.6f);
}