﻿using System.Text;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Interfaces;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Mage_Lightningball : MH_Skill
{
    private static GameObject Lightningball_Prefab;
    private static GameObject Lightningball_Explosion;

    public Mage_Lightningball()
    {
        _definition._InternalName = "Mage_Lightningball";
        _definition.Animation =
            ClassAnimationReplace.MH_AnimationNames[ClassAnimationReplace.MH_Animation.MageProjectile];
        _definition.Name = "$mh_mage_lightningball";
        _definition.Description = "$mh_mage_lightningball_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Damage", 8f,
            "Damage amount (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Damage", 60f,
            "Damage amount (Max Lvl)");

        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Manacost", 15f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Manacost", 25f,
            "Manacost amount (Max Lvl)");

        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Cooldown", 12f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Cooldown", 6f,
            "Cooldown amount (Max Lvl)");


        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 10,
            "Max Skill Level");

        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn",
            4, "Required Level");

        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 6,
            "Leveling Step");


        _definition.AnimationTime = 0.5f;
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Mage_Lightningball_Icon");
        _definition.Video = "https://kg.sayless.eu/skills/MH_Mage_Lightningball.mp4";
        Lightningball_Prefab = MagicHeim.asset.LoadAsset<GameObject>("Mage_Lightningball_Prefab");
        Lightningball_Prefab.AddComponent<LightningBallComponent>();
        Lightningball_Explosion = MagicHeim.asset.LoadAsset<GameObject>("Mage_Lightningball_Explosion");

        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
        this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
        this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScene_Awake_Patch
    {
        static void Postfix(ZNetScene __instance)
        {
            __instance.m_namedPrefabs[Lightningball_Prefab.name.GetStableHashCode()] = Lightningball_Prefab;
            __instance.m_namedPrefabs[Lightningball_Explosion.name.GetStableHashCode()] = Lightningball_Explosion;
        }
    }


    private static readonly LayerMask mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece",
        "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "hitbox", "character_noenv",
        "vehicle");


    public class LightningBallComponent : MonoBehaviour
    {
        static readonly int m_rayMaskSolids = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece",
            "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "hitbox", "character_noenv",
            "vehicle");


        public void Setup(Vector3 dir, float damage)
        {
            StartCoroutine(Move(dir, damage));
        }

        private void Explosion(float damage)
        {
            GameObject explosion = Instantiate(Lightningball_Explosion, transform.position, Quaternion.identity);
            Collider[] array = Physics.OverlapSphere(transform.position, 4f, m_rayMaskSolids,
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
                        hit.m_damage.m_blunt = damage / 2f;
                        hit.m_point = collider.ClosestPoint(transform.position);
                        hit.m_ranged = true;
                        hit.SetAttacker(Player.m_localPlayer);
                        component.DamageMH(hit);
                    }
                }
            }
        }

        private IEnumerator Move(Vector3 dir, float damage)
        {
            bool didhit = false;
            float speed = 40f;
            float count = 0;
            while (count <= 1f)
            {
                count += Time.deltaTime;

                Vector3 oldPos = transform.position;
                transform.position += dir * speed * Time.deltaTime;
                Vector3 newPos = transform.position;
                Vector3 normalized = newPos - oldPos;
                RaycastHit[] array = Physics.SphereCastAll(transform.position, 0.15f, normalized.normalized,
                    normalized.magnitude, mask);
                if (array.Length != 0)
                {
                    Array.Sort(array, (x, y) => x.distance.CompareTo(y.distance));
                    foreach (RaycastHit raycastHit in array)
                    {
                        GameObject go = raycastHit.collider ? Projectile.FindHitObject(raycastHit.collider) : null;
                        IDestructible destructible = go ? go.GetComponent<IDestructible>() : null;
                        if (destructible is Character c && !Utils.IsEnemy(c)) continue;
                        Explosion(damage);
                        didhit = true;
                        count = 100f;
                        break;
                    }
                }

                yield return null;
            }

            if (!didhit) Explosion(damage);
            ZNetScene.instance.Destroy(gameObject);
        }
    }


    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        float cooldown = this.CalculateSkillCooldown();
        Player p = Player.m_localPlayer;
        Vector3 target = Utils.GetPerfectEyePosition() + p.m_eye.transform.forward * 100f;
        Vector3 rot = (target - p.transform.position).normalized;
        rot.y = 0;
        p.transform.rotation = Quaternion.LookRotation(rot);
        GameObject go = UnityEngine.Object.Instantiate(Lightningball_Prefab,
            p.transform.position + Vector3.up * 1.2f + GameCamera.instance.transform.forward * 0.5f,
            GameCamera.instance.transform.rotation);
        Vector3 direction = (target - go.transform.position).normalized;
        float damage = this.CalculateSkillValue();
        go.GetComponent<LightningBallComponent>().Setup(direction, damage);
        StartCooldown(cooldown);
    }


    public override bool CanExecute()
    {
        return !Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Projectile, AoE, Damage</color>";
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

        builder.AppendLine(
            $"Damage: <color=blue>Lightning {Math.Round(currentValue / 2f, 1)}</color> + <color=yellow>Blunt {Math.Round(currentValue / 2f, 1)}</color>");
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
            builder.AppendLine(
                $"Damage: <color=blue>Lightning {Math.Round(nextValue / 2f, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color></color> + <color=yellow>Blunt {Math.Round(nextValue / 2f, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color></color>");
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
    public override Color SkillColor => new Color(0.12f, 0.15f, 1f);
}