using System.Text;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;
using Logger = MagicHeim_Logger.Logger;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Mage_ManaFlow : MH_Skill
{
    public Mage_ManaFlow()
    {
        _definition._InternalName = "Mage_Manaflow";
        _definition.Name = "$mh_mage_manaflow";
        _definition.Description = "$mh_mage_manaflow_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Eitr Bonus", 100f,
            "Value amount (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Eitr Bonus", 325f,
            "Value amount (Max Lvl)");
        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 7,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn",
            1, "Required Level");
        Level = 1;
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Mage_ManaFlow");
        CachedKey = _definition.Key;

        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 10,
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
        return "<color=red>Passive, Max Eitr Amount Bonus</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine("\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentValue = this.CalculateSkillValue(forLevel);

        builder.AppendLine($"Max Eitr Bonus: {Math.Round(currentValue, 1)}");

        if (Level < maxLevel && Level > 0)
        {
            float nextValue = this.CalculateSkillValue(forLevel + 1);
            float valueDiff = nextValue - currentValue;

            double roundedValueDiff = Math.Round(valueDiff, 1);

            builder.AppendLine("\nNext Level:");
            builder.AppendLine(
                $"Max Eitr Bonus: {Math.Round(nextValue, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
        }


        return builder.ToString();
    }


    //action
    [HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
    static class Player_GetTotalFoodValue_Patch
    {
        static void Postfix(ref float eitr)
        {
            if (ClassManager.CurrentClass == Class.None) return;
            MH_Skill skill = ClassManager.CurrentClassDef.GetSkill(CachedKey);
            if (skill is not { Level: > 0 }) return;
            eitr += skill.Value;
        }
    }


    public override bool CanRightClickCast => false;
    public override bool IsPassive => true;
    public override CostType _costType => CostType.None;
    public override Color SkillColor => new Color(0.18f, 0.72f, 1f);
}