using System.Text;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Classes;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;
using MagicHeim.UI_s;
using Logger = MagicHeim_Logger.Logger;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Druid_NatureBuff : MH_Skill
{
    private static GameObject Prefab;

    public Druid_NatureBuff()
    {
        _definition._InternalName = "Druid_NatureBuff";
        _definition.Name = "$mh_druid_naturebuff";
        _definition.Description = "$mh_druid_naturebuff_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl MoveSpeed (Percentage)", 20f,
            "Heal amount (Min Lvl)");

        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl MoveSpeed (Percentage)", 100f,
            "Heal amount (Max Lvl)");

        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Manacost", 20f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Manacost", 55f,
            "Manacost amount (Max Lvl)");

        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Cooldown", 60f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Cooldown", 20f,
            "Cooldown amount (Max Lvl)");

        _definition.MinLvlDuration = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Duration", 20f,
            "Duration amount (Min Lvl)");
        _definition.MaxLvlDuration = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Duration", 60f,
            "Duration amount (Max Lvl)");

        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Max Level", 10,
            "Max Skill Level");


        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Required Level To Learn",
            10, "Required Level");

        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            $"Leveling Step", 4,
            "Leveling Step");


        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_NatureBuff_Icon");
        _definition.Video = "https://kg-dev.xyz/skills/MH_Druid_Heal.mp4";
        _definition.Animation = ClassAnimationReplace.MH_AnimationNames[ClassAnimationReplace.MH_Animation.MageSlam];
        _definition.AnimationTime = 0.8f;
        Prefab = MagicHeim.asset.LoadAsset<GameObject>("Druid_NatureBuff_Buff");
        CachedIcon = _definition.Icon; 
        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
        this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
        this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);
    }

    private static Sprite CachedIcon;

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScene_Awake_Patch
    {
        static void Postfix(ZNetScene __instance)
        {
            __instance.m_namedPrefabs[Prefab.name.GetStableHashCode()] = Prefab;
        }
    }


    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        Player p = Player.m_localPlayer;
        List<Player> list = Player.GetAllPlayers().Where(p =>
            Vector3.Distance(p.transform.position, Player.m_localPlayer.transform.position) <= 20f &&
            Utils.IsPlayerInGroup(p)).ToList();

        foreach (var player in list)
        {
            player.m_seman.AddStatusEffect("Druid_NatureBuff_Buff", true, (int)this.CalculateSkillDuration(),
                this.CalculateSkillValue());
        }
        StartCooldown(this.CalculateSkillCooldown());
    }


    public class SE_Druid_NatureBuff : StatusEffect
    {
        public int msBonus;

        public SE_Druid_NatureBuff()
        {
            name = "Druid_NatureBuff_Buff";
            m_tooltip = "MS Bonus";
            m_icon = CachedIcon;
            m_name = "Nature Buff";
            m_ttl = 100;
            m_startEffects = new EffectList
            {
                m_effectPrefabs = new[]
                {
                    new EffectList.EffectData()
                    {
                        m_attach = true, m_enabled = true, m_inheritParentRotation = true,
                        m_inheritParentScale = true,
                        m_prefab = Prefab
                    }
                }
            };
        }

        public override void SetLevel(int itemLevel, float skillLevel)
        {
            m_ttl = skillLevel;
            msBonus = itemLevel;
        }

        public override void ModifySpeed(float baseSpeed, ref float speed)
        {
            speed *= (1 + msBonus / 100f);
        }

        public override void ModifyRunStaminaDrain(float baseDrain, ref float drain)
        {
            drain *= Mathf.Clamp01(1 - msBonus / 100f / 2f);
        }

        public override void ModifyJumpStaminaUsage(float baseStaminaUse, ref float staminaUse)
        {
            staminaUse *= Mathf.Clamp01(1 - msBonus / 100f / 2f);
        }
    }

    public static class Mage_ThunderSock_DB_Patches
    {
        private static void Add_SE(ObjectDB odb)
        {
            if (ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0 ||
                ObjectDB.instance.GetItemPrefab("Amber") == null) return;

            if (!odb.m_StatusEffects.Find(se => se.name == "Druid_NatureBuff_Buff"))
                odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Druid_NatureBuff>());
        }

        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        public static class ObjectDBAwake
        {
            public static void Postfix(ObjectDB __instance)
            {
                Add_SE(__instance);
            }
        }

        [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
        public static class ObjectDBCopyOtherDB
        {
            public static void Postfix(ObjectDB __instance)
            {
                Add_SE(__instance);
            }
        }
    }


    public override bool CanExecute()
    {
        return !Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Buff, Affects Party Members Nearby</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine($"\n");

        int maxLevel = this.MaxLevel;
        int forLevel = this.Level > 0 ? this.Level : 1;
        float currentValue = this.CalculateSkillValue(forLevel);
        float currentCooldown = this.CalculateSkillCooldown(forLevel);
        float currentManacost = this.CalculateSkillManacost(forLevel);
        float duration = this.CalculateSkillDuration(forLevel);

        builder.AppendLine($"MoveSpeed Increase: {Math.Round(currentValue, 1)}%");
        builder.AppendLine($"Duration: {Math.Round(duration, 1)}s");
        builder.AppendLine($"Cooldown: {Math.Round(currentCooldown, 1)}");
        builder.AppendLine($"Manacost: {Math.Round(currentManacost, 1)}");

        if (this.Level < maxLevel && this.Level > 0)
        {
            float nextValue = this.CalculateSkillValue(forLevel + 1);
            float nextCooldown = this.CalculateSkillCooldown(forLevel + 1);
            float nextManacost = this.CalculateSkillManacost(forLevel + 1);
            float nextDuration = this.CalculateSkillDuration(forLevel + 1);
            float cooldownDiff = nextCooldown - currentCooldown;
            float manacostDiff = nextManacost - currentManacost;
            float valueDiff = nextValue - currentValue;
            float durationDiff = nextDuration - duration;

            var roundedCooldownDiff = Math.Round(cooldownDiff, 1);
            var roundedManacostDiff = Math.Round(manacostDiff, 1);
            var roundedValueDiff = Math.Round(valueDiff, 1);

            builder.AppendLine($"\nNext Level:");
            builder.AppendLine(
                $"MoveSpeed Increase: {Math.Round(nextValue, 1)}% <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
            builder.AppendLine(
                $"Duration: {Math.Round(nextDuration, 1)}s <color=green>({(durationDiff > 0 ? "+" : "")}{Math.Round(durationDiff, 1)})</color>");
            builder.AppendLine(
                $"Cooldown: {Math.Round(nextCooldown, 1)} <color=green>({(roundedCooldownDiff > 0 ? "+" : "")}{roundedCooldownDiff})</color>");
            builder.AppendLine(
                $"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }


        return builder.ToString();
    }


    public override Class PreferableClass => Class.Druid;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => new Color(0.05f, 1f, 0.04f);
}