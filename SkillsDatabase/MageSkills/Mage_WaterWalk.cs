using System.Text;
using MagicHeim.MH_Interfaces;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Mage_WaterWalk : MH_Skill
{
    private static GameObject WaterWalk_Prefab;
    private static bool StaticBool_InWater;

    public Mage_WaterWalk()
    {
        _definition._InternalName = "Mage_Waterwalk";
        _definition.Name = "$mh_mage_waterwalk";
        _definition.Description = "$mh_mage_waterwalk_desc";

        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Manacost", 10f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Manacost", 2f,
            "Manacost amount (Max Lvl)");

        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 10,
            "Max Skill Level");

        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn",
            40, "Required Level");


        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 3,
            "Leveling Step");

        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Mage_WaterWalk_Icon");
        _definition.Video = "https://kg.sayless.eu/skills/MH_Mage_WaterWalk.mp4";
        WaterWalk_Prefab = MagicHeim.asset.LoadAsset<GameObject>("Mage_WaterWalk_Prefab");

        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
        this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
        this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScene_Awake_Patch
    {
        static void Postfix(ZNetScene __instance)
        {
            __instance.m_namedPrefabs[WaterWalk_Prefab.name.GetStableHashCode()] = WaterWalk_Prefab;
        }
    }

    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        
    }

    public override bool CanExecute()
    {
        return !Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Walk On Water, Toggle</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine("\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentManacost = this.CalculateSkillManacost(forLevel);
        builder.AppendLine($"Manacost (Per Second): {Math.Round(currentManacost, 1)}");

        if (Level < maxLevel && Level > 0)
        {
            float nextManacost = this.CalculateSkillManacost(forLevel + 1);
            float manacostDiff = nextManacost - currentManacost;
            double roundedManacostDiff = Math.Round(manacostDiff, 1);

            builder.AppendLine("\nNext Level:");
            builder.AppendLine(
                $"Manacost (Per Second): {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }


        return builder.ToString();
    }

    public override bool CanRightClickCast => true;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => new Color(0.44f, 1f, 0.92f);
}