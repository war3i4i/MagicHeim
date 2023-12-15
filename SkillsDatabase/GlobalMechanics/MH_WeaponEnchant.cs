using ItemDataManager;

namespace MagicHeim.SkillsDatabase.GlobalMechanics;

public static class MH_WeaponEnchants_VFXs
{
    public static readonly Dictionary<MH_WeaponEnchant.Type, GameObject> WeaponEnchantVFXs =
        new Dictionary<MH_WeaponEnchant.Type, GameObject>();
}

public class MH_WeaponEnchant : ItemData
{
    public enum Type
    {
        None = 0,
        Fire = 1,
        Lightning = 2,
        Frost = 3,
        Poison = 4,
        Spirit = 5,
    }

    public Type type;
    public long time;
    public int value;
    public int duration;

    public override void Save()
    {
        Value = time.ToString() + ";" + value.ToString() + ";" + type.ToString() + ";" + duration.ToString();
    }

    public override void Load()
    {
        var split = Value.Split(';');
        time = long.Parse(split[0]);
        value = int.Parse(split[1]);
        type = (Type)Enum.Parse(typeof(Type), split[2]);
        duration = int.Parse(split[3]);
    }
}

[HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.DropItem))]
static class ItemDrop_DropItem_Patch
{
    static void Postfix(ref ItemDrop __result)
    {
        var data = __result.m_itemData?.Data().Get<MH_WeaponEnchant>();
        if (data != null && __result.m_nview && __result.m_nview.IsValid())
        {
            __result.m_nview.m_zdo.Set("MH_WeaponEnchant", (int)data.type);
        }
    }
}

[HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Start))]
static class ItemDrop_Start_Patch
{
    static void Postfix(ItemDrop __instance)
    {
        if (!__instance.m_nview || !__instance.m_nview.IsValid()) return;
        if (__instance.m_nview.m_zdo.GetInt("MH_WeaponEnchant") is int val && val > 0)
        {
            var MeshFilter = __instance.GetComponentInChildren<MeshFilter>();
            if (MeshFilter == null || (MH_WeaponEnchant.Type)val == MH_WeaponEnchant.Type.None) return;
            var _VFX = MH_WeaponEnchants_VFXs.WeaponEnchantVFXs[(MH_WeaponEnchant.Type)val];
            var go = UnityEngine.Object.Instantiate(_VFX, __instance.transform);
            PSMeshRendererUpdater update = go.GetComponent<PSMeshRendererUpdater>();
            update.MeshObject = __instance.gameObject;
            update.UpdateMeshEffect(); 
        }
    }
}

[HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetLeftItem))]
static class VisEquipment_Patch_Left
{
    static void Prefix(VisEquipment __instance, ref string name)
    {
        if (__instance != Player.m_localPlayer?.m_visEquipment) return;
        __instance.m_nview.m_zdo.Set("mh_mage_weaponenchantLeft", 0);
        if (name == "") return;
        var leftItem = Player.m_localPlayer.m_leftItem;
        if (leftItem == null) return;
        var data = leftItem.Data().Get<MH_WeaponEnchant>();
        if (data == null) return;
        if (data.time + data.duration < EnvMan.instance.m_totalSeconds)
        {
            leftItem.Data().Remove<MH_WeaponEnchant>();
            return;
        }

        __instance.m_nview.m_zdo.Set("mh_mage_weaponenchantLeft", (int)data.type);
    }
}

[HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetRightItem))]
static class VisEquipment_Patch_Right
{
    static void Prefix(VisEquipment __instance, ref string name)
    {
        if (__instance != Player.m_localPlayer?.m_visEquipment) return;
        __instance.m_nview.m_zdo.Set("mh_mage_weaponenchantRight", 0);
        if (name == "") return;
        var rightItem = Player.m_localPlayer.m_rightItem;
        if (rightItem == null) return;
        var data = rightItem.Data().Get<MH_WeaponEnchant>();
        if (data == null) return;
        if (data.time + data.duration < EnvMan.instance.m_totalSeconds)
        {
            rightItem.Data().Remove<MH_WeaponEnchant>();
            return;
        }

        __instance.m_nview.m_zdo.Set("mh_mage_weaponenchantRight", (int)data.type);
    }
}

[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDamage), typeof(int), typeof(float))]
static class ItemDrop__Patch
{
    static void Postfix(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result)
    {
        var data = __instance.Data().Get<MH_WeaponEnchant>();
        if (data == null) return;
        if (data.type is MH_WeaponEnchant.Type.None) return;
        if (data.time + data.duration < EnvMan.instance.m_totalSeconds)
        {
            __instance.Data().Remove<MH_WeaponEnchant>();
            return;
        }

        switch (data.type)
        {
            case MH_WeaponEnchant.Type.Fire:
                __result.m_fire += data.value;
                break;
            case MH_WeaponEnchant.Type.Frost:
                __result.m_frost += data.value;
                break;
            case MH_WeaponEnchant.Type.Lightning:
                __result.m_lightning += data.value;
                break;
            case MH_WeaponEnchant.Type.Poison:
                __result.m_poison += data.value;
                break;
            case MH_WeaponEnchant.Type.Spirit:
                __result.m_spirit += data.value;
                break;
        }
    }
}

[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int),
    typeof(bool), typeof(float))]
public class GetTooltipPatch
{
    public static void Postfix(ItemDrop.ItemData item, bool crafting, ref string __result)
    {
        if (crafting) return;
        var data = item.Data().Get<MH_WeaponEnchant>();
        if (data == null) return;
        if (data.time + data.duration < EnvMan.instance.m_totalSeconds)
        {
            item.Data().Remove<MH_WeaponEnchant>();
            return;
        }

        switch (data.type)
        {
            case MH_WeaponEnchant.Type.Fire:
                __result +=
                    $"\nEnchanted with Fire Damage <color=red>(+{data.value})</color>. Time left: <color=yellow>{(int)(data.duration - (EnvMan.instance.m_totalSeconds - data.time))} seconds</color>";
                break;
            case MH_WeaponEnchant.Type.Frost:
                __result +=
                    $"\nEnchanted with Frost Damage <color=#00FFFF>(+{data.value})</color>. Time left: <color=yellow>{(int)(data.duration - (EnvMan.instance.m_totalSeconds - data.time))} seconds</color>";
                break;
            case MH_WeaponEnchant.Type.Lightning:
                __result +=
                    $"\nEnchanted with Lightning Damage <color=blue>(+{data.value})</color>. Time left: <color=yellow>{(int)(data.duration - (EnvMan.instance.m_totalSeconds - data.time))} seconds</color>";
                break;
            case MH_WeaponEnchant.Type.Poison:
                __result +=
                    $"\nEnchanted with Poison Damage <color=green>(+{data.value})</color>. Time left: <color=yellow>{(int)(data.duration - (EnvMan.instance.m_totalSeconds - data.time))} seconds</color>";
                break;
            case MH_WeaponEnchant.Type.Spirit:
                __result +=
                    $"\nEnchanted with Spirit Damage <color=#808080>(+{data.value})</color>. Time left: <color=yellow>{(int)(data.duration - (EnvMan.instance.m_totalSeconds - data.time))} seconds</color>";
                break;
             
        }
    }
}