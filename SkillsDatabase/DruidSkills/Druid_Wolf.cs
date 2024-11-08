﻿using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Interfaces;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Serialization;
using Logger = MagicHeim_Logger.Logger;

namespace MagicHeim.SkillsDatabase.DruidSkills;

public sealed class Druid_Wolf : MH_Skill
{
    public static int CachedKey;
    private static GameObject Wolf_Explosion;
    
    static Druid_Wolf()
    {
        AnimationSpeedManager.Add(AnimSpeedManager);
    }

    private static int CachedAnimHash = "Druid_WolfForm".GetStableHashCode();
    
    private static double AnimSpeedManager(Character c, double speed)
    {
        if (c != Player.m_localPlayer || !c.InAttack()) return speed;
        SE_Druid_WolfForm se = c.m_seman.GetStatusEffect(CachedAnimHash) as SE_Druid_WolfForm;
        if (se == null) return speed;
        return speed * (1 + se.aspeed / 100f);
    }

    public Druid_Wolf()
    {
        _definition._InternalName = "Druid_Wolf";
        _definition.Name = "$mh_druid_wolf";
        _definition.Description = "$mh_druid_wolf_desc";
        CachedKey = _definition.Key;
        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Manacost", 10f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Manacost", 5f,
            "Manacost amount (Max Lvl)");

        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Cooldown", 12f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Cooldown", 3f,
            "Cooldown amount (Max Lvl)");

        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 5,
            "Max Skill Level");

        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn",
            40, "Required Level");


        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 5,
            "Leveling Step");

        _definition.ExternalValues =
        [
            MagicHeim.config($"{_definition._InternalName}",
                "MIN LVL Attack Speed", 10f, "Attack Speed amount (Min Lvl)"),

            MagicHeim.config($"{_definition._InternalName}",
                "MAX LVL Attack Speed", 20f, "Attack Speed amount (Max Lvl)"),

            MagicHeim.config($"{_definition._InternalName}",
                "MIN LVL Movement Speed", 20f, "Movement Speed amount (Min Lvl)"),

            MagicHeim.config($"{_definition._InternalName}",
                "MAX LVL Movement Speed", 40f, "Movement Speed amount (Max Lvl)")
        ];
        
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_Wolf_Icon");
        _definition.Video = "https://kg.sayless.eu/skills/MH_Druid_Wolf.mp4";

        _definition.Animation =
            ClassAnimationReplace.MH_AnimationNames[ClassAnimationReplace.MH_Animation.TwoHandedTransform];
        _definition.AnimationTime = 1f;
        CachedIcon = _definition.Icon;
        Wolf_Explosion = MagicHeim.asset.LoadAsset<GameObject>("Druid_Wolf_Explosion");
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScene_Awake_Patch
    {
        [UsedImplicitly]
        static void Postfix(ZNetScene __instance)
        {
            __instance.m_namedPrefabs[Wolf_Explosion.name.GetStableHashCode()] = Wolf_Explosion;
        }
    }

    private static Sprite CachedIcon;

    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        Player p = Player.m_localPlayer;
        float manacost = this.CalculateSkillManacost();
        if (!Toggled)
        {
            if (p.HaveEitr(manacost * 3f))
            {
                MagicHeim._thistype.StartCoroutine(ManaDrain());
            }
        }
        else
        {
            Toggled = false;
        }
    }

    private IEnumerator ManaDrain()
    {
        int latestAttack = 1;
        Toggled = true;
        float manacost = this.CalculateSkillManacost();
        Player p = Player.m_localPlayer;
        float stamina = p.GetStamina();
        p.m_seman.AddStatusEffect("Druid_WolfForm".GetStableHashCode(), false, Mathf.CeilToInt(this.CalculateSkillExternalValue(0)), Mathf.CeilToInt(this.CalculateSkillExternalValue(2)));
        UnityEngine.Object.Instantiate(Wolf_Explosion, p.transform.position, Quaternion.identity);
        for (;;)
        {
            if(!p) yield break;
            float useMana = manacost * Time.deltaTime;
            if (!Toggled || p.IsDead() || !p.HaveEitr(useMana) || Utils.InWater())
            {
                Toggled = false;
                p.m_seman.RemoveStatusEffect("Druid_WolfForm".GetStableHashCode());
                ZDOID zdoID = Player.m_localPlayer.GetZDOID();
                ZPackage pkg = new(); 
                pkg.Write(zdoID);
                pkg.Write("");
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "MH_Druid_WolfForm_RPC", pkg);
                UnityEngine.Object.Instantiate(Wolf_Explosion, p.transform.position, Quaternion.identity);
                StartCooldown(1f);
                yield break;
            }

            if (ZInput.GetButtonDown("Attack") && !p.InAttack())
            {
                p.m_zanim.SetTrigger("attack" + latestAttack);
                latestAttack++; 
                if (latestAttack > 3) latestAttack = 1;
            }
            p.m_zanim.SetFloat(Character.s_turnSpeed, 0);
            p.UseEitr(useMana);
            p.m_stamina = stamina;
            if (p.m_stamina > p.m_maxStamina) p.m_stamina = p.m_maxStamina;
            yield return null;
        }
    }

    [HarmonyPatch(typeof(Player), "Start")]
    private static class PlayerStartPatch
    {
        private static void Postfix(Player __instance)
        {
            if (!Player.m_localPlayer) return;
            string @string = __instance.m_nview.m_zdo.GetString("MH_Druid_WolfForm");
            if (!string.IsNullOrWhiteSpace(@string)) ReplacePlayerModel(__instance, @string);
        }
    }

    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    private static class AddingZroutMethods
    {
        private static void Postfix()
        {
            ZRoutedRpc.instance.Register("MH_Druid_WolfForm_RPC", new Action<long, ZPackage>(PlayerChangedModel));
        }
    }

    private static void PlayerChangedModel(long sender, ZPackage pkg)
    {
        ZDOID id = pkg.ReadZDOID();
        string changedModel = pkg.ReadString();
        GameObject go = ZNetScene.instance.FindInstance(id);
        if (!go || !go.GetComponent<Player>()) return;
        Player component = go.GetComponent<Player>();
        ReplacePlayerModel(component, changedModel);
    }
    
    private static readonly int Wakeup = Animator.StringToHash("wakeup");

    private static void ResetPlayerModel(Player p)
    {
        Transform transform = p.transform.Find("KG_transform_DruidWolf");
        if (!transform) return;
        UnityEngine.Object.Destroy(p.transform.Find("KG_transform_DruidWolf").gameObject);
        p.m_visual = p.transform.Find("Visual").gameObject;
        p.m_visual.transform.SetSiblingIndex(0);
        p.m_visual.SetActive(true);
        p.m_animator = p.m_visual.GetComponent<Animator>();
        p.m_zanim.m_animator = p.m_visual.GetComponent<Animator>();
        p.m_visEquipment.m_visual = p.m_visual;
        p.GetComponent<FootStep>().m_feet = new[]
        { 
            global::Utils.FindChild(p.m_visual.transform, "LeftFoot"),
            global::Utils.FindChild(p.m_visual.transform, "RightFoot")
        };
        p.m_collider.enabled = true;
        p.m_animator.SetBool(Wakeup, false);
        p.m_animator.SetBool(Character.s_inWater, false);
        p.StartCoroutine(isWaterNoRoutine(p));
    }

    private static IEnumerator isWaterNoRoutine(Player p)
    {
        yield return new WaitForSecondsRealtime(1f);
        p.m_animator.SetBool(Character.s_inWater, false);
    }

    private static void ReplacePlayerModel(Player p, string changedModel)
    {
        ResetPlayerModel(p);
        if (p.m_nview.IsOwner()) p.m_nview.m_zdo.Set("MH_Druid_WolfForm", changedModel);
        GameObject gameObject = ZNetScene.instance.GetPrefab(changedModel);
        if (!gameObject || !gameObject.GetComponent<Character>()) return;
        p.m_visual =
            UnityEngine.Object.Instantiate(gameObject.GetComponentInChildren<Animator>().gameObject, p.transform);
        p.m_visual.layer = LayerMask.NameToLayer("character");
        p.m_visual.transform.SetSiblingIndex(0);
        p.m_visual.transform.name = "KG_transform_DruidWolf";
        Collider collider = Utils.CopyComponent(ZNetScene.instance.GetPrefab(changedModel).GetComponent<Collider>(),
            p.m_visual);
        collider.gameObject.layer = LayerMask.NameToLayer("character");
        p.m_visual.transform.localPosition = Vector3.zero;
        p.m_animator = p.m_visual.GetComponent<Animator>();
        p.m_zanim.m_animator = p.m_visual.GetComponent<Animator>();
        p.m_zanim.m_animator.runtimeAnimatorController = ClassAnimationReplace.MH_WolfController;
        p.m_zanim.m_animator.Update(0f);
        p.transform.Find("Visual").gameObject.SetActive(false);
        p.m_visEquipment.m_visual = p.m_visual;
        p.m_animator.logWarnings = false;
        p.m_collider.enabled = false;
        p.GetComponent<FootStep>().m_feet = new[]
        {
            global::Utils.FindChild(p.m_visual.transform, "LeftFoot"),
            global::Utils.FindChild(p.m_visual.transform, "RightFoot")
        };
    }
    
    public class SE_Druid_WolfForm : StatusEffect
    {
        public int aspeed;
        public int mspeed;
        
        public SE_Druid_WolfForm()
        {
            name = "Druid_WolfForm";
            m_tooltip = "Transform Into Wolf";
            m_icon = CachedIcon;
            m_name = "Druid Wolf Form";
            m_ttl = 0;
        }

        public override void SetLevel(int itemLevel, float skillLevel)
        {
            aspeed = itemLevel;
            mspeed = (int)skillLevel;
        }

        public override void ModifySpeed(float baseSpeed, ref float speed, Character character, Vector3 dir)
        {
            speed *= (1 + mspeed / 100f);
        }

        public override void Setup(Character character) 
        {
            base.Setup(character);
            ZDOID zdoID = Player.m_localPlayer.GetZDOID();
            ZPackage pkg = new();
            pkg.Write(zdoID);
            pkg.Write("Wolf");
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "MH_Druid_WolfForm_RPC", pkg);
        }

        public override void UpdateStatusEffect(float dt)
        {
            if (IsDone())
            {
                Instantiate(Wolf_Explosion, m_character.transform.position + Vector3.up, Quaternion.identity);
                ZDOID zdoID = Player.m_localPlayer.GetZDOID();
                ZPackage pkg = new();
                pkg.Write(zdoID);
                pkg.Write("");
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "MH_Druid_WolfForm_RPC", pkg);
            }
        }
    }

    public static class Druid_Wolf_DB_Patches
    {
        private static void Add_SE(ObjectDB odb)
        {
            if (ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0 ||
                ObjectDB.instance.GetItemPrefab("Amber") == null) return;

            if (!odb.m_StatusEffects.Find(se => se.name == "Druid_WolfForm"))
                odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Druid_WolfForm>());
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
    
    [HarmonyPatch(typeof(Player), nameof(Player.InMinorAction))]
    static class Player_InMinorAction_Patch
    {
        static bool Prefix(Player __instance)
        {
            return __instance.m_animator.gameObject.name != "KG_transform_DruidWolf";
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.InMinorActionSlowdown))]
    static class Player_InMinorAction_Patch2
    {
        static bool Prefix(Player __instance)
        {
            return __instance.m_animator.gameObject.name != "KG_transform_DruidWolf";
        }
    }

    public override bool CanExecute()
    {
        return !Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Transform, Movement Speed, AttackSpeed, Toggle</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine("\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentManacost = this.CalculateSkillManacost(forLevel);
        int currentAttackSpeed = (int)this.CalculateSkillExternalValue(0, forLevel);
        int currentMovementSpeed = (int)this.CalculateSkillExternalValue(2, forLevel);
        builder.AppendLine($"Attack Speed: {currentAttackSpeed}%");
        builder.AppendLine($"Movement Speed: {currentMovementSpeed}%");
        builder.AppendLine($"Manacost: {Math.Round(currentManacost, 1)}");

        if (Level < maxLevel && Level > 0)
        {
            int nextAttackSpeed = (int)this.CalculateSkillExternalValue(0, forLevel + 1);
            int nextMovementSpeed = (int)this.CalculateSkillExternalValue(2, forLevel + 1);
            float nextManacost = this.CalculateSkillManacost(forLevel + 1);
            int attackSpeedDiff = nextAttackSpeed - currentAttackSpeed;
            int moveSpeedDiff = nextMovementSpeed - currentMovementSpeed;
            float manacostDiff = nextManacost - currentManacost;
            double roundedManacostDiff = Math.Round(manacostDiff, 1);

            builder.AppendLine("\nNext Level:");
            builder.AppendLine($"Attack Speed: {nextAttackSpeed}% <color=green>({(attackSpeedDiff > 0 ? "+" : "")}{attackSpeedDiff})</color>");
            builder.AppendLine($"Movement Speed: {nextMovementSpeed}% <color=green>({(moveSpeedDiff > 0 ? "+" : "")}{moveSpeedDiff})</color>");
            builder.AppendLine($"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }
 

        return builder.ToString();
    }

    public override bool CanRightClickCast => true;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => Color.green;
}