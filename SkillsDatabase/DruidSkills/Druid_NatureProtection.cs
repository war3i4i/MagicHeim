﻿using System.Text;
using JetBrains.Annotations;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Interfaces;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Druid_NatureProtection : MH_Skill
{
    private static GameObject Buff;
    private static GameObject Explosion;

    public Druid_NatureProtection()
    {
        _definition._InternalName = "Druid_Natureprotection";
        _definition.Name = "$mh_druid_natureprotection";
        _definition.Description = "$mh_druid_natureprotection_desc";

        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Manacost", 100f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Manacost", 75f,
            "Manacost amount (Max Lvl)");
        
        _definition.MinLvlDuration = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Duration", 5f,
            "Duration amount (Min Lvl)");
        
        _definition.MaxLvlDuration = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Duration", 10f,
            "Duration amount (Max Lvl)");

        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Cooldown", 400f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Cooldown", 240f,
            "Cooldown amount (Max Lvl)");

        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 10,
            "Max Skill Level");

        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn",
            12, "Required Level");

        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 7,
            "Leveling Step");


        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_NatureProtection_Icon");
        CachedIcon = _definition.Icon;
        _definition.Video = "https://kg.sayless.eu/skills/MH_Druid_NatureProtection.mp4";
        _definition.Animation = ClassAnimationReplace.MH_AnimationNames[ClassAnimationReplace.MH_Animation.MageWave];
        _definition.AnimationTime = 0.8f;
        Buff = MagicHeim.asset.LoadAsset<GameObject>("Druid_NatureProtection_Buff");
        Explosion = MagicHeim.asset.LoadAsset<GameObject>("Druid_NatureProtection_Explosion");

        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
        this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
        this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);
    }

    private static Sprite CachedIcon;

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScene_Awake_Patch
    {
        [UsedImplicitly]
        static void Postfix(ZNetScene __instance)
        {
            __instance.m_namedPrefabs[Buff.name.GetStableHashCode()] = Buff;
            __instance.m_namedPrefabs[Explosion.name.GetStableHashCode()] = Explosion;
        }
    }


    public override void Execute(Func<bool> Cond)
    { 
        if (!Player.m_localPlayer) return;
        Player p = Player.m_localPlayer;
        float duration = this.CalculateSkillDuration();
        List<Player> players = Player.GetAllPlayers()
            .Where(x => Vector3.Distance(x.transform.position, p.transform.position) <= 10f).ToList();
        UnityEngine.Object.Instantiate(Explosion, p.transform.position, Quaternion.identity);
        foreach (Player player in players)
        {
            if (!Utils.IsPlayerInGroup(player)) continue;
            player.GetSEMan().AddStatusEffect("Druid_NatureProtection_Buff".GetStableHashCode(), true, 0, duration);
        }

        StartCooldown(this.CalculateSkillCooldown());
    }

    public override bool CanExecute()
    {
        return !Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Magic Shield, Immortality</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine("\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentDuration = this.CalculateSkillDuration(forLevel);
        float currentCooldown = this.CalculateSkillCooldown(forLevel);
        float currentManacost = this.CalculateSkillManacost(forLevel);
        builder.AppendLine($"Duration: {Math.Round(currentDuration, 1)}");
        builder.AppendLine($"Cooldown: {Math.Round(currentCooldown, 1)}");
        builder.AppendLine($"Manacost: {Math.Round(currentManacost, 1)}");

        if (Level < maxLevel && Level > 0)
        {
            float nextDuration = this.CalculateSkillDuration(forLevel + 1);
            float nextCooldown = this.CalculateSkillCooldown(forLevel + 1);
            float nextManacost = this.CalculateSkillManacost(forLevel + 1);
            float durationDiff = nextDuration - currentDuration;
            float cooldownDiff = nextCooldown - currentCooldown;
            float manacostDiff = nextManacost - currentManacost;

            double roundedDurationDiff = Math.Round(durationDiff, 1);
            double roundedCooldownDiff = Math.Round(cooldownDiff, 1);
            double roundedManacostDiff = Math.Round(manacostDiff, 1);

            builder.AppendLine("\nNext Level:");
            builder.AppendLine($"Duration: {Math.Round(nextDuration, 1)} <color=green>({(roundedDurationDiff > 0 ? "+" : "")}{roundedDurationDiff})</color>");
            builder.AppendLine($"Cooldown: {Math.Round(nextCooldown, 1)} <color=green>({(roundedCooldownDiff > 0 ? "+" : "")}{roundedCooldownDiff})</color>");
            builder.AppendLine($"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }


        return builder.ToString();
    }

    public class SE_Druid_NatureProtection_Buff : StatusEffect
    {
        public SE_Druid_NatureProtection_Buff()
        {
            name = "Druid_NatureProtection_Buff";
            m_tooltip = "Immortality";
            m_icon = CachedIcon;
            m_name = "Natures Protection";
            m_ttl = 60;
            m_startEffects = new EffectList
            {
                m_effectPrefabs = new[]
                {
                    new EffectList.EffectData()
                    {
                        m_attach = true, m_enabled = true, m_inheritParentRotation = true,
                        m_inheritParentScale = true,
                        m_prefab = Buff
                    }
                }
            };
        }
        
        public override void SetLevel(int itemLevel, float skillLevel)
        {
            m_ttl = skillLevel;
        }

        public override void OnDamaged(HitData hit, Character attacker)
        {
            hit.ApplyModifier(0f);
        }
    }

    public static class Mage_ThunderSock_DB_Patches
    {
        private static void Add_SE(ObjectDB odb)
        {
            if (ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0 ||
                ObjectDB.instance.GetItemPrefab("Amber") == null) return;

            if (!odb.m_StatusEffects.Find(se => se.name == "Druid_NatureProtection_Buff"))
                odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Druid_NatureProtection_Buff>());
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

    public override bool CanRightClickCast => true;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => new Color(0.43f, 1f, 0.33f);
}