using System.Text;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Mage_MasterOfTime : MH_Skill
{
    public Mage_MasterOfTime()
    {
        _definition._InternalName = "Mage_Masteroftime";
        _definition.Name = "$mh_mage_masteroftime";
        _definition.Description = "$mh_mage_masteroftime_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Chance To Reset Skill CD (Percentage)", 2f,
            "Value amount (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Chance To Reset Skill CD (Percentage)", 45f,
            "Value amount (Max Lvl)");
        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Max Level", 7,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Required Level To Learn",
            70, "Required Level");
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Mage_MasterOfTime");
        CachedKey = _definition.Key;

        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            $"Leveling Step", 1,
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
        return "<color=red>Passive, Change to reset CD</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine($"\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentValue = this.CalculateSkillValue(forLevel);

        builder.AppendLine($"Chance to reset skill cooldown: {Math.Round(currentValue, 1)}%");

        if (Level < maxLevel && Level > 0)
        {
            float nextValue = this.CalculateSkillValue(forLevel + 1);
            float valueDiff = nextValue - currentValue;

            var roundedValueDiff = Math.Round(valueDiff, 1);

            builder.AppendLine($"\nNext Level:");
            builder.AppendLine(
                $"Chance to reset skill cooldown: {Math.Round(nextValue, 1)}% <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
        }


        return builder.ToString();
    }

    //action
    public static void TryToCheckSkill(ref float cd)
    {
        if (ClassManager.CurrentClass == Class.None) return;
        var skillDef = ClassManager.CurrentClassDef.GetSkill(CachedKey);
        if (skillDef == null || skillDef.Level <= 0) return;
        var chance = skillDef.CalculateSkillValue(skillDef.Level);
        var random = UnityEngine.Random.Range(0, 100);
        if (random <= chance)
        {
            cd = 0f;
        }
    }


    public override bool CanRightClickCast => false;
    public override bool IsPassive => true;
    public override CostType _costType => CostType.None;
    public override Color SkillColor => new Color(0.18f, 0.72f, 1f);
}