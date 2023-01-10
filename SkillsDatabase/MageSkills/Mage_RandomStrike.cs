using System.Text;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Classes;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;
using MagicHeim.UI_s;
using Random = UnityEngine.Random;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Mage_RandomStrike : MH_Skill
{
    private static GameObject Teleport_RangeShowup;
    private static GameObject Teleport_TargetPoint;

    private static GameObject Fire_Preload;
    private static GameObject Fire_Explosion;
    private static GameObject Ice_Preload;
    private static GameObject Ice_Explosion;
    private static GameObject Lightning_Preload;
    private static GameObject Lightning_Explosion;

    public enum RandomType
    {
        Fire,
        Ice,
        Lightning
    }

    public Mage_RandomStrike()
    {
        _definition._InternalName = "Mage_Randomstrike";
        _definition.Name = "$mh_mage_randomstrike";
        _definition.Description = "$mh_mage_randomstrike_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Damage", 30f,
            "Value amount (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Damage", 105f,
            "Value amount (Max Lvl)");

        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Manacost", 25f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Manacost", 35f,
            "Manacost amount (Max Lvl)");

        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Cooldown", 18f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Cooldown", 9f,
            "Cooldown amount (Max Lvl)");


        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Max Level", 10,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Required Level To Learn",
            33, "Required Level");

        
        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            $"Leveling Step", 4,
            "Leveling Step");

        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Mage_RandomStrike_Icon");
        _definition.Video = "https://kg-dev.xyz/skills/MH_Mage_RandomStrike.mp4";
        Teleport_RangeShowup = MagicHeim.asset.LoadAsset<GameObject>("Mage_AreaShowup");
        Teleport_TargetPoint = MagicHeim.asset.LoadAsset<GameObject>("Mage_RandomStrike_TargetShowup");

        Fire_Preload = MagicHeim.asset.LoadAsset<GameObject>("Mage_RandomStrike_FirePreload");
        Fire_Explosion = MagicHeim.asset.LoadAsset<GameObject>("Mage_RandomStrike_FireExplosion");
        Ice_Preload = MagicHeim.asset.LoadAsset<GameObject>("Mage_RandomStrike_IcePreload");
        Ice_Explosion = MagicHeim.asset.LoadAsset<GameObject>("Mage_RandomStrike_IceExplosion");
        Lightning_Preload = MagicHeim.asset.LoadAsset<GameObject>("Mage_RandomStrike_LightningPreload");
        Lightning_Explosion = MagicHeim.asset.LoadAsset<GameObject>("Mage_RandomStrike_LightningExplosion");
        
        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScene_Awake_Patch
    {
        static void Postfix(ZNetScene __instance)
        {
            __instance.m_namedPrefabs[Ice_Explosion.name.GetStableHashCode()] = Ice_Explosion;
            __instance.m_namedPrefabs[Ice_Preload.name.GetStableHashCode()] = Ice_Preload;
            __instance.m_namedPrefabs[Fire_Explosion.name.GetStableHashCode()] = Fire_Explosion;
            __instance.m_namedPrefabs[Fire_Preload.name.GetStableHashCode()] = Fire_Preload;
            __instance.m_namedPrefabs[Lightning_Explosion.name.GetStableHashCode()] = Lightning_Explosion;
            __instance.m_namedPrefabs[Lightning_Preload.name.GetStableHashCode()] = Lightning_Preload;
        }
    }


    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        MagicHeim._thistype.StartCoroutine(Charge(Cond));
    }

    private static readonly int JumpMask =
        LayerMask.GetMask("terrain", "Default", "piece", "piece_nonsolid", "static_solid");

    private static Vector3 NON_Vector = new Vector3(-100000, 0, 0);


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
            p.m_zanim.SetTrigger(ClassAnimationReplace.MH_AnimationNames[ClassAnimationReplace.MH_Animation.MageSummon]);
            p.StartCoroutine(DelayExplosion(target));
        }
        else
        {
            if (!cancel)
            {
                p.AddEitr(this.CalculateSkillManacost());
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "<color=cyan>Too far</color>");
            }
        }

        UnityEngine.Object.Destroy(rangeShowup);
        UnityEngine.Object.Destroy(targetPoint);
    }


    static int m_rayMaskSolids = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece",
        "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "hitbox", "character_noenv",
        "vehicle");

    private IEnumerator DelayExplosion(Vector3 pos)
    {
        RandomType random = (RandomType)Random.Range(0, 3);
        GameObject preload = random switch
        {
            RandomType.Fire => Fire_Preload,
            RandomType.Ice => Ice_Preload,
            RandomType.Lightning => Lightning_Preload,
            _ => Fire_Preload
        };
        GameObject prefab = random switch
        {
            RandomType.Fire => Fire_Explosion,
            RandomType.Ice => Ice_Explosion,
            RandomType.Lightning => Lightning_Explosion,
            _ => Fire_Explosion
        };
        UnityEngine.Object.Instantiate(preload, pos, Quaternion.identity);
        yield return new WaitForSeconds(1f);
        UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity);

        Collider[] array = Physics.OverlapSphere(pos, 4f, m_rayMaskSolids, QueryTriggerInteraction.UseGlobal);
        HashSet<GameObject> hashSet = new HashSet<GameObject>();
        var damage = this.CalculateSkillValue();
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
                    hit.m_damage.m_blunt = damage / 2f;
                    if (random == RandomType.Fire)
                        hit.m_damage.m_fire = damage / 2f;
                    else if (random == RandomType.Ice)
                        hit.m_damage.m_frost = damage / 2f; 
                    else if (random == RandomType.Lightning)
                        hit.m_damage.m_lightning = damage / 2f;
                    hit.m_point = collider.ClosestPoint(pos);
                    hit.m_ranged = true;
                    hit.SetAttacker(Player.m_localPlayer);
                    component.DamageMH(hit);
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
        return "<color=red>Precast, Random Element, AoE, Damage</color>";
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

        builder.AppendLine($"Damage (Random Element): {Math.Round(currentValue, 1)}");
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
            builder.AppendLine($"Damage (Random Element): {Math.Round(nextValue, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
            builder.AppendLine($"Cooldown: {Math.Round(nextCooldown, 1)} <color=green>({(roundedCooldownDiff > 0 ? "+" : "")}{roundedCooldownDiff})</color>");
            builder.AppendLine($"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }

        

        return builder.ToString();
    }

    public override Class PreferableClass => Class.Mage;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => new Color(0.99f, 1f, 0.15f);
}