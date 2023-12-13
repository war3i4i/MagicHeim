using System.Text;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Interfaces;
using MagicHeim.UI_s;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Mage_BlackHole : MH_Skill
{
    private static GameObject Teleport_RangeShowup;
    private static GameObject Teleport_TargetPoint;
    private static GameObject BlackHole_Prefab;


    public Mage_BlackHole()
    {
        _definition._InternalName = "Mage_Blackhole";
        _definition.Name = "$mh_mage_blackhole";
        _definition.Description = "$mh_mage_blackhole_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Damage", 20f,
            "Value amount (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Damage", 60f,
            "Value amount (Max Lvl)");

        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Manacost", 30f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Manacost", 60f,
            "Manacost amount (Max Lvl)");

        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Cooldown", 40f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Cooldown", 26f,
            "Cooldown amount (Max Lvl)");


        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Max Level", 10,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Required Level To Learn",
            30, "Required Level");


        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            $"Leveling Step", 5,
            "Leveling Step");

        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Mage_BlackHole_Icon");
        _definition.Video = "https://kg.sayless.eu/skills/MH_Mage_BlackHole.mp4";
        Teleport_RangeShowup = MagicHeim.asset.LoadAsset<GameObject>("Mage_AreaShowup");
        Teleport_TargetPoint = MagicHeim.asset.LoadAsset<GameObject>("Mage_BlackHole_TargetShowup");
        BlackHole_Prefab = MagicHeim.asset.LoadAsset<GameObject>("Mage_BlackHole_Prefab");
        BlackHole_Prefab.AddComponent<BlackHoleComponent>();

        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
        this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
        this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScene_Awake_Patch
    {
        static void Postfix(ZNetScene __instance)
        {
            __instance.m_namedPrefabs[BlackHole_Prefab.name.GetStableHashCode()] = BlackHole_Prefab;
        }
    }


    public class BlackHoleComponent : MonoBehaviour
    {
        private ZNetView _znv;
        private float counter;
        private float _damage;

        static readonly int m_rayMaskSolids = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece",
            "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "hitbox", "character_noenv",
            "vehicle");


        private void Awake()
        {
            _znv = GetComponent<ZNetView>();
        }

        public void Setup(float damage)
        {
            _damage = damage;
        }

        private void FixedUpdate()
        {
            List<Character> list = new();
            Character.GetCharactersInRange(transform.position, 8.4f, list);
            list = list.Where(d =>
                d.m_nview.IsValid() && d.m_nview.IsOwner() && d.GetHealth() > 0 && !d.IsTamed() &&
                (d == Player.m_localPlayer || Utils.IsEnemy(d))).ToList();
            foreach (var VARIABLE in list)
            {
                float distance = Vector3.Distance(transform.position, VARIABLE.transform.position);
                float multiplier = distance / 40;
                Vector3 angle = (transform.position - VARIABLE.transform.position).normalized * multiplier;
                VARIABLE.transform.position += angle;
            }

            if (!_znv.IsOwner() || !Player.m_localPlayer) return;
            counter += Time.fixedDeltaTime;
            if (counter >= 1f)
            {
                counter = 0f;
                Collider[] array = Physics.OverlapSphere(transform.position, 8f, m_rayMaskSolids,
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
                            HitData hit = new();
                            hit.m_skill = Skills.SkillType.ElementalMagic;
                            hit.m_damage.m_blunt = _damage;
                            hit.m_point = collider.ClosestPoint(transform.position);
                            hit.m_ranged = true;
                            hit.SetAttacker(Player.m_localPlayer);
                            component.DamageMH(hit);
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
            UnityEngine.Object.Instantiate(Teleport_RangeShowup, p.transform.position, Quaternion.identity);
        GameObject targetPoint =
            UnityEngine.Object.Instantiate(Teleport_TargetPoint, p.transform.position, Quaternion.identity);
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
            var go = UnityEngine.Object.Instantiate(BlackHole_Prefab, target, Quaternion.identity);
            go.GetComponent<BlackHoleComponent>().Setup(this.CalculateSkillValue());
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


    public override bool CanExecute()
    {
        return !Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Precast, AoE, Damage, Can Affect Owner, Pulls Inside</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine($"\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentValue = this.CalculateSkillValue(forLevel);
        float currentCooldown = this.CalculateSkillCooldown(forLevel);
        float currentManacost = this.CalculateSkillManacost(forLevel);

        builder.AppendLine($"Damage (Per Second): <color=yellow>Blunt  {Math.Round(currentValue, 1)}</color>");
        builder.AppendLine($"Cooldown: {Math.Round(currentCooldown, 1)}");
        builder.AppendLine($"Manacost: {Math.Round(currentManacost, 1)}");

        if (Level < maxLevel && Level > 0)
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
                $"Damage (Per Second): <color=yellow>Blunt  {Math.Round(nextValue, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color></color>");
            builder.AppendLine(
                $"Cooldown: {Math.Round(nextCooldown, 1)} <color=green>({(roundedCooldownDiff > 0 ? "+" : "")}{roundedCooldownDiff})</color>");
            builder.AppendLine(
                $"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }


        return builder.ToString();
    }

    public override bool CanRightClickCast => false;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => new Color(0.18f, 0.13f, 0.22f);
}