﻿using System.Text;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Classes;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;
using MagicHeim.UI_s;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Mage_EitrSphere : MH_Skill
{
    private static GameObject Sphere_Prefab;
    private static GameObject Sphere_Explosion;

    public Mage_EitrSphere()
    {
        _definition._InternalName = "Mage_Eitrsphere";
        _definition.Name = "$mh_mage_eitrsphere";
        _definition.Description = "$mh_mage_eitrsphere_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Eitr Regen (Per Second)", 2f,
            "Armor Bonus amount (Min Lvl)");

        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Eitr Regen (Per Second)", 25f,
            "Armor Bonus amount (Max Lvl)");

        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Manacost", 20f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Manacost", 10f,
            "Manacost amount (Max Lvl)");

        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Cooldown", 300f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Cooldown", 120f,
            "Cooldown amount (Max Lvl)");

        _definition.MinLvlDuration = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Duration", 10f,
            "Duration amount (Min Lvl)");
        _definition.MaxLvlDuration = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Duration", 25f,
            "Duration amount (Max Lvl)");

        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Max Level", 10,
            "Max Skill Level");

        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Required Level To Learn",
            50, "Required Level");

        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            $"Leveling Step", 3,
            "Leveling Step");

        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Mage_EitrSphere_Icon");
        _definition.Video = "https://kg-dev.xyz/skills/MH_Mage_EitrSphere.mp4";
        _definition.Animation = ClassAnimationReplace.MH_AnimationNames[ClassAnimationReplace.MH_Animation.MageSlam];
        _definition.AnimationTime = 0.8f;
        Sphere_Prefab = MagicHeim.asset.LoadAsset<GameObject>("Mage_EitrSphere_Prefab");
        Sphere_Prefab.AddComponent<AoeMechanic>();
        Sphere_Explosion = MagicHeim.asset.LoadAsset<GameObject>("Mage_EitrSphere_Explosion");
        
        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);
    }


    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScene_Awake_Patch
    {
        static void Postfix(ZNetScene __instance)
        {
            __instance.m_namedPrefabs[Sphere_Prefab.name.GetStableHashCode()] = Sphere_Prefab;
            __instance.m_namedPrefabs[Sphere_Explosion.name.GetStableHashCode()] = Sphere_Explosion;
        }
    }


    public class AoeMechanic : MonoBehaviour
    {
        private static float time;
        public ZNetView nview;

        private void Awake()
        {
            nview = GetComponent<ZNetView>();
        }

        public void Setup(float eitrRegen, float duration)
        {
            nview.m_zdo.Set("eitrRegen", eitrRegen);
            GetComponent<TimedDestruction>().m_timeout = duration;
            GetComponent<TimedDestruction>().CancelInvoke("DestroyNow");
            GetComponent<TimedDestruction>().Trigger();
        } 

        public bool IsInsideMultiplier(Vector3 point)
        {
            if (Vector3.Distance(point, transform.position) <= 25f) return true;
            return false;
        }

        void FixedUpdate()
        {
            if (!Player.m_localPlayer) return;
            if (!nview.IsValid()) return;
            if (time == Time.time) return;
            time = Time.time;
            var eitrRegen = nview.m_zdo.GetFloat("eitrRegen");
            Player.m_localPlayer.AddEitr(eitrRegen * Time.fixedDeltaTime);
        }
    }

    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        Player p = Player.m_localPlayer;
        var sphere = UnityEngine.Object.Instantiate(Sphere_Prefab, p.transform.position, Quaternion.identity);
        sphere.GetComponent<AoeMechanic>().Setup(this.CalculateSkillValue(), this.CalculateSkillDuration());
        UnityEngine.Object.Instantiate(Sphere_Explosion, p.transform.position, Quaternion.identity);
        StartCooldown(this.CalculateSkillCooldown());
    }

    public override bool CanExecute()
    {
        return !Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>AoE, Eitr Regen Inside, Affects All Players</color>";
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

        builder.AppendLine($"Eitr Regen (Per Second): {Math.Round(currentValue, 1)}");
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
            builder.AppendLine($"Eitr Regen (Per Second): {Math.Round(nextValue, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
            builder.AppendLine($"Duration: {Math.Round(nextDuration, 1)} <color=green>({(roundedDurationDiff > 0 ? "+" : "")}{roundedDurationDiff})</color>");
            builder.AppendLine($"Cooldown: {Math.Round(nextCooldown, 1)} <color=green>({(roundedCooldownDiff > 0 ? "+" : "")}{roundedCooldownDiff})</color>");
            builder.AppendLine($"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }

        
        return builder.ToString();
    }


    public override Class PreferableClass => Class.Mage;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => Color.cyan;
}