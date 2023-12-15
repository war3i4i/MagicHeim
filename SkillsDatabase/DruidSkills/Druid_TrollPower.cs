using System.Text;
using JetBrains.Annotations;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Interfaces;
using MagicHeim.UI_s;

namespace MagicHeim.SkillsDatabase.DruidSkills;

public sealed class Druid_TrollPower : MH_Skill
{
    private static readonly GameObject InactiveGO = new(){name = "Troll_GO_DruidTrollSmash_Inactive", hideFlags = HideFlags.HideAndDontSave};
    private static GameObject Troll_GO;
    private static GameObject Explosion;
    private static GameObject Preload;
    
    private static GameObject RangeShowup;
    private static GameObject TargetPoint;
    

    public Druid_TrollPower()
    {
        InactiveGO.SetActive(false);
        _definition._InternalName = "Druid_TrollPower";
        _definition.Name = "$mh_druid_trollpower";
        _definition.Description = "$mh_druid_trollpower_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Damage", 1f,
            "Value amount (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Damage", 10f,
            "Value amount (Max Lvl)");

        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Manacost", 1f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Manacost", 10f,
            "Manacost amount (Max Lvl)");

        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Cooldown", 10f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Cooldown", 1f,
            "Cooldown amount (Max Lvl)");


        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 10,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn",
            1, "Required Level");


        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 1,
            "Leveling Step");

        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_TrollSmash_Icon");
        _definition.Video = "https://kg.sayless.eu/skills/MH_Druid_TrollSmash.mp4";
        RangeShowup = MagicHeim.asset.LoadAsset<GameObject>("Druid_AreaShowup");
        TargetPoint = MagicHeim.asset.LoadAsset<GameObject>("Druid_TrollSmash_AreaShowup");
        Explosion = MagicHeim.asset.LoadAsset<GameObject>("Druid_TrollSmash_Explosion");
        Preload = MagicHeim.asset.LoadAsset<GameObject>("Druid_TrollSmash_Preload");
        this.InitRequiredItemFirstHalf("Wood", 1, 1.88f);
        this.InitRequiredItemSecondHalf("Coins", 1, 1.88f);
        this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScene_Awake_Patch
    {
        [UsedImplicitly]
        static void Postfix(ZNetScene __instance)
        { 
            if (!Troll_GO)
            {
                GameObject _origGO = __instance.GetPrefab("Troll").GetComponentInChildren<Animator>(true).gameObject;
                Troll_GO = UnityEngine.Object.Instantiate(_origGO, InactiveGO.transform);
                Troll_GO.name = "Troll_GO_DruidTrollSmash";
                Troll_GO.gameObject.SetActive(true);
                Troll_GO.gameObject.transform.localPosition = Vector3.zero;
                Troll_GO.gameObject.transform.localRotation = Quaternion.identity;

                Material mat = MagicHeim.asset.LoadAsset<Material>("DruidMat");
                foreach (Renderer renderer in Troll_GO.GetComponentsInChildren<Renderer>())
                    renderer.sharedMaterial = mat;

                TimedDestruction timed = Troll_GO.AddComponent<TimedDestruction>();
                timed.m_timeout = 3f;
                timed.m_triggerOnAwake = true;
                
                Troll_GO.AddComponent<ZNetView>();
                UnityEngine.Object.Destroy(Troll_GO.GetComponent<CharacterAnimEvent>());
                UnityEngine.Object.Destroy(Troll_GO.GetComponent<LevelEffects>());
                Troll_GO.AddComponent<TrollSmash_Local>();
            }
            __instance.m_namedPrefabs[Troll_GO.name.GetStableHashCode()] = Troll_GO;
            __instance.m_namedPrefabs[Explosion.name.GetStableHashCode()] = Explosion;
            __instance.m_namedPrefabs[Preload.name.GetStableHashCode()] = Preload;
        }
    }

    static readonly int m_rayMaskSolids = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece",
        "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "hitbox", "character_noenv",
        "vehicle");
    
    private class TrollSmash_Local : MonoBehaviour
    {
        private ZNetView _znv;
        private float _damage;
        private Vector3 _target = NON_Vector;
        
        public void Setup(float damage, Vector3 target)
        {
            _target = target;
            _damage = damage;
        }

        private void Awake()
        {
            _znv = GetComponent<ZNetView>();
            GetComponent<Animator>().SetTrigger(SwingLogv);
            if (_znv.IsOwner())
            {
                Invoke(nameof(DoDamage), 1.5f);
            }
        }
        private void DoDamage()
        {
            if (_target == NON_Vector) return;
            Instantiate(Explosion, _target, Quaternion.identity);
            Collider[] array = Physics.OverlapSphere(_target, 8f, m_rayMaskSolids, QueryTriggerInteraction.UseGlobal);
            HashSet<GameObject> hashSet = new HashSet<GameObject>();
            foreach (Collider collider in array)
            {
                GameObject gameObject = Projectile.FindHitObject(collider);
                IDestructible component = gameObject.GetComponent<IDestructible>();
                if (component != null && hashSet.Add(gameObject))
                {
                    if (component is Character character)
                    {
                        if (!Utils.IsEnemy(character)) continue;
                        HitData hit = new();
                        hit.m_skill = Skills.SkillType.ElementalMagic;
                        hit.m_damage.m_blunt = _damage;
                        hit.m_point = collider.ClosestPoint(_target);
                        hit.m_ranged = true;
                        hit.SetAttacker(Player.m_localPlayer);
                        character.DamageMH(hit);
                        character.Stagger(Vector3.zero);
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
    private static readonly int SwingLogv = Animator.StringToHash("swing_logv");


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
            bool castHit = Physics.Raycast(Utils.GetPerfectEyePosition(), p.GetLookDir(), out RaycastHit raycast,
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
        if (!cancel && p && !p.IsDead() && target != NON_Vector && global::Utils.DistanceXZ(target, p.transform.position) <= maxDistance)
        {
            Vector3 rot = (target - p.transform.position).normalized;
            rot.y = 0;
            p.transform.rotation = Quaternion.LookRotation(rot);
            StartCooldown(this.CalculateSkillCooldown());
            p.m_zanim.SetTrigger(ClassAnimationReplace.MH_AnimationNames[ClassAnimationReplace.MH_Animation.MageSummon]);
            Vector3 vfxPos = target + (rot * 2f);
            GameObject troll = UnityEngine.Object.Instantiate(Troll_GO, vfxPos, Quaternion.LookRotation((target - vfxPos).normalized));
            troll.GetComponent<TrollSmash_Local>().Setup(this.CalculateSkillValue(), target);
            UnityEngine.Object.Instantiate(Preload, vfxPos, Quaternion.identity);
        }
        else
        {
            if (!cancel)
            {
                p.AddEitr(this.CalculateSkillManacost());
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "<color=#00FF00>Too far</color>");
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
        return "<color=red>Precast, AoE, Damage, Stagger</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine("\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentValue = this.CalculateSkillValue(forLevel);
        float currentCooldown = this.CalculateSkillCooldown(forLevel);
        float currentManacost = this.CalculateSkillManacost(forLevel);

        builder.AppendLine($"Damage: <color=yellow>Blunt {Math.Round(currentValue, 1)}</color>");
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

            double roundedValueDiff = Math.Round(valueDiff, 1);
            double roundedCooldownDiff = Math.Round(cooldownDiff, 1);
            double roundedManacostDiff = Math.Round(manacostDiff, 1);

            builder.AppendLine("\nNext Level:");
            builder.AppendLine($"Damage: <color=yellow>Blunt {Math.Round(nextValue, 1)}</color> <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
            builder.AppendLine($"Cooldown: {Math.Round(nextCooldown, 1)} <color=green>({(roundedCooldownDiff > 0 ? "+" : "")}{roundedCooldownDiff})</color>");
            builder.AppendLine($"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }


        return builder.ToString();
    }

    public override bool CanRightClickCast => false;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => new Color(0.35f, 1f, 0.33f);
}