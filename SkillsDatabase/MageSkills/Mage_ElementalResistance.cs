using System.Text;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Mage_ElementalResistance : MH_Skill
{
    public Mage_ElementalResistance()
    {
        _definition._InternalName = "Mage_Elementalresistance";
        _definition.Name = "$mh_mage_elementalresistance";
        _definition.Description = "$mh_mage_elementalresistance_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MIN Elemental Damage Resistance (Percentage)", 3f,
            "Value amount (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MAX Elemental Damage Resistance (Percentage)", 30f,
            "Value amount (Max Lvl)");
        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 7,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn",
            48, "Required Level");
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Mage_ElementalResistance");
        CachedKey = _definition.Key;

        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 4,
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
        return "<color=red>Passive, Elemental Damage Damage Reduction (%)</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine("\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentValue = this.CalculateSkillValue(forLevel);

        builder.AppendLine($"Elemental Damage Damage Reduction: {Math.Round(currentValue, 1)}%");

        if (Level < maxLevel && Level > 0)
        {
            float nextValue = this.CalculateSkillValue(forLevel + 1);
            float valueDiff = nextValue - currentValue;

            double roundedValueDiff = Math.Round(valueDiff, 1);

            builder.AppendLine("\nNext Level:");
            builder.AppendLine(
                $"Elemental Damage Damage Reduction: {Math.Round(nextValue, 1)}% <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
        }


        return builder.ToString();
    }


    //action
    [HarmonyPatch(typeof(SEMan), nameof(SEMan.OnDamaged))]
    static class ModifyDamage
    {
        static void Postfix(SEMan __instance, ref HitData hit)
        {
            if (ClassManager.CurrentClass == Class.None || __instance.m_character != Player.m_localPlayer) return;
            MH_Skill skillDef = ClassManager.CurrentClassDef.GetSkill(CachedKey);
            if (skillDef is not { Level: > 0 }) return;
            hit.m_damage.m_fire *= Mathf.Clamp01(1 - skillDef.CalculateSkillValue(skillDef.Level) / 100);
            hit.m_damage.m_frost *= Mathf.Clamp01(1 - skillDef.CalculateSkillValue(skillDef.Level) / 100);
            hit.m_damage.m_lightning *= Mathf.Clamp01(1 - skillDef.CalculateSkillValue(skillDef.Level) / 100);
        }
    }


    public override bool CanRightClickCast => false;
    public override bool IsPassive => true;
    public override CostType _costType => CostType.None;
    public override Color SkillColor => new Color(0.18f, 0.72f, 1f);
}