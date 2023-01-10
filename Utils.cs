using Groups;
using MagicHeim.MH_Interfaces;
using MagicHeim.SkillsDatabase.MageSkills;
using Mono.Unix.Native;
using Logger = MagicHeim_Logger.Logger;
using Random = UnityEngine.Random;

namespace MagicHeim;

public static class Utils
{
    public static bool IsEnemy(Character c)
    {
        if (c == Player.m_localPlayer) return false;
        if (c.IsPlayer())
        {
            return Player.m_localPlayer.IsPVPEnabled() && c.IsPVPEnabled();
        }

        return !c.m_baseAI || c.m_baseAI.IsEnemy(Player.m_localPlayer);
    }

    public static float CalculateSkillValue(this MH_Skill skill, int forLevel = -1)
    {
        if (skill.Definition.MaxLevel.Value <= 0) return skill.Definition.MinLvlValue.Value;
        int currentLevel = forLevel > 0 ? forLevel - 1 : skill.Level - 1;
        int maxLevel = skill.Definition.MaxLevel.Value - 1;
        float minValue = skill.Definition.MinLvlValue.Value;
        float maxValue = skill.Definition.MaxLvlValue.Value;
        return Mathf.Lerp(minValue, maxValue, (float)currentLevel / maxLevel);
    }

    public static float CalculateSkillCooldown(this MH_Skill skill, int forLevel = -1, bool skipChecks = false)
    {
        if (skill.Definition.MaxLevel.Value <= 0) return skill.Definition.MinLvlCooldown.Value;
        int currentLevel = forLevel > 0 ? forLevel - 1 : skill.Level - 1;
        int maxLevel = skill.Definition.MaxLevel.Value - 1;
        float minValue = skill.Definition.MinLvlCooldown.Value;
        float maxValue = skill.Definition.MaxLvlCooldown.Value;
        var cooldown = Mathf.Lerp(minValue, maxValue, (float)currentLevel / maxLevel);
        //passives
        if (!skipChecks)
        {
            Mage_ElementalTimescale.TryToCheckSkill(ref cooldown);
        }

        return cooldown;
    }

    public static float CalculateSkillManacost(this MH_Skill skill, int forLevel = -1, bool skipChecks = false)
    {
        if (skill.Definition.MaxLevel.Value <= 0) return skill.Definition.MinLvlManacost.Value;
        int currentLevel = forLevel > 0 ? forLevel - 1 : skill.Level - 1;
        int maxLevel = skill.Definition.MaxLevel.Value - 1;
        float minValue = skill.Definition.MinLvlManacost.Value;
        float maxValue = skill.Definition.MaxLvlManacost.Value;
        var manacost = Mathf.Lerp(minValue, maxValue, (float)currentLevel / maxLevel);
        //passives check
        if (!skipChecks)
        {
            Mage_EitrEconomy.TryToCheckSkill(ref manacost);
        }


        return manacost;
    }

    public static float CalculateSkillAoe(this MH_Skill skill, int forLevel = -1)
    {
        if (skill.Definition.MaxLevel.Value <= 0) return skill.Definition.MinLvlAoE.Value;
        int currentLevel = forLevel > 0 ? forLevel - 1 : skill.Level - 1;
        int maxLevel = skill.Definition.MaxLevel.Value - 1;
        float minValue = skill.Definition.MinLvlAoE.Value;
        float maxValue = skill.Definition.MaxLvlAoE.Value;
        return Mathf.Lerp(minValue, maxValue, (float)currentLevel / maxLevel);
    }

    public static float CalculateSkillDuration(this MH_Skill skill, int forLevel = -1)
    {
        if (skill.Definition.MaxLevel.Value <= 0) return skill.Definition.MinLvlDuration.Value;
        int currentLevel = forLevel > 0 ? forLevel - 1 : skill.Level - 1;
        int maxLevel = skill.Definition.MaxLevel.Value - 1;
        float minValue = skill.Definition.MinLvlDuration.Value;
        float maxValue = skill.Definition.MaxLvlDuration.Value;
        return Mathf.Lerp(minValue, maxValue, (float)currentLevel / maxLevel);
    }

    public static float CalculateSkillChargeTime(this MH_Skill skill, int forLevel = -1)
    {
        if (skill.Definition.MaxLevel.Value <= 0) return skill.Definition.MinLvlChargeTime.Value;
        int currentLevel = forLevel > 0 ? forLevel - 1 : skill.Level - 1;
        int maxLevel = skill.Definition.MaxLevel.Value - 1;
        float minValue = skill.Definition.MinLvlChargeTime.Value;
        float maxValue = skill.Definition.MaxLvlChargeTime.Value;
        return Mathf.Lerp(minValue, maxValue, (float)currentLevel / maxLevel);
    }

    public static float CalculateValueCharged(this float maxValue, float maxChargeTime, float charge,
        int percentageFromOriginalValue)
    {
        float minValue = maxValue * (percentageFromOriginalValue / 100f);
        return Mathf.Lerp(minValue, maxValue, charge / maxChargeTime);
    }

    public static void InitRequiredItemFirstHalf(this MH_Skill skill, string prefab, int initialValue, float step)
    {
        skill.Definition.RequiredItemFirstHalfToUpgrade = MagicHeim.config($"{skill.Definition._InternalName}",
            $"Required Item to Upgrade First Half", prefab,
            "Required Item to Upgrade");
        skill.Definition.RequiredItemFirstHalfAmountToUpgrade = MagicHeim.config($"{skill.Definition._InternalName}",
            $"Required Item Amount to Upgrade First Half", initialValue,
            "Required Item Amount to Upgrade");
        skill.Definition.RequiredItemFirstHalfAmountToUpgrade_Step = MagicHeim.config($"{skill.Definition._InternalName}",
            $"Required Item Amount to Upgrade Step First Half", step,
            "Required Item Amount to Upgrade Step");
    }
    
    public static void InitRequiredItemSecondHalf(this MH_Skill skill, string prefab, int initialValue, float step)
    {
        skill.Definition.RequiredItemSecondHalfToUpgrade = MagicHeim.config($"{skill.Definition._InternalName}",
            $"Required Item to Upgrade Second Half", prefab,
            "Required Item to Upgrade");
        skill.Definition.RequiredItemSecondHalfAmountToUpgrade = MagicHeim.config($"{skill.Definition._InternalName}",
            $"Required Item Amount to Upgrade Second Half", initialValue,
            "Required Item Amount to Upgrade");
        skill.Definition.RequiredItemSecondHalfAmountToUpgrade_Step = MagicHeim.config($"{skill.Definition._InternalName}",
            $"Required Item Amount to Upgrade Step Second Half", step,
            "Required Item Amount to Upgrade Step");
    }
    
    public static void InitRequiredItemFinal(this MH_Skill skill, string prefab, int initialValue)
    {
        skill.Definition.RequiredItemFinalToUpgrade = MagicHeim.config($"{skill.Definition._InternalName}",
            $"Required Item to Upgrade Final", prefab,
            "Required Item to Upgrade");
        skill.Definition.RequiredItemFinalAmountToUpgrade = MagicHeim.config($"{skill.Definition._InternalName}",
            $"Required Item Amount to Upgrade Final", initialValue,
            "Required Item Amount to Upgrade");
    }

    public static Vector3 GetPerfectEyePosition()
    {
        return Player.m_localPlayer.m_eye.position + Player.m_localPlayer.m_eye.right * 0.3f + Vector3.up * 0.5f;
    }

    public static bool IsMeshReadable(GameObject go)
    {
        MeshFilter[] componentsInChildren = go.GetComponentsInChildren<MeshFilter>();
        for (int i = 0; i < componentsInChildren.Length; i++)
        {
            if (!componentsInChildren[i].sharedMesh.isReadable)
            {
                return false;
            }
        }

        return true;
    }

    public static T CopyComponent<T>(T original, GameObject destination) where T : Component
    {
        Type type = original.GetType();
        Component component = destination.AddComponent(type);
        try
        {
            BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public |
                                       BindingFlags.NonPublic;
            PropertyInfo[] properties = type.GetProperties(bindingAttr);
            foreach (PropertyInfo propertyInfo in properties)
            {
                bool canWrite = propertyInfo.CanWrite;
                if (canWrite)
                {
                    propertyInfo.SetValue(component, propertyInfo.GetValue(original, null), null);
                }
            }

            FieldInfo[] fields = type.GetFields(bindingAttr);
            foreach (FieldInfo fieldInfo in fields)
            {
                fieldInfo.SetValue(component, fieldInfo.GetValue(original));
            }
        }
        catch
        {
            // ignored
        }

        return component as T;
    }

    public static int CountItems(string prefab)
    {
        int num = 0;
        foreach (ItemDrop.ItemData itemData in Player.m_localPlayer.m_inventory.m_inventory)
        {
            if (itemData.m_dropPrefab.name == prefab)
            {
                num += itemData.m_stack;
            }
        }

        return num;
    }

    public static void RemoveItems(string prefab, int amount)
    {
        GameObject go = ObjectDB.instance.GetItemPrefab(prefab);
        if (!go) return;
        Player.m_localPlayer.m_inventory.RemoveItem(go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name, amount);
    }


    public static bool IsPlayerInGroup(Player p)
    {
        if (!Groups.API.IsLoaded() || p == Player.m_localPlayer) return true;
        foreach (var party in Groups.API.GroupPlayers())
        {
            if (party.peerId == p.m_nview.m_zdo.m_uid.m_userID)
                return true;
        }

        return false;
    }


    public static void CheckProblems_ZNS()
    {
        foreach (var go in MagicHeim.asset.LoadAllAssets<GameObject>())
        {
            if (go.GetComponent<ZNetView>() &&
                !ZNetScene.instance.m_namedPrefabs.ContainsKey(go.name.GetStableHashCode()))
            {
                MagicHeim_Logger.Logger.Log($"{go} has ZNetView but not in ZNetScene");
            }

            if (!go.GetComponent<ZNetView>() &&
                ZNetScene.instance.m_namedPrefabs.ContainsKey(go.name.GetStableHashCode()))
            {
                MagicHeim_Logger.Logger.Log($"{go} is in ZNetScene but doesn't have ZNetVview");
            }
        }
    }

    public static void FloatingText(string text)
    {
        float random = Random.Range(1.2f, 1.6f);
        float random2 = Random.Range(1.2f, 1.6f);

        DamageText.WorldTextInstance worldTextInstance = new DamageText.WorldTextInstance
        {
            m_worldPos = Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.right * random +
                         Vector3.up * random2,
            m_gui = UnityEngine.Object.Instantiate(DamageText.instance.m_worldTextBase, DamageText.instance.transform)
        };
        worldTextInstance.m_gui.GetComponent<RectTransform>().sizeDelta *= 2;
        worldTextInstance.m_textField = worldTextInstance.m_gui.GetComponent<Text>();
        DamageText.instance.m_worldTexts.Add(worldTextInstance);
        worldTextInstance.m_textField.fontSize = 20;
        worldTextInstance.m_textField.text = text;
        worldTextInstance.m_timer = -1f;
    }

    [HarmonyPatch(typeof(DamageText), nameof(DamageText.Awake))]
    static class DamageText_RichSupport
    {
        static void Postfix(DamageText __instance)
        {
            __instance.m_worldTextBase.GetComponentInChildren<Text>().supportRichText = true;
        }
    }

    public static bool InWater() => Player.m_localPlayer.InLiquidSwimDepth();

    public static void DamageMH(this IDestructible c, HitData hit)
    {
        hit.ApplyModifier(Exp_Configs.GLOBAL_DAMAGE_MULTIPLIER.Value);
        c.Damage(hit);
    }
}