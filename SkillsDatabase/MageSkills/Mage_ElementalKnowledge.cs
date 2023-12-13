using System.Text;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Mage_ElementalKnowledge : MH_Skill
{
    public Mage_ElementalKnowledge()
    {
        _definition._InternalName = "Mage_Elementalknowledge";
        _definition.Name = "$mh_mage_elementalknowledge";
        _definition.Description = "$mh_mage_elementalknowledge_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Elemental Damage Bonus (Percentage)", 5f,
            "Value amount (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Elemental Damage Bonus (Percentage)", 25f,
            "Value amount (Max Lvl)");
        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Max Level", 7,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Required Level To Learn",
            6, "Required Level");
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Mage_ElementalKnowledge");
        CachedKey = _definition.Key;

        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            $"Leveling Step", 9,
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
        return "<color=red>Passive, Elemental Damage Bonus (%)</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine($"\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentValue = this.CalculateSkillValue(forLevel);

        builder.AppendLine($"Elemental Magic Damage Bonus: {Math.Round(currentValue, 1)}%");

        if (Level < maxLevel && Level > 0)
        {
            float nextValue = this.CalculateSkillValue(forLevel + 1);
            float valueDiff = nextValue - currentValue;

            var roundedValueDiff = Math.Round(valueDiff, 1);

            builder.AppendLine($"\nNext Level:");
            builder.AppendLine(
                $"Elemental Magic Damage Bonus: {Math.Round(nextValue, 1)}% <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
        }


        return builder.ToString();
    }


    //action
    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    static class ModifyDamage
    {
        static void Prefix(ref HitData hit)
        {
            if (hit.m_skill != Skills.SkillType.ElementalMagic || ClassManager.CurrentClass == Class.None ||
                hit.GetAttacker() != Player.m_localPlayer) return;
            var skillDef = ClassManager.CurrentClassDef.GetSkill(CachedKey);
            if (skillDef == null || skillDef.Level <= 0) return;
            hit.ApplyModifier(1 + skillDef.CalculateSkillValue() / 100f);
        }
    }


    public override bool CanRightClickCast => false;
    public override bool IsPassive => true;
    public override CostType _costType => CostType.None;
    public override Color SkillColor => new Color(0.18f, 0.72f, 1f);
}