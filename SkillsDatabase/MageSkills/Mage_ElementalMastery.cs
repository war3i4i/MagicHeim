using System.Text;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Mage_ElementalMastery : MH_Skill
{
    public Mage_ElementalMastery()
    {
        _definition._InternalName = "Mage_Elementalmastery";
        _definition.Name = "$mh_mage_elementalmastery";
        _definition.Description = "$mh_mage_elementalmastery_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MIN Skill Level Bonus", 10f,
            "Value amount (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MAX Skill Level Bonus", 20f,
            "Value amount (Max Lvl)");
        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 7,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn",
            58, "Required Level");
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Mage_ElementalMastery");
        CachedKey = _definition.Key;

        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 3,
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
        return "<color=red>Passive, Skill Level Bonus</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine("\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentValue = this.CalculateSkillValue(forLevel);

        builder.AppendLine($"Elemental Magic Skill Level Bonus: {Math.Round(currentValue, 1)}");

        if (Level < maxLevel && Level > 0)
        {
            float nextValue = this.CalculateSkillValue(forLevel + 1);
            float valueDiff = nextValue - currentValue;

            double roundedValueDiff = Math.Round(valueDiff, 1);

            builder.AppendLine("\nNext Level:");
            builder.AppendLine(
                $"Elemental Magic Skill Level Bonus: {Math.Round(nextValue, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
        }


        return builder.ToString();
    }


    //action
    [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifySkillLevel))]
    static class ModifyDamage
    {
        static void Postfix(SEMan __instance, Skills.SkillType skill, ref float level)
        {
            if (skill != Skills.SkillType.ElementalMagic || ClassManager.CurrentClass == Class.None) return;
            MH_Skill skillDef = ClassManager.CurrentClassDef.GetSkill(CachedKey);
            if (skillDef is not { Level: > 0 }) return;
            level += skillDef.CalculateSkillValue(skillDef.Level);
        }
    }


    public override bool CanRightClickCast => false;
    public override bool IsPassive => true;
    public override CostType _costType => CostType.None;
    public override Color SkillColor => new Color(0.18f, 0.72f, 1f);
}