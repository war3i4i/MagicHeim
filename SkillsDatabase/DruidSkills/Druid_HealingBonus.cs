using System.Text;
using JetBrains.Annotations;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Druid_HealingBonus : MH_Skill
{
    public Druid_HealingBonus()
    {
        _definition._InternalName = "Druid_HealingBonus";
        _definition.Name = "$mh_druid_healingbonus";
        _definition.Description = "$mh_druid_healingbonus_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Healing Bonus", 10f,
            "Healing bonus percent (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Healing Bonus", 100f,
            "Healing bonus percent (Max Lvl)");
        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 7,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn",
            1, "Required Level");
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_HealingBonus_Icon");
        CachedKey = _definition.Key;

        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 1, 
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
        return "<color=red>Passive, Received healing % increase</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine("\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentValue = this.CalculateSkillValue(forLevel);

        builder.AppendLine($"Received healing bonus: {Math.Round(currentValue, 1)}%");

        if (Level < maxLevel && Level > 0)
        {
            float nextValue = this.CalculateSkillValue(forLevel + 1);
            float valueDiff = nextValue - currentValue;

            double roundedValueDiff = Math.Round(valueDiff, 1);

            builder.AppendLine("\nNext Level:");
            builder.AppendLine($"Received healing bonus: {Math.Round(nextValue, 1)}% <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
        }


        return builder.ToString();
    }

    
    [HarmonyPatch(typeof(Character), nameof(Character.Heal))]
    static class Player_GetTotalFoodValue_Patch
    {
        static void Prefix(Character __instance, ref float hp)
        {
            if (ClassManager.CurrentClass == Class.None) return;
            if (__instance != Player.m_localPlayer) return;
            MH_Skill skill = ClassManager.CurrentClassDef.GetSkill(CachedKey);
            if (skill is not { Level: > 0 }) return;
            
            hp *= 1 + (skill.CalculateSkillValue(skill.Level) / 100f);
        }
    }

    public override bool CanRightClickCast => false;
    public override bool IsPassive => true;
    public override CostType _costType => CostType.None;
    public override Color SkillColor => new Color(0.48f, 1f, 0.6f);
}