﻿using System.Text;
using ItemDataManager;
using JetBrains.Annotations;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Interfaces;
using MagicHeim.SkillsDatabase.GlobalMechanics;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Druid_WeaponEnchantPoison : MH_Skill
{
    private static GameObject VFX_Fist;
    private static GameObject VFX;
    private static GameObject VFX_NonRead;

    public Druid_WeaponEnchantPoison()
    {
        _definition._InternalName = "Druid_Weaponenchantpoison";
        _definition.Name = "$mh_druid_weaponenchantpoison";
        _definition.Description = "$mh_druid_weaponenchantpoison_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Damage", 2f,
            "Armor Bonus amount (Min Lvl)");

        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Damage", 30f,
            "Armor Bonus amount (Max Lvl)");

        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Manacost", 50f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Manacost", 100f,
            "Manacost amount (Max Lvl)");

        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Cooldown", 1200f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Cooldown", 600f,
            "Cooldown amount (Max Lvl)");

        _definition.MinLvlDuration = MagicHeim.config($"{_definition._InternalName}", 
            "MIN Lvl Duration", 150f,
            "Duration amount (Min Lvl)");
        _definition.MaxLvlDuration = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Duration", 600f,
            "Duration amount (Max Lvl)");

        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 10,
            "Max Skill Level");

        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn",
            20, "Required Level");

        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 5,
            "Leveling Step"); 
 
 
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_WeaponEnchantPoison_Icon");
        _definition.Video = "https://kg.sayless.eu/skills/MH_Mage_WeaponFireEnchant.mp4";
        _definition.Animation = ClassAnimationReplace.MH_AnimationNames[ClassAnimationReplace.MH_Animation.MageWave];
        _definition.AnimationTime = 0.8f;
        VFX = MagicHeim.asset.LoadAsset<GameObject>("Druid_WeaponEnchantPoison_VFX");
        VFX_NonRead = MagicHeim.asset.LoadAsset<GameObject>("Druid_WeaponEnchantPoison_NonRead");
        VFX_Fist = MagicHeim.asset.LoadAsset<GameObject>("Druid_WeaponEnchantPoison_Fist");
        MH_WeaponEnchants_VFXs.WeaponEnchantVFXs.Add(MH_WeaponEnchant.Type.Poison, VFX_NonRead);
        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
        this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
        this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScene_Awake_Patch
    {
        [UsedImplicitly]
        static void Postfix(ZNetScene __instance)
        {
            __instance.m_namedPrefabs[VFX_Fist.name.GetStableHashCode()] = VFX_Fist;
        }
    }

    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        Player p = Player.m_localPlayer;
        ItemDrop.ItemData weapon = p.GetCurrentWeapon();
        if (weapon == null || weapon == p.m_unarmedWeapon.m_itemData)
        {
            p.AddEitr(this.CalculateSkillManacost());
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$mh_mage_weaponenchant_no_weapon");
            return;
        }

        MH_WeaponEnchant data = weapon.Data().GetOrCreate<MH_WeaponEnchant>();
        data.time = (long)EnvMan.instance.m_totalSeconds;
        data.value = (int)this.CalculateSkillValue();
        data.type = MH_WeaponEnchant.Type.Poison;
        data.duration = (int)this.CalculateSkillDuration();
        StartCooldown(this.CalculateSkillCooldown());
        UnityEngine.Object.Instantiate(VFX_Fist, p.m_visEquipment.m_rightHand);
        UnityEngine.Object.Instantiate(VFX_Fist, p.m_visEquipment.m_leftHand);
        p.StartCoroutine(FrameSkipEquip(weapon));
    }

    private IEnumerator FrameSkipEquip(ItemDrop.ItemData weapon)
    {
        Player.m_localPlayer.UnequipItem(weapon);
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        if (weapon != null && Player.m_localPlayer.m_inventory.ContainsItem(weapon))
            Player.m_localPlayer?.EquipItem(weapon);
    }

    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetLeftHandEquipped))]
    private static class MockLeft
    {
        private static bool Transfer;

        static void Prefix(VisEquipment __instance, int hash)
        {
            if (__instance.m_currentLeftItemHash != hash)
            {
                Transfer = true;
            }
        }

        private static void Postfix(VisEquipment __instance)
        {
            if (!Transfer || !__instance.m_nview || __instance.m_nview.m_zdo == null) return;
            Transfer = false;
            if (__instance.m_leftItemInstance)
            {
                if (__instance.m_nview.m_zdo.GetInt("mh_mage_weaponenchantLeft") == (int)MH_WeaponEnchant.Type.Poison)
                {
                    GameObject go = UnityEngine.Object.Instantiate(
                        Utils.IsMeshReadable(__instance.m_leftItemInstance) ? VFX : VFX_NonRead,
                        __instance.m_leftItemInstance.transform);
                    PSMeshRendererUpdater update = go.GetComponent<PSMeshRendererUpdater>();
                    update.MeshObject = __instance.m_leftItemInstance;
                    update.UpdateMeshEffect();
                }
            }
        }
    }

    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetRightHandEquipped))]
    private static class MockRight
    {
        private static bool Transfer;

        static void Prefix(VisEquipment __instance, int hash)
        {
            if (__instance.m_currentRightItemHash != hash)
            {
                Transfer = true;
            }
        }

        private static void Postfix(VisEquipment __instance)
        {
            if (!Transfer || !__instance.m_nview || __instance.m_nview.m_zdo == null) return;
            Transfer = false;
            if (__instance.m_rightItemInstance)
            {
                if (__instance.m_nview.m_zdo.GetInt("mh_mage_weaponenchantRight") == (int)MH_WeaponEnchant.Type.Poison)
                {
                    GameObject go = UnityEngine.Object.Instantiate(
                        Utils.IsMeshReadable(__instance.m_rightItemInstance) ? VFX : VFX_NonRead,
                        __instance.m_rightItemInstance.transform);
                    PSMeshRendererUpdater update = go.GetComponent<PSMeshRendererUpdater>();
                    update.MeshObject = __instance.m_rightItemInstance;
                    update.UpdateMeshEffect();
                }
            }
        }
    }


    public override bool CanExecute()
    {
        return !Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Weapon Enchant</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine("\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentValue = this.CalculateSkillValue(forLevel);
        float currentDuration = this.CalculateSkillDuration(forLevel);
        float currentCooldown = this.CalculateSkillCooldown(forLevel);
        float currentManacost = this.CalculateSkillManacost(forLevel);

        builder.AppendLine($"Bonus Poison Damage: {Math.Round(currentValue, 1)}");
        builder.AppendLine($"Duration: {Math.Round(currentDuration, 1)}");
        builder.AppendLine($"Cooldown: {Math.Round(currentCooldown, 1)}");
        builder.AppendLine($"Manacost: {Math.Round(currentManacost, 1)}");

        if (Level < maxLevel && Level > 0)
        {
            float nextValue = this.CalculateSkillValue(forLevel + 1);
            float nextDuration = this.CalculateSkillDuration(forLevel + 1);
            float nextCooldown = this.CalculateSkillCooldown(forLevel + 1);
            float nextManacost = this.CalculateSkillManacost(forLevel + 1);
            float durationDiff = nextDuration - currentDuration;
            float cooldownDiff = nextCooldown - currentCooldown;
            float manacostDiff = nextManacost - currentManacost;
            float valueDiff = nextValue - currentValue;

            double roundedDurationDiff = Math.Round(durationDiff, 1);
            double roundedCooldownDiff = Math.Round(cooldownDiff, 1);
            double roundedManacostDiff = Math.Round(manacostDiff, 1);
            double roundedValueDiff = Math.Round(valueDiff, 1);

            builder.AppendLine("\nNext Level:");
            builder.AppendLine($"Bonus Poison Damage: {Math.Round(nextValue, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
            builder.AppendLine($"Duration: {Math.Round(nextDuration, 1)} <color=green>({(roundedDurationDiff > 0 ? "+" : "")}{roundedDurationDiff})</color>");
            builder.AppendLine($"Cooldown: {Math.Round(nextCooldown, 1)} <color=green>({(roundedCooldownDiff > 0 ? "+" : "")}{roundedCooldownDiff})</color>");
            builder.AppendLine($"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }


        return builder.ToString();
    }

    public override bool CanRightClickCast => true;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => new Color(0.22f, 1f, 0.28f);
}