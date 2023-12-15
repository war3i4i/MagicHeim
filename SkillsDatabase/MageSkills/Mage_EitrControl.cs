using System.Text;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Mage_EitrControl : MH_Skill
{
    public Mage_EitrControl()
    {
        _definition._InternalName = "Mage_Eitrcontrol";
        _definition.Name = "$mh_mage_eitrcontrol";
        _definition.Description = "$mh_mage_eitrcontrol_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MIN Eitr Regen Speed Bonus (Percentage)", 20f,
            "Value amount (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MAX Eitr Regen Speed Bonus (Percentage)", 100f,
            "Value amount (Max Lvl)");
        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 7,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn",
            16, "Required Level");
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Mage_EitrControl");
        CachedKey = _definition.Key;

        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 7,
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
        return "<color=red>Passive, Eitr Regen Bonus</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine("\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentValue = this.CalculateSkillValue(forLevel);

        builder.AppendLine($"Eitr Regen Bonus: {Math.Round(currentValue, 1)}%");

        if (Level < maxLevel && Level > 0)
        {
            float nextValue = this.CalculateSkillValue(forLevel + 1);
            float valueDiff = nextValue - currentValue;

            double roundedValueDiff = Math.Round(valueDiff, 1);

            builder.AppendLine("\nNext Level:");
            builder.AppendLine(
                $"Eitr Regen Bonus: {Math.Round(nextValue, 1)}% <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
        }


        return builder.ToString();
    }


    //action
    [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyEitrRegen))]
    static class ModifyDamage
    {
        static void Postfix(SEMan __instance, ref float eitrMultiplier)
        {
            if (ClassManager.CurrentClass == Class.None || __instance.m_character != Player.m_localPlayer) return;
            MH_Skill skillDef = ClassManager.CurrentClassDef.GetSkill(CachedKey);
            if (skillDef is not { Level: > 0 }) return;
            eitrMultiplier += skillDef.CalculateSkillValue() / 100;
        }
    }


    public override bool CanRightClickCast => false;
    public override bool IsPassive => true;
    public override CostType _costType => CostType.None;
    public override Color SkillColor => new Color(1f, 0.32f, 0.91f);
}