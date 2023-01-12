using System.Text;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Classes;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;
using MagicHeim.UI_s;

namespace MagicHeim.SkillsDatabase.DruidSkills;

public sealed class Druid_Fish : MH_Skill
{
    public static int CachedKey;
    private static GameObject Fish_Explosion;
    private static GameObject Fish_Prefab;

    public Druid_Fish()
    {
        _definition._InternalName = "Druid_Fish";
        _definition.Name = "$mh_druid_fish";
        _definition.Description = "$mh_druid_fish_desc";
        CachedKey = _definition.Key;
        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Manacost", 10f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Manacost", 2f,
            "Manacost amount (Max Lvl)");

        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Cooldown", 12f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Cooldown", 3f,
            "Cooldown amount (Max Lvl)");

        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Max Level", 5,
            "Max Skill Level");

        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Required Level To Learn",
            75, "Required Level");


        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            $"Leveling Step", 1,
            "Leveling Step");

        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_Fish_Icon");
        _definition.Video = "https://kg-dev.xyz/skills/Mage_EnergyBlast.mp4";
        _definition.Animation =
            ClassAnimationReplace.MH_AnimationNames[ClassAnimationReplace.MH_Animation.TwoHandedTransform];
        _definition.AnimationTime = 1f;
        CachedIcon = _definition.Icon;
        Fish_Explosion = MagicHeim.asset.LoadAsset<GameObject>("Druid_Fish_Explosion");
        Fish_Prefab = MagicHeim.asset.LoadAsset<GameObject>("Druid_Fish_Prefab");
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScene_Awake_Patch
    {
        static void Postfix(ZNetScene __instance)
        {
            __instance.m_namedPrefabs[Fish_Explosion.name.GetStableHashCode()] = Fish_Explosion;
        }
    }

    private static Sprite CachedIcon;

    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        Player p = Player.m_localPlayer;
        var manacost = this.CalculateSkillManacost();
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
        Toggled = true;
        var manacost = this.CalculateSkillManacost();
        Player p = Player.m_localPlayer;
        var stamina = p.GetStamina();
        p.m_seman.AddStatusEffect("Druid_FishForm");
        UnityEngine.Object.Instantiate(Fish_Explosion, p.transform.position, Quaternion.identity);
        for (;;)
        {
            if(!p) yield break;
            var useMana = manacost * Time.deltaTime;
            if (!Toggled || p.IsDead() || !p.HaveEitr(useMana) || !Utils.InWater())
            {
                Toggled = false;
                p?.m_seman.RemoveStatusEffect("Druid_FishForm");
                ZDOID zdoID = Player.m_localPlayer.GetZDOID();
                ZPackage pkg = new();
                pkg.Write(zdoID);
                pkg.Write("");
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "MH_Druid_FishForm_RPC", pkg);
                UnityEngine.Object.Instantiate(Fish_Explosion, p.transform.position, Quaternion.identity);
                StartCooldown(1f);
                yield break;
            }

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
            string @string = __instance.m_nview.m_zdo.GetString("MH_Druid_FishForm");
            if (!string.IsNullOrWhiteSpace(@string)) ReplacePlayerModel(__instance, @string);
        }
    }

    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    private static class AddingZroutMethods
    {
        private static void Postfix()
        {
            ZRoutedRpc.instance.Register("MH_Druid_FishForm_RPC", new Action<long, ZPackage>(PlayerChangedModel));
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
        Transform transform = p.transform.Find("KG_transform_DruidFish");
        if (!transform) return;
        UnityEngine.Object.Destroy(p.transform.Find("KG_transform_DruidFish").gameObject);
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
        p.m_animator.SetBool(Character.inWater, false);
    }

    private static void ReplacePlayerModel(Player p, string changedModel)
    {
        ResetPlayerModel(p);
        if (p.m_nview.IsOwner()) p.m_nview.m_zdo.Set("MH_Druid_FishForm", changedModel);
        if (changedModel != "MH_DruidFish") return;
        p.m_visual = UnityEngine.Object.Instantiate(Fish_Prefab, p.transform);
        p.m_visual.layer = LayerMask.NameToLayer("character");
        p.m_visual.transform.SetSiblingIndex(0);
        p.m_visual.transform.name = "KG_transform_DruidFish";
        p.m_visual.transform.localPosition = Vector3.zero;
        p.transform.Find("Visual").gameObject.SetActive(false);
        p.m_collider.enabled = false;
    }


    public class SE_Druid_FishForm : StatusEffect
    {
        public SE_Druid_FishForm()
        {
            name = "Druid_FishForm";
            m_tooltip = "";
            m_icon = CachedIcon;
            m_name = "Druid Fish Form";
            m_ttl = 0;
        }

        public override void ModifySpeed(float baseSpeed, ref float speed)
        {
            speed *= 4f;
        }

        public override void ModifyRunStaminaDrain(float baseDrain, ref float drain)
        {
            drain = 0;
        }

        public override void ModifyJumpStaminaUsage(float baseStaminaUse, ref float staminaUse)
        {
            staminaUse = 0;
        }

        public override void Setup(Character character)
        {
            base.Setup(character);
            ZDOID zdoID = Player.m_localPlayer.GetZDOID();
            ZPackage pkg = new();
            pkg.Write(zdoID);
            pkg.Write("MH_DruidFish");
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "MH_Druid_FishForm_RPC", pkg);
        }

        public override void UpdateStatusEffect(float dt)
        {
            if (IsDone())
            {
                UnityEngine.Object.Instantiate(Fish_Explosion, this.m_character.transform.position + Vector3.up,
                    Quaternion.identity);
                ZDOID zdoID = Player.m_localPlayer.GetZDOID();
                ZPackage pkg = new();
                pkg.Write(zdoID);
                pkg.Write("");
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "MH_Druid_FishForm_RPC", pkg);
            }
        }
    }

    public static class Druid_Fish_DB_Patches
    {
        private static void Add_SE(ObjectDB odb)
        {
            if (ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0 ||
                ObjectDB.instance.GetItemPrefab("Amber") == null) return;

            if (!odb.m_StatusEffects.Find(se => se.name == "Druid_FishForm"))
                odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Druid_FishForm>());
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
            return __instance.m_animator.gameObject.name != "KG_transform_DruidFish";
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.InMinorActionSlowdown))]
    static class Player_InMinorAction_Patch2
    {
        static bool Prefix(Player __instance)
        {
            return __instance.m_animator.gameObject.name != "KG_transform_DruidFish";
        }
    }

    public override bool CanExecute()
    {
        return Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Transform, Movement Speed, Toggle</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine($"\n");

        int maxLevel = this.MaxLevel;
        int forLevel = this.Level > 0 ? this.Level : 1;
        float currentManacost = this.CalculateSkillManacost(forLevel);
        builder.AppendLine($"Manacost: {Math.Round(currentManacost, 1)}");

        if (this.Level < maxLevel && this.Level > 0)
        {
            float nextManacost = this.CalculateSkillManacost(forLevel + 1);
            float manacostDiff = nextManacost - currentManacost;
            var roundedManacostDiff = Math.Round(manacostDiff, 1);

            builder.AppendLine($"\nNext Level:");
            builder.AppendLine(
                $"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }


        return builder.ToString();
    }

    public override Class PreferableClass => Class.Druid;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => Color.green;
}