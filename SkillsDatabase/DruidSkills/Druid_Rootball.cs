using System.Text;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Classes;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;
using MagicHeim.UI_s;

namespace MagicHeim.SkillsDatabase.DruidSkills;

public sealed class Druid_Rootball : MH_Skill
{
    private static GameObject _Prefab;
    private static GameObject _PrefabDebuff;
    private static GameObject _PrefabExplosion;

    public Druid_Rootball()
    {
        _definition._InternalName = "Druid_Rootball";
        _definition.Animation =
            ClassAnimationReplace.MH_AnimationNames[ClassAnimationReplace.MH_Animation.TwoHandedProjectile];
        _definition.Name = "$mh_druid_rootball";
        _definition.Description = "$mh_druid_rootball_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Damage", 8f,
            "Damage amount (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Damage", 60f,
            "Damage amount (Max Lvl)");

        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Manacost", 15f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Manacost", 25f,
            "Manacost amount (Max Lvl)");

        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Cooldown", 12f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Cooldown", 6f,
            "Cooldown amount (Max Lvl)");

        _definition.MinLvlDuration = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Duration", 2f,
            "Duration amount (Min Lvl)");
        
        _definition.MaxLvlDuration = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Duration", 4f,
            "Duration amount (Max Lvl)");

        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Max Level", 10,
            "Max Skill Level");

        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Required Level To Learn",
            1, "Required Level");


        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            $"Leveling Step", 6,
            "Leveling Step");

        _definition.AnimationTime = 0.8f;
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_Rootball_Icon");
        _definition.Video = "https://kg-dev.xyz/skills/MH_Druid_Rootball.mp4";
        _Prefab = MagicHeim.asset.LoadAsset<GameObject>("Druid_Rootball_Prefab");
        _PrefabDebuff = MagicHeim.asset.LoadAsset<GameObject>("Druid_Rootball_Debuff");
        _PrefabExplosion = MagicHeim.asset.LoadAsset<GameObject>("Druid_Rootball_Explosion");
        _Prefab.AddComponent<RootballComponent>();

        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
        this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
        this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);

        CachedSprite = _definition.Icon;
    }

    private static Sprite CachedSprite;

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScene_Awake_Patch
    {
        static void Postfix(ZNetScene __instance)
        {
            __instance.m_namedPrefabs[_Prefab.name.GetStableHashCode()] = _Prefab;
            __instance.m_namedPrefabs[_PrefabDebuff.name.GetStableHashCode()] = _PrefabDebuff;
            __instance.m_namedPrefabs[_PrefabExplosion.name.GetStableHashCode()] = _PrefabExplosion;
        }
    }


    private static readonly LayerMask mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece",
        "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "hitbox", "character_noenv",
        "vehicle");


    public class RootballComponent : MonoBehaviour
    {
        static readonly int m_rayMaskSolids = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece",
            "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "hitbox", "character_noenv",
            "vehicle");


        public void Setup(Vector3 dir, float damage, float duration)
        {
            StartCoroutine(Move(dir, damage, duration));
        } 

        private void Explosion(float damage, float duration)
        {
            Instantiate(_PrefabExplosion, transform.position, Quaternion.identity);
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
                        hit.m_statusEffect = "Druid_Rootball_Debuff";
                        hit.m_skillLevel = duration;
                        hit.m_skill = Skills.SkillType.ElementalMagic;
                        hit.m_damage.m_blunt = damage / 2f;
                        hit.m_damage.m_poison = damage / 2f;
                        hit.m_point = collider.ClosestPoint(transform.position);
                        hit.m_ranged = true;
                        hit.SetAttacker(Player.m_localPlayer);
                        component.DamageMH(hit);
                    }
                }
            }
        }

        private IEnumerator Move(Vector3 dir, float damage, float duration)
        {
            bool didhit = false;
            float speed = 20f;
            float count = 0;
            while (count <= 2f)
            {
                count += Time.deltaTime;

                var oldPos = transform.position;
                transform.position += dir * speed * Time.deltaTime;
                var newPos = transform.position;
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
                        Explosion(damage, duration);
                        didhit = true;
                        count = 100f;
                        break;
                    }
                }

                yield return null; 
            }

            if (!didhit) Explosion(damage, duration);
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
        var go = UnityEngine.Object.Instantiate(_Prefab,
            p.transform.position + Vector3.up * 1.2f + GameCamera.instance.transform.forward * 0.5f,
            GameCamera.instance.transform.rotation);
        var direction = (target - go.transform.position).normalized;
        float damage = this.CalculateSkillValue();
        go.GetComponent<RootballComponent>().Setup(direction, damage, this.CalculateSkillDuration());
        StartCooldown(cooldown);
    }

    public class SE_Druid_Rootball_Debuff : StatusEffect
    {
        public SE_Druid_Rootball_Debuff()
        {
            name = "Druid_Rootball_Debuff";
            m_tooltip = "Rooted";
            m_icon = CachedSprite;
            m_name = "Rooted";
            m_ttl = 1;
            m_startEffects = new EffectList
            {
                m_effectPrefabs = new[]
                {
                    new EffectList.EffectData()
                    {
                        m_attach = true, m_enabled = true, m_inheritParentRotation = true,
                        m_inheritParentScale = true,
                        m_prefab = _PrefabDebuff, m_randomRotation = false, m_scale = true
                    }
                }
            };
        }

        public override void SetLevel(int itemLevel, float skillLevel)
        {
            m_ttl = skillLevel;
        }

        public override void ModifySpeed(float baseSpeed, ref float speed)
        {
            speed = 0;
        }
    }
    
    public static class Druid_Rootball_Debuff_Patches
    {
        private static void Add_SE(ObjectDB odb)
        {
            if (ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0 ||
                ObjectDB.instance.GetItemPrefab("Amber") == null) return;

            if (!odb.m_StatusEffects.Find(se => se.name == "Druid_Rootball_Debuff"))
                odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Druid_Rootball_Debuff>());
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
        return "<color=red>Projectile, AoE, Damage</color>";
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

        builder.AppendLine($"Damage: <color=magenta>Piercing {Math.Round(currentValue, 1)}</color>");
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
            builder.AppendLine($"Damage: <color=magenta>Piercing {Math.Round(nextValue, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color></color>");
            builder.AppendLine(
                $"Cooldown: {Math.Round(nextCooldown, 1)} <color=green>({(roundedCooldownDiff > 0 ? "+" : "")}{roundedCooldownDiff})</color>");
            builder.AppendLine(
                $"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }


        return builder.ToString();
    }

    public override Class PreferableClass => Class.Druid;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => new Color(1f, 0.76f, 0.21f);
}