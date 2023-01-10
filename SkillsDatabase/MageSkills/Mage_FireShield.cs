using System.Text;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Classes;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;
using MagicHeim.UI_s;
using Logger = MagicHeim_Logger.Logger;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Mage_FireShield : MH_Skill
{
    private static GameObject FireShield_Buff;

    public Mage_FireShield()
    {
        _definition._InternalName = "Mage_Fireshield";
        _definition.Name = "$mh_mage_fireshield";
        _definition.Description = "$mh_mage_fireshield_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Armor Bonus", 5f,
            "Armor Bonus amount (Min Lvl)");

        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Armor Bonus", 45f,
            "Armor Bonus amount (Max Lvl)");

        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Manacost", 20f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Manacost", 55f,
            "Manacost amount (Max Lvl)");

        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Cooldown", 600f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Cooldown", 360f,
            "Cooldown amount (Max Lvl)");

        _definition.MinLvlDuration = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Duration", 60f,
            "Duration amount (Min Lvl)");
        _definition.MaxLvlDuration = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Duration", 240f,
            "Duration amount (Max Lvl)");

        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Max Level", 10,
            "Max Skill Level");

        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Required Level To Learn",
            8, "Required Level");


        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            $"Leveling Step", 5,
            "Leveling Step");

        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Mage_FireShield_Icon");
        CachedIcon = _definition.Icon;
        _definition.Video = "https://kg-dev.xyz/skills/MH_Mage_FireShield.mp4";
        _definition.Animation = ClassAnimationReplace.MH_AnimationNames[ClassAnimationReplace.MH_Animation.MageWave];
        _definition.AnimationTime = 0.8f;
        FireShield_Buff = MagicHeim.asset.LoadAsset<GameObject>("Mage_FireShield_Prefab");

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
            __instance.m_namedPrefabs[FireShield_Buff.name.GetStableHashCode()] = FireShield_Buff;
        }
    }


    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        Player p = Player.m_localPlayer;
        var armorBonus = (int)this.CalculateSkillValue();
        var duration = this.CalculateSkillDuration();
        var players = Player.GetAllPlayers()
            .Where(x => Vector3.Distance(x.transform.position, p.transform.position) <= 10f).ToList();
        foreach (var player in players)
        {
            if (!Utils.IsPlayerInGroup(player)) continue;
            player.GetSEMan().AddStatusEffect("Mage_FireShield_Buff", true, armorBonus, duration);
        }

        StartCooldown(this.CalculateSkillCooldown());
    }

    public override bool CanExecute()
    {
        return !Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Buff, Elemental Resistance Bonus (%), Affects Party Members Nearby</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine($"\n");

        int maxLevel = this.MaxLevel;
        int forLevel = this.Level > 0 ? this.Level : 1;
        float currentValue = this.CalculateSkillValue(forLevel);
        float currentDuration = this.CalculateSkillDuration(forLevel);
        float currentCooldown = this.CalculateSkillCooldown(forLevel);
        float currentManacost = this.CalculateSkillManacost(forLevel);

        builder.AppendLine($"Elemental Damage Resistance: {Math.Round(currentValue, 1)}%");
        builder.AppendLine($"Duration: {Math.Round(currentDuration, 1)}");
        builder.AppendLine($"Cooldown: {Math.Round(currentCooldown, 1)}");
        builder.AppendLine($"Manacost: {Math.Round(currentManacost, 1)}");

        if (this.Level < maxLevel && this.Level > 0)
        {
            float nextValue = this.CalculateSkillValue(forLevel + 1);
            float nextDuration = this.CalculateSkillDuration(forLevel + 1);
            float nextCooldown = this.CalculateSkillCooldown(forLevel + 1);
            float nextManacost = this.CalculateSkillManacost(forLevel + 1);
            float durationDiff = nextDuration - currentDuration;
            float cooldownDiff = nextCooldown - currentCooldown;
            float manacostDiff = nextManacost - currentManacost;
            float valueDiff = nextValue - currentValue;

            var roundedDurationDiff = Math.Round(durationDiff, 1);
            var roundedCooldownDiff = Math.Round(cooldownDiff, 1);
            var roundedManacostDiff = Math.Round(manacostDiff, 1);
            var roundedValueDiff = Math.Round(valueDiff, 1);

            builder.AppendLine($"\nNext Level:");
            builder.AppendLine(
                $"Elemental Damage Resistance: {Math.Round(nextValue, 1)}% <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
            builder.AppendLine(
                $"Duration: {Math.Round(nextDuration, 1)} <color=green>({(roundedDurationDiff > 0 ? "+" : "")}{roundedDurationDiff})</color>");
            builder.AppendLine(
                $"Cooldown: {Math.Round(nextCooldown, 1)} <color=green>({(roundedCooldownDiff > 0 ? "+" : "")}{roundedCooldownDiff})</color>");
            builder.AppendLine(
                $"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }


        return builder.ToString();
    }

    public class SE_Mage_FireShield_Buff : StatusEffect
    {
        public int armorBonus;

        public SE_Mage_FireShield_Buff()
        {
            name = "Mage_FireShield_Buff";
            m_tooltip = "Armor Bonus";
            m_icon = CachedIcon;
            m_name = "Fire Shield";
            m_ttl = 100;
            m_startEffects = new EffectList
            {
                m_effectPrefabs = new[]
                {
                    new EffectList.EffectData()
                    {
                        m_attach = true, m_enabled = true, m_inheritParentRotation = true,
                        m_inheritParentScale = true,
                        m_prefab = FireShield_Buff
                    }
                }
            };
        }

        public override void SetLevel(int itemLevel, float skillLevel)
        {
            m_ttl = skillLevel;
            armorBonus = itemLevel;
        }

        public override void OnDamaged(HitData hit, Character attacker)
        {
            hit.m_damage.m_fire *= Mathf.Clamp01(1 - armorBonus / 100f);
            hit.m_damage.m_lightning *= Mathf.Clamp01(1 - armorBonus / 100f);
            hit.m_damage.m_frost *= Mathf.Clamp01(1 - armorBonus / 100f);
        }
    }

    public static class Mage_ThunderSock_DB_Patches
    {
        private static void Add_SE(ObjectDB odb)
        {
            if (ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0 ||
                ObjectDB.instance.GetItemPrefab("Amber") == null) return;

            if (!odb.m_StatusEffects.Find(se => se.name == "Mage_FireShield_Buff"))
                odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Mage_FireShield_Buff>());
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

    public override Class PreferableClass => Class.Mage;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => new Color(1f, 0.08f, 0.35f);
}