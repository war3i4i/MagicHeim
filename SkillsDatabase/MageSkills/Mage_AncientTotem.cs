using System.Text;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Classes;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;
using MagicHeim.UI_s;
using Logger = MagicHeim_Logger.Logger;
using Random = UnityEngine.Random;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Mage_AncientTotem : MH_Skill
{
    private static GameObject RangeShowup;
    private static GameObject TargetPoint;
    private static GameObject TotemPrefab;
    private static GameObject TotemDebuff;


    public Mage_AncientTotem()
    {
        _definition._InternalName = "Mage_Ancienttotem";
        _definition.Name = "$mh_mage_ancienttotem";
        _definition.Description = "$mh_mage_ancienttotem_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Damage Increase", 10f,
            "Value amount (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Damage Increase", 40f,
            "Value amount (Max Lvl)");

        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Manacost", 25f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Manacost", 65f,
            "Manacost amount (Max Lvl)");

        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Cooldown", 90f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Cooldown", 30f,
            "Cooldown amount (Max Lvl)");


        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Max Level", 10,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Required Level To Learn",
            26, "Required Level");

        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            $"Leveling Step", 6,
            "Leveling Step");


        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Mage_AncientTotem_Icon");
        _definition.Video = "https://kg-dev.xyz/skills/MH_Mage_AncientTotem.mp4";
        RangeShowup = MagicHeim.asset.LoadAsset<GameObject>("Mage_AreaShowup");
        TargetPoint = MagicHeim.asset.LoadAsset<GameObject>("Mage_AncientTotem_TargetShowup");
        TotemPrefab = MagicHeim.asset.LoadAsset<GameObject>("Mage_AncientTotem_Prefab");
        TotemDebuff = MagicHeim.asset.LoadAsset<GameObject>("Mage_AncientTotem_Debuff");
        TotemPrefab.AddComponent<AncientTotemComponent>();
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
            __instance.m_namedPrefabs[TotemPrefab.name.GetStableHashCode()] = TotemPrefab;
            __instance.m_namedPrefabs[TotemDebuff.name.GetStableHashCode()] = TotemDebuff;
        }
    }


    public class AncientTotemComponent : MonoBehaviour
    {
        private ZNetView _znv;
        private float counter;
        private float _value;

        static readonly int m_rayMaskSolids = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece",
            "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "hitbox", "character_noenv",
            "vehicle");


        private void Awake()
        {
            _znv = GetComponent<ZNetView>();
        }

        public void Setup(float value)
        {
            _value = value;
        }

        private void FixedUpdate()
        {
            if (!_znv.IsOwner() || !Player.m_localPlayer) return;
            counter += Time.fixedDeltaTime;
            if (counter >= 0.75f)
            {
                counter = 0f;
                Collider[] array = Physics.OverlapSphere(transform.position, 6f, m_rayMaskSolids,
                    QueryTriggerInteraction.UseGlobal);
                HashSet<GameObject> hashSet = new HashSet<GameObject>();
                foreach (Collider collider in array)
                {
                    GameObject gameObject = Projectile.FindHitObject(collider);
                    IDestructible component = gameObject.GetComponent<IDestructible>();
                    if (component != null && !hashSet.Contains(gameObject))
                    {
                        hashSet.Add(gameObject);
                        if (component is Character character)
                        {
                            if (!Utils.IsEnemy(character)) continue;
                            character.m_seman.AddStatusEffect("Mage_AncientTotem_Debuff".GetStableHashCode(), true, 0, _value);
                        }
                    }
                }
            }
        }
    }


    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        MagicHeim._thistype.StartCoroutine(Charge(Cond));
    }

    private static readonly int JumpMask =
        LayerMask.GetMask("terrain", "Default", "piece", "piece_nonsolid", "static_solid");

    private static readonly Vector3 NON_Vector = new Vector3(-100000, 0, 0);


    private IEnumerator Charge(Func<bool> Cond)
    {
        bool cancel = false;
        SkillChargeUI.ShowCharge(this);
        Player p = Player.m_localPlayer;
        float maxDistance = 40f;
        GameObject rangeShowup =
            UnityEngine.Object.Instantiate(RangeShowup, p.transform.position, Quaternion.identity);
        GameObject targetPoint =
            UnityEngine.Object.Instantiate(TargetPoint, p.transform.position, Quaternion.identity);
        rangeShowup.GetComponent<CircleProjector>().m_radius = maxDistance;
        rangeShowup.GetComponent<CircleProjector>().Update();
        Vector3 target = NON_Vector;
        while (Cond() && p && !p.IsDead())
        {
            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                cancel = true;
                break;
            }

            rangeShowup.transform.position = p.transform.position;
            bool castHit = Physics.Raycast(Utils.GetPerfectEyePosition(), p.GetLookDir(), out var raycast,
                _definition.MaxLvlValue.Value + 10f,
                JumpMask);
            if (castHit && raycast.collider)
            {
                targetPoint.SetActive(true);
                target = raycast.point;
                targetPoint.transform.position = target;
            }
            else
            {
                targetPoint.SetActive(false);
                target = NON_Vector;
            }

            yield return null;
        }

        SkillChargeUI.RemoveCharge(this);
        if (!cancel && p && !p.IsDead() && target != NON_Vector &&
            global::Utils.DistanceXZ(target, p.transform.position) <= maxDistance)
        {
            Vector3 rot = (target - p.transform.position).normalized;
            rot.y = 0;
            p.transform.rotation = Quaternion.LookRotation(rot);
            StartCooldown(this.CalculateSkillCooldown());
            var go = UnityEngine.Object.Instantiate(TotemPrefab, target, Quaternion.identity);
            go.GetComponent<AncientTotemComponent>().Setup(this.CalculateSkillValue());
            p.m_zanim.SetTrigger(
                ClassAnimationReplace.MH_AnimationNames[ClassAnimationReplace.MH_Animation.MageSummon]);
        }
        else
        {
            if (!cancel)
            {
                p.AddEitr(this.CalculateSkillManacost());
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "<color=#00FFFF>Too far</color>");
            }
        }

        UnityEngine.Object.Destroy(rangeShowup);
        UnityEngine.Object.Destroy(targetPoint);
    }


    public class SE_Mage_AncientTotem_Debuff : StatusEffect
    {
        public SE_Mage_AncientTotem_Debuff()
        {
            name = "Mage_AncientTotem_Debuff";
            m_tooltip = "Elemental Magic damage income increased";
            m_icon = CachedIcon;
            m_name = "Blind";
            m_ttl = 1;
            m_startEffects = new EffectList
            {
                m_effectPrefabs = new[]
                {
                    new EffectList.EffectData()
                    {
                        m_attach = true, m_enabled = true, m_inheritParentRotation = true,
                        m_inheritParentScale = true,
                        m_prefab = TotemDebuff
                    }
                }
            };
        }

        private float value;

        public override void SetLevel(int itemLevel, float skillLevel)
        {
            value = skillLevel;
        }

        public override void OnDamaged(HitData hit, Character attacker)
        {
            if (hit.m_skill != Skills.SkillType.ElementalMagic) return;
            hit.ApplyModifier(1 + value / 100f);
        }
    }

    public static class Mage_ThunderSock_DB_Patches
    {
        private static void Add_SE(ObjectDB odb)
        {
            if (ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0 ||
                ObjectDB.instance.GetItemPrefab("Amber") == null) return;

            if (!odb.m_StatusEffects.Find(se => se.name == "Mage_AncientTotem_Debuff"))
                odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Mage_AncientTotem_Debuff>());
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
        return "<color=red>Precast, Debuff</color>";
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

        builder.AppendLine($"Elemental Damage Increase Debuff: {Math.Round(currentValue, 1)}%");
        builder.AppendLine($"Cooldown: {Math.Round(currentCooldown, 1)}");
        builder.AppendLine($"Manacost: {Math.Round(currentManacost, 1)}");

        if (this.Level < maxLevel && this.Level > 0)
        {
            float nextValue = this.CalculateSkillValue(forLevel + 1);
            float nextCooldown = this.CalculateSkillCooldown(forLevel + 1);
            float nextManacost = this.CalculateSkillManacost(forLevel + 1);
            float valueDiff = nextValue - currentValue;
            float cooldownDiff = nextCooldown - currentCooldown;
            float manacostDiff = nextManacost - currentManacost;

            var roundedValueDiff = Math.Round(valueDiff, 1);
            var roundedCooldownDiff = Math.Round(cooldownDiff, 1);
            var roundedManacostDiff = Math.Round(manacostDiff, 1);

            builder.AppendLine($"\nNext Level:");
            builder.AppendLine(
                $"Elemental Damage Increase Debuff: {Math.Round(nextValue, 1)}% <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
            builder.AppendLine(
                $"Cooldown: {Math.Round(nextCooldown, 1)} <color=green>({(roundedCooldownDiff > 0 ? "+" : "")}{roundedCooldownDiff})</color>");
            builder.AppendLine(
                $"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }


        return builder.ToString();
    }

    public override Class PreferableClass => Class.Mage;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => new Color(0.74f, 0.08f, 1f);
}