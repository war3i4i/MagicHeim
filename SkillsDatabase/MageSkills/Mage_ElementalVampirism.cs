using System.Text;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Mage_ElementalVampirism : MH_Skill
{
    public Mage_ElementalVampirism()
    {
        _definition._InternalName = "Mage_Elementalvampirism";
        _definition.Name = "$mh_mage_elementalvampirism";
        _definition.Description = "$mh_mage_elementalvampirism_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Elemental Damage Vamp (Percentage)", 1f,
            "Value amount (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Elemental Damage Vamp (Percentage)", 4f,
            "Value amount (Max Lvl)");
        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Max Level", 7,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Required Level To Learn",
            66, "Required Level");
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Mage_Elementalvampirism");
        CachedKey = _definition.Key;

        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            $"Leveling Step", 2,
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
        return "<color=red>Passive, Vamp (heals HP from % damage dealt)</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine($"\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentValue = this.CalculateSkillValue(forLevel);

        builder.AppendLine($"Elemental Damage Vamp: {Math.Round(currentValue, 1)}%");

        if (Level < maxLevel && Level > 0)
        {
            float nextValue = this.CalculateSkillValue(forLevel + 1);
            float valueDiff = nextValue - currentValue;

            var roundedValueDiff = Math.Round(valueDiff, 1);

            builder.AppendLine($"\nNext Level:");
            builder.AppendLine(
                $"Elemental Damage Vamp: {Math.Round(nextValue, 1)}% <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
        }


        return builder.ToString();
    }


    //action
    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    static class Character_Damage_Patch
    {
        public static float SimulateDamageElemental(HitData other, Character c)
        {
            var hit = other.Clone();
            if (c.m_baseAI != null && !c.m_baseAI.IsAlerted() && hit.m_backstabBonus > 1f &&
                Time.time - c.m_backstabTime > 300f) hit.ApplyModifier(hit.m_backstabBonus);
            if (c.IsStaggering() && !c.IsPlayer()) hit.ApplyModifier(2f);
            var damageModifiers = c.GetDamageModifiers();
            hit.ApplyResistance(damageModifiers, out _);
            return hit.GetTotalDamage();
        }

        static void Prefix(Character __instance, HitData hit)
        {
            if (hit.m_skill != Skills.SkillType.ElementalMagic || ClassManager.CurrentClass == Class.None) return;
            var skillDef = ClassManager.CurrentClassDef.GetSkill(CachedKey);
            if (skillDef == null || skillDef.Level <= 0 || hit.GetAttacker() != Player.m_localPlayer) return;
            var dmg = SimulateDamageElemental(hit, __instance);
            var heal = dmg * (skillDef.CalculateSkillValue(skillDef.Level) / 100);
            Player.m_localPlayer.Heal(heal);
        }
    }


    public override bool CanRightClickCast => false;
    public override bool IsPassive => true;
    public override CostType _costType => CostType.None;
    public override Color SkillColor => new Color(0.4f, 0.89f, 1f);
}