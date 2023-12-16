using System.Text;
using JetBrains.Annotations;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;

namespace MagicHeim.SkillsDatabase.DruidSkills;

public sealed class Druid_HardenSkin : MH_Skill
{
    public Druid_HardenSkin()
    {
        _definition._InternalName = "Druid_HardenSkin";
        _definition.Name = "$mh_druid_hardenskin";
        _definition.Description = "$mh_druid_hardenskin_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Chance", 5f,
            "Chance (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Chance", 35f,
            "Chance (Max Lvl)");
        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 10,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn",
            1, "Required Level");
        
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_HardenSkin_Icon");
        CachedKey = _definition.Key;

        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 1,
            "Leveling Step"); 

        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
        this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
        this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);
    } 

    private static int CachedKey;

    public override void Execute(Func<bool> Cond){}
 
    public override bool CanExecute()
    {
        return false;
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Passive, Chance to ignore damage</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine("\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentValue = this.CalculateSkillValue(forLevel);
        builder.AppendLine($"Chance to ignore damage: {Math.Round(currentValue, 1)}%");
        if (Level < maxLevel && Level > 0)
        {
            float nextValue = this.CalculateSkillValue(forLevel + 1);
            float valueDiff = nextValue - currentValue;
 
            double roundedValueDiff = Math.Round(valueDiff, 1);

            builder.AppendLine("\nNext Level:");
            builder.AppendLine($"Chance to ignore damage: {Math.Round(nextValue, 1)}% <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
        }


        return builder.ToString();
    }
 
    [HarmonyPatch(typeof(Character),nameof(Character.RPC_Damage))]
    private static class Character_RPC_Damage_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Character __instance, HitData hit)
        {
            if (ClassManager.CurrentClass == Class.None) return;
            if (__instance != Player.m_localPlayer) return;
            float totalDmg = hit.GetTotalBlockableDamage();
            if (totalDmg < 1f) return;
            MH_Skill skill = ClassManager.CurrentClassDef.GetSkill(CachedKey);
            if (skill is not { Level: > 0 }) return;
            float chance = skill.CalculateSkillValue(skill.Level);
            if (UnityEngine.Random.Range(0f, 100f) > chance) return;
            hit.ApplyModifier(0f);
            Utils.FloatingText("<color=#791f87>HS</color>");
        }
    }


    public override bool CanRightClickCast => false;
    public override bool IsPassive => true;
    public override CostType _costType => CostType.None;
    public override Color SkillColor => new Color(1f, 0.38f, 0.95f);
}