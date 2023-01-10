using System.Text;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Classes;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;
using MagicHeim.UI_s;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Mage_ElementalTimescale : MH_Skill
{
    public Mage_ElementalTimescale()
    {
        _definition._InternalName = "Mage_Elementaltimescale";
        _definition.Name = "$mh_mage_elementaltimescale";
        _definition.Description = "$mh_mage_elementaltimescale_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Skills Cooldown Time Reduction (Percentage)", 5f,
            "Value amount (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Skills Cooldown Time Reduction (Percentage)", 20f,
            "Value amount (Max Lvl)");
        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Max Level", 7,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Required Level To Learn",
            28, "Required Level");
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Mage_ElementalTimescale");
        CachedKey = _definition.Key;
        
        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            $"Leveling Step", 6,
            "Leveling Step");
        
        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);
    }

    public static int CachedKey;

    public override void Execute(Func<bool> Cond)
    {
    }

    public override bool CanExecute()
    {
        return false;
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Passive, Skills Cooldown Time Reduction (%)</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine($"\n");

        int maxLevel = this.MaxLevel;
        int forLevel = this.Level > 0 ? this.Level : 1;
        float currentValue = this.CalculateSkillValue(forLevel);

        builder.AppendLine($"Skills Cooldown Time Reduction: {Math.Round(currentValue, 1)}%");

        if (this.Level < maxLevel && this.Level > 0)
        {
            float nextValue = this.CalculateSkillValue(forLevel + 1);
            float valueDiff = nextValue - currentValue;

            var roundedValueDiff = Math.Round(valueDiff, 1);

            builder.AppendLine($"\nNext Level:");
            builder.AppendLine($"Skills Cooldown Time Reduction: {Math.Round(nextValue, 1)}% <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
        }

        

        return builder.ToString();
    }


    //action
    public static void TryToCheckSkill(ref float cooldown)
    {
        if (ClassManager.CurrentClass == Class.None) return;
        var skillDef = ClassManager.CurrentClassDef.GetSkill(CachedKey);
        if (skillDef == null || skillDef.Level <= 0) return;
        cooldown *= Mathf.Clamp01(1 - skillDef.CalculateSkillValue() / 100f);
    }


    public override Class PreferableClass => Class.Mage;
    public override bool IsPassive => true;
    public override CostType _costType => CostType.None;
    public override Color SkillColor => new Color(0.18f, 0.72f, 1f);
}