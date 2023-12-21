using System.Text;
using JetBrains.Annotations;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Interfaces;

namespace MagicHeim.SkillsDatabase.DruidSkills;

public sealed class Druid_EikthyrPower : MH_Skill
{
    private static readonly GameObject InactiveGO = new(){name = "Eikthyr_GO_DruidEikthyrPower_Inactive", hideFlags = HideFlags.HideAndDontSave};
    private static GameObject Eikthyr_GO;
    private static GameObject Preload;
    private static GameObject Explosion;
    private static GameObject Prefab;
    

    public Druid_EikthyrPower()
    {
        InactiveGO.SetActive(false);
        _definition._InternalName = "Druid_EikthyrPower";
        _definition.Name = "$mh_druid_eikthyrpower";
        _definition.Description = "$mh_druid_eikthyrpower_desc";
        _definition.Animation = ClassAnimationReplace.MH_AnimationNames[ClassAnimationReplace.MH_Animation.MageSummon];
        _definition.AnimationTime = 0.5f;

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Damage", 20f,
            "Value amount (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Damage", 80f,
            "Value amount (Max Lvl)");

        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Manacost", 45f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Manacost", 30f,
            "Manacost amount (Max Lvl)");

        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Cooldown", 60f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Cooldown", 30f,
            "Cooldown amount (Max Lvl)");


        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 10,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn",
            15, "Required Level");


        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 6,
            "Leveling Step");

        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_EikthyrPower_Icon");
        _definition.Video = "https://kg.sayless.eu/skills/MH_Druid_EikthyrPower.mp4";
        Preload = MagicHeim.asset.LoadAsset<GameObject>("Druid_EikthyrPower_Preload");
        Explosion = MagicHeim.asset.LoadAsset<GameObject>("Druid_EikthyrPower_Explosion");
        Prefab = MagicHeim.asset.LoadAsset<GameObject>("Druid_EikthyrPower_Prefab");
        Prefab.AddComponent<MovableComponent>();
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
            if (!Eikthyr_GO)
            {
                GameObject _origGO = __instance.GetPrefab("Eikthyr").GetComponentInChildren<Animator>(true).gameObject;
                Eikthyr_GO = UnityEngine.Object.Instantiate(_origGO, InactiveGO.transform);
                Eikthyr_GO.name = "Eikthyr_GO_DruidEikthyrPower";
                Eikthyr_GO.gameObject.SetActive(true);
                Eikthyr_GO.gameObject.transform.localPosition = Vector3.zero;
                Eikthyr_GO.gameObject.transform.localRotation = Quaternion.identity;

                Material mat = MagicHeim.asset.LoadAsset<Material>("DruidMat");
                foreach (Renderer renderer in Eikthyr_GO.GetComponentsInChildren<Renderer>())
                    renderer.sharedMaterial = mat;

                TimedDestruction timed = Eikthyr_GO.AddComponent<TimedDestruction>();
                timed.m_timeout = 3f;
                timed.m_triggerOnAwake = true;
                Eikthyr_GO.AddComponent<ZNetView>();
                Eikthyr_GO.AddComponent<EikthyrPower_Local>();
                UnityEngine.Object.Destroy(Eikthyr_GO.GetComponent<CharacterAnimEvent>());
               
            }
            __instance.m_namedPrefabs[Eikthyr_GO.name.GetStableHashCode()] = Eikthyr_GO;
            __instance.m_namedPrefabs[Explosion.name.GetStableHashCode()] = Explosion;
            __instance.m_namedPrefabs[Preload.name.GetStableHashCode()] = Preload;
            __instance.m_namedPrefabs[Prefab.name.GetStableHashCode()] = Prefab;
        }
    }

    private static readonly Vector3 NON_Vector = new Vector3(-100000, 0, 0);
    
    private class EikthyrPower_Local : MonoBehaviour
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
            GetComponent<Animator>().SetTrigger(BossAnim);
            if (_znv.IsOwner()) 
            {
                Invoke(nameof(DoDamage), 1.5f);
            }
        }
        private void DoDamage()
        {
            if (_target == NON_Vector) return;
            Transform t = transform;
            Vector3 start = t.position + t.forward * 4f + Vector3.up * 2f;
            Quaternion rot = Quaternion.LookRotation((_target - start).normalized);
            Instantiate(Explosion, start, rot); 
            
            GameObject movable = Instantiate(Prefab, start, rot);
            movable.GetComponent<MovableComponent>().Setup(_damage);
        }
    }
    
    
    public class MovableComponent : MonoBehaviour
    {
        private HashSet<Character> list = new();
        private ZNetView znv;
        private float VALUE;

        private void OnTriggerEnter(Collider other)
        {
            if (!znv.IsOwner()) return;
            if (other.GetComponentInParent<Character>() is {} c)
            {
                if (!list.Add(c)) return;
                if (Utils.IsEnemy(c))
                {
                    HitData hit = new();
                    hit.m_attacker = Player.m_localPlayer.GetZDOID();
                    hit.m_point = c.m_collider.ClosestPointOnBounds(transform.position);
                    hit.m_skill = Skills.SkillType.ElementalMagic;
                    hit.m_damage.m_lightning = VALUE;
                    hit.m_ranged = true;
                    c.Stagger(Vector3.zero);
                    c.DamageMH(hit);
                }
            }
        }

        private void Awake()
        {
            znv = GetComponent<ZNetView>();
        }

        public void Setup(float val)
        {
            VALUE = val;
            StartCoroutine(Move(transform.forward));
        }

        private IEnumerator Move(Vector3 dir)
        {
            float speed = 30f;
            float count = 0;
            while (count <= 1f)
            {
                count += Time.deltaTime;
                transform.position += dir * speed * Time.deltaTime;
                yield return null;
            }
            znv.ClaimOwnership();
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
        Vector3 spawnPoint = p.transform.position + Vector3.up * 3f - GameCamera.instance.transform.forward * 2f - p.transform.right * 1.5f;
        Quaternion spawnRot = (target - spawnPoint).normalized == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation((target - spawnPoint).normalized);
        GameObject go = UnityEngine.Object.Instantiate(Eikthyr_GO, spawnPoint, spawnRot);
        UnityEngine.Object.Instantiate(Preload, go.transform.position + go.transform.forward + Vector3.up, Quaternion.identity);
        float damage = this.CalculateSkillValue();
        go.GetComponent<EikthyrPower_Local>().Setup(damage, target);
        StartCooldown(cooldown); 
    }
    
    private static readonly int BossAnim = Animator.StringToHash("attack2");
    
    public override bool CanExecute()
    {
        return !Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Projectile, Damage</color>";
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

        builder.AppendLine($"Damage: <color=blue>Lightning {Math.Round(currentValue, 1)}</color>");
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
            builder.AppendLine($"Damage: <color=blue>Lightning {Math.Round(nextValue, 1)}</color> <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
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