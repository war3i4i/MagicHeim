using System.Text;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Interfaces;
using Object = UnityEngine.Object;

namespace MagicHeim.SkillsDatabase.DruidSkills;

public sealed class Druid_SpiritWave : MH_Skill
{
    private static GameObject _Prefab;
    private static GameObject _Prefab_Explosion;

    public Druid_SpiritWave()
    {
        _definition._InternalName = "Druid_Spiritwave";
        _definition.Name = "$mh_druid_spiritwave";
        _definition.Description = "$mh_druid_spiritwave_desc";
        _definition.Animation =
            ClassAnimationReplace.MH_AnimationNames[
                ClassAnimationReplace.MH_Animation.TwoHandedProjectile];
        _definition.AnimationTime = 0.8f;
        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Damage", 1f,
            "Damage amount (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Damage", 10f,
            "Damage amount (Max Lvl)");

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
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_SpiritWave_Icon");
        _definition.Video = "https://kg.sayless.eu/skills/MH_Druid_SpiritWave.mp4";
        _Prefab = MagicHeim.asset.LoadAsset<GameObject>("Druid_Spiritwave_Prefab");
        _Prefab.layer = 3;
        _Prefab_Explosion = MagicHeim.asset.LoadAsset<GameObject>("Druid_SpiritWave_Explosion");
        _Prefab.AddComponent<SpiritWaveComponent>();
        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
        this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
        this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);
    }
 

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScene_Awake_Patch
    {
        static void Postfix(ZNetScene __instance)
        {
            __instance.m_namedPrefabs[_Prefab.name.GetStableHashCode()] = _Prefab;
            __instance.m_namedPrefabs[_Prefab_Explosion.name.GetStableHashCode()] = _Prefab_Explosion;
        }
    }

    public class SpiritWaveComponent : MonoBehaviour
    {
        private HashSet<Character> list = new();
        private ZNetView znv;
        private float VALUE;

        private void OnTriggerEnter(Collider other)
        {
            if (!znv.IsOwner()) return;
            if (other.TryGetComponent<Character>(out Character c))
            {
                if (!list.Contains(c))
                {
                    if (!Utils.IsEnemy(c))
                    {
                        c.Heal(VALUE / 2f);
                    }
                    else
                    {
                        HitData hit = new();
                        hit.m_attacker = Player.m_localPlayer.GetZDOID();
                        hit.m_point = c.m_collider.ClosestPointOnBounds(transform.position);
                        hit.m_skill = Skills.SkillType.ElementalMagic;
                        hit.m_damage.m_pierce = VALUE / 2f;
                        hit.m_damage.m_blunt = VALUE / 2f;
                        hit.m_ranged = true;
                        c.DamageMH(hit);
                    }
                    Instantiate(_Prefab_Explosion, c.transform.position, Quaternion.identity);
                    list.Add(c);
                }
            }
        }

        private void Awake()
        {
            znv = GetComponent<ZNetView>();
        }

        public void Setup(Vector3 dir, float val)
        {
            VALUE = val;
            StartCoroutine(Move(dir));
        }

        private IEnumerator Move(Vector3 dir)
        {
            float speed = 15f;
            float count = 0;
            while (count <= 2f)
            {
                count += Time.deltaTime;
                transform.position += dir * speed * Time.deltaTime;
                yield return null;
            }

            ZNetScene.instance.Destroy(gameObject);
        }
    }
    
    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        Player p = Player.m_localPlayer;
        Vector3 target = Utils.GetPerfectEyePosition() + p.GetLookDir() * 100f;
        Vector3 rot = (target - p.transform.position).normalized;
        rot.y = 0;
        p.transform.rotation = Quaternion.LookRotation(rot);
        GameObject proj = Object.Instantiate(_Prefab, p.transform.position + Vector3.up + p.transform.forward * 0.3f, GameCamera.instance.transform.rotation);
        Vector3 rotTo = (target - proj.transform.position).normalized;
        proj.GetComponent<SpiritWaveComponent>().Setup(rotTo, this.CalculateSkillValue());
        StartCooldown(this.CalculateSkillCooldown());
    }

    public override bool CanExecute()
    {
        return !Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Projectile, AoE, Heals Allies, Damages Enemies</color>";
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

        builder.AppendLine($"Damage: <color=#FF00FF>Piercing {Math.Round(currentValue / 2f, 1)}</color> + <color=yellow>Blunt {Math.Round(currentValue / 2f, 1)}</color>");
        builder.AppendLine($"Healing: <color=#00FF00>{Math.Round(currentValue / 2f, 1)}</color>");
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
            builder.AppendLine($"Damage: <color=#FF00FF>Piercing {Math.Round(nextValue / 2f, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color></color> + <color=yellow>Blunt {Math.Round(nextValue / 2f, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color></color>");
            builder.AppendLine($"Healing: <color=#00FF00>{Math.Round(nextValue / 2f, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color></color>");
            builder.AppendLine($"Cooldown: {Math.Round(nextCooldown, 1)} <color=green>({(roundedCooldownDiff > 0 ? "+" : "")}{roundedCooldownDiff})</color>");
            builder.AppendLine($"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }


        return builder.ToString();
    }
    
    public override bool CanRightClickCast => false;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => new Color(0.96f, 1f, 0.79f);
}