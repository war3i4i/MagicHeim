using System.Text;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Interfaces;
using MagicHeim.UI_s;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Mage_EnergyBlast : MH_Skill
{
    private static GameObject Energyblast_Prefab;
    private static GameObject Energyblast_Explosion;

    public Mage_EnergyBlast()
    {
        _definition._InternalName = "Mage_Energyblast";
        _definition.Animation = ClassAnimationReplace.MH_AnimationNames[ClassAnimationReplace.MH_Animation.MageSummon];
        _definition.Name = "$mh_mage_energyblast";
        _definition.Description = "$mh_mage_energyblast_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Damage", 65f,
            "Damage amount (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Damage", 115f,
            "Damage amount (Max Lvl)");

        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Manacost", 20f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Manacost", 45f,
            "Manacost amount (Max Lvl)");

        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Cooldown", 15f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Cooldown", 8f,
            "Cooldown amount (Max Lvl)");

        _definition.MinLvlAoE = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl AoE", 4,
            "AoE amount (Min Lvl)");
        _definition.MaxLvlAoE = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl AoE", 12,
            "AoE amount (Max Lvl)");

        _definition.MinLvlChargeTime = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Charge Time", 8,
            "Charge Time amount (Min Lvl)");
        _definition.MaxLvlChargeTime = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Charge Time", 4,
            "Charge Time amount (Max Lvl)");

        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Max Level", 10,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Required Level To Learn",
            43, "Required Level");


        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            $"Leveling Step", 3,
            "Leveling Step");

        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
        this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
        this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);

        _definition.AnimationTime = 0.6f;
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Mage_EnergyBlast_Icon");
        _definition.Video = "https://kg.sayless.eu/skills/MH_Mage_EnergyBlast.mp4";
        Energyblast_Prefab = MagicHeim.asset.LoadAsset<GameObject>("Mage_EnergyBlast");
        Energyblast_Prefab.AddComponent<EnergyBlastComponent>();
        Energyblast_Explosion = MagicHeim.asset.LoadAsset<GameObject>("Mage_EnergyBlast_Impact");
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScene_Awake_Patch
    {
        static void Postfix(ZNetScene __instance)
        {
            __instance.m_namedPrefabs[Energyblast_Prefab.name.GetStableHashCode()] = Energyblast_Prefab;
            __instance.m_namedPrefabs[Energyblast_Explosion.name.GetStableHashCode()] = Energyblast_Explosion;
        }
    }


    private static readonly LayerMask mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece",
        "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "hitbox", "character_noenv",
        "vehicle");


    public class EnergyBlastComponent : MonoBehaviour
    {
        static readonly int m_rayMaskSolids = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece",
            "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "hitbox", "character_noenv",
            "vehicle");


        public void Setup(Vector3 dir, float aoe, float damage)
        {
            StartCoroutine(Move(dir, aoe, damage));
        }

        private void Explosion(float aoe, float damage)
        {
            var explosion = Instantiate(Energyblast_Explosion, transform.position, Quaternion.identity);
            explosion.transform.localScale *= aoe;
            Collider[] array = Physics.OverlapSphere(transform.position, aoe, m_rayMaskSolids,
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
                        hit.m_damage.m_lightning = damage / 2f;
                        hit.m_damage.m_fire = damage / 2f;
                        hit.m_point = collider.ClosestPoint(transform.position);
                        hit.m_ranged = true;
                        hit.m_pushForce = 5f;
                        hit.SetAttacker(Player.m_localPlayer);
                        component.DamageMH(hit);
                    }
                    else
                    {
                        HitData hit = new();
                        hit.m_skill = Skills.SkillType.ElementalMagic;
                        hit.m_damage.m_chop = damage / 5f;
                        hit.m_damage.m_pickaxe = damage / 5f;
                        hit.m_toolTier = 100;
                        hit.m_point = collider.ClosestPointOnBounds(transform.position);
                        hit.m_ranged = true;
                        hit.m_pushForce = 5f;
                        hit.SetAttacker(Player.m_localPlayer);
                        component.DamageMH(hit);
                    }
                }
            }
        }

        private IEnumerator Move(Vector3 dir, float aoe, float damage)
        {
            bool didhit = false;
            float speed = 15f;
            float count = 0;
            while (count <= 2f)
            {
                count += Time.deltaTime;

                var oldPos = transform.position;
                transform.position += dir * speed * Time.deltaTime;
                var newPos = transform.position;
                Vector3 normalized = newPos - oldPos;
                RaycastHit[] array = Physics.SphereCastAll(transform.position, 0.2f, normalized.normalized,
                    normalized.magnitude, mask);
                if (array.Length != 0)
                {
                    Array.Sort(array, (x, y) => x.distance.CompareTo(y.distance));
                    foreach (RaycastHit raycastHit in array)
                    {
                        GameObject go = raycastHit.collider ? Projectile.FindHitObject(raycastHit.collider) : null;
                        IDestructible destructible = go ? go.GetComponent<IDestructible>() : null;
                        if (destructible is Character c && !Utils.IsEnemy(c)) continue;
                        Explosion(aoe, damage);
                        didhit = true;
                        count = 100f;
                        break;
                    }
                }

                yield return null;
            }

            if (!didhit) Explosion(aoe, damage);
            ZNetScene.instance.Destroy(gameObject);
        }
    }


    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        MagicHeim._thistype.StartCoroutine(Charge(Cond));
    }

    private IEnumerator Charge(Func<bool> Cond)
    {
        float charge = 0f;
        float maxCharge = Mathf.Max(0.1f, this.CalculateSkillChargeTime());
        float maxAoe = this.CalculateSkillAoe();
        float cooldown = this.CalculateSkillCooldown();
        SkillChargeUI.ShowCharge(this, maxCharge);
        float scaleAmount = maxAoe / maxCharge / 4f;
        float animationTime = _definition.AnimationTime * 1f;
        Player p = Player.m_localPlayer;
        Vector3 initPos = p.transform.position;
        Vector3 target = Utils.GetPerfectEyePosition() + p.m_eye.transform.forward * 100f;
        Vector3 rot = (target - p.transform.position).normalized;
        rot.y = 0;
        p.transform.rotation = Quaternion.LookRotation(rot);
        var go = UnityEngine.Object.Instantiate(Energyblast_Prefab,
            p.transform.position + Vector3.up * 1.2f + GameCamera.instance.transform.forward * 2f,
            GameCamera.instance.transform.rotation);
        while (Cond() && charge < maxCharge && p && !p.IsDead())
        {
            var dt = Time.deltaTime;
            charge += dt;
            go.transform.localScale += Vector3.one * scaleAmount * Time.deltaTime;
            target = Utils.GetPerfectEyePosition() + p.m_eye.transform.forward * 100f;
            rot = (target - p.transform.position).normalized;
            rot.y = 0;
            p.transform.rotation = Quaternion.LookRotation(rot);
            p.m_body.position = new Vector3(initPos.x, p.transform.position.y, initPos.z);
            go.transform.position = p.transform.position + Vector3.up + GameCamera.instance.transform.forward * 2f;
            animationTime -= dt;
            if (animationTime <= 0f)
            {
                animationTime = _definition.AnimationTime * 1.1f;
                SkillCastHelper.PlayAnimation(this);
            }

            yield return null;
        }

        StartCooldown(cooldown);
        SkillChargeUI.RemoveCharge(this);
        if (!p || p.IsDead())
        {
            ZNetScene.instance.Destroy(go);
            yield break;
        }

        var direction = (target - go.transform.position).normalized;
        float aoe = this.CalculateSkillAoe().CalculateValueCharged(maxCharge, charge, 25);
        float damage = this.CalculateSkillValue().CalculateValueCharged(maxCharge, charge, 25);
        go.GetComponent<EnergyBlastComponent>().Setup(direction, aoe, damage);
    }


    public override bool CanExecute()
    {
        return !Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Chargable, Projectile, AoE, Damage</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine($"\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentValue = this.CalculateSkillValue(forLevel);
        float currentAoe = this.CalculateSkillAoe(forLevel);
        float currentChargeTime = this.CalculateSkillChargeTime(forLevel);
        float currentCooldown = this.CalculateSkillCooldown(forLevel);
        float currentManacost = this.CalculateSkillManacost(forLevel);

        builder.AppendLine(
            $"Damage: <color=red>Fire {Math.Round(currentValue / 2f, 1)}</color> + <color=blue>Lightning {Math.Round(currentValue / 2f, 1)}</color>");
        builder.AppendLine($"Cooldown: {Math.Round(currentCooldown, 1)}");
        builder.AppendLine($"Manacost: {Math.Round(currentManacost, 1)}");
        builder.AppendLine($"Area of Effect: {Math.Round(currentAoe, 1)}");
        builder.AppendLine($"Charge Time: {Math.Round(currentChargeTime, 1)}");

        if (Level < maxLevel && Level > 0)
        {
            float nextValue = this.CalculateSkillValue(forLevel + 1);
            float nextAoe = this.CalculateSkillAoe(forLevel + 1);
            float nextChargeTime = this.CalculateSkillChargeTime(forLevel + 1);
            float nextCooldown = this.CalculateSkillCooldown(forLevel + 1);
            float nextManacost = this.CalculateSkillManacost(forLevel + 1);
            float valueDiff = nextValue - currentValue;
            float aoeDiff = nextAoe - currentAoe;
            float chargeTimeDiff = nextChargeTime - currentChargeTime;
            float cooldownDiff = nextCooldown - currentCooldown;
            float manacostDiff = nextManacost - currentManacost;

            var roundedValueDiff = Math.Round(valueDiff, 1);
            var roundedAoeDiff = Math.Round(aoeDiff, 1);
            var roundedChargeTimeDiff = Math.Round(chargeTimeDiff, 1);
            var roundedCooldownDiff = Math.Round(cooldownDiff, 1);
            var roundedManacostDiff = Math.Round(manacostDiff, 1);

            builder.AppendLine($"\nNext Level:");
            builder.AppendLine(
                $"Damage: <color=red>Fire {Math.Round(nextValue / 2f, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color></color> + <color=blue>Lightning {Math.Round(nextValue / 2f, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color></color>");
            builder.AppendLine(
                $"Cooldown: {Math.Round(nextCooldown, 1)} <color=green>({(roundedCooldownDiff > 0 ? "+" : "")}{roundedCooldownDiff})</color>");
            builder.AppendLine(
                $"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
            builder.AppendLine(
                $"Area of Effect: {Math.Round(nextAoe, 1)} <color=green>({(roundedAoeDiff > 0 ? "+" : "")}{roundedAoeDiff})</color>");
            builder.AppendLine(
                $"Charge Time: {Math.Round(nextChargeTime, 1)} <color=green>({(roundedChargeTimeDiff > 0 ? "+" : "")}{roundedChargeTimeDiff})</color>");
        }


        return builder.ToString();
    }

    public override bool CanRightClickCast => false;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => new Color(1f, 0.76f, 0.21f);
}