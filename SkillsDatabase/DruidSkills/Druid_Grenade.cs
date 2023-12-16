using System.Text;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Interfaces;

namespace MagicHeim.SkillsDatabase.DruidSkills;

public sealed class Druid_Grenade : MH_Skill
{
    private static GameObject _Prefab;
    private static GameObject Explosion;

    public Druid_Grenade()
    {
        _definition._InternalName = "Druid_Grenade";
        _definition.Animation =
            ClassAnimationReplace.MH_AnimationNames[ClassAnimationReplace.MH_Animation.Throw];
        _definition.Name = "$mh_druid_grenade";
        _definition.Description = "$mh_druid_grenade_desc";

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

        _definition.AnimationTime = 0.8f;
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_Grenade_Icon");
        _definition.Video = "https://kg.sayless.eu/skills/MH_Druid_Grenade.mp4";
        _Prefab = MagicHeim.asset.LoadAsset<GameObject>("Druid_Grenade_Prefab");
        _Prefab.AddComponent<GrenadeComponent>();
        Explosion = MagicHeim.asset.LoadAsset<GameObject>("Druid_Grenade_Explosion");

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
            __instance.m_namedPrefabs[Explosion.name.GetStableHashCode()] = Explosion;
        } 
    }

          public class GrenadeComponent : MonoBehaviour
        {
            private ZNetView znv;
            private Rigidbody rbody;
            private float aoe;
            private float count;
            private float damage;
            
            private void Awake()
            { 
                znv = GetComponent<ZNetView>();
                rbody = GetComponent<Rigidbody>();
            }

            public void Setup(Vector3 fwd, float aoe, float time, float damage)
            {
                fwd.y = Mathf.Min(1f, fwd.y + 0.4f);
                rbody.AddForce(fwd * (18f * (fwd.y + 0.75f)), ForceMode.Impulse);
                this.aoe = aoe; 
                count = time;
                this.damage = damage;
            }
 
            private void FixedUpdate()
            {
                if (!znv.IsOwner()) return;
                count -= Time.fixedDeltaTime; 
                if (count <= 0 && Player.m_localPlayer)
                {
                    Instantiate(Explosion, transform.position, Quaternion.identity);
                    List<Character> list = new List<Character>();
                    Character.GetCharactersInRange(transform.position, aoe, list);
                    foreach (Character c in list)
                    {
                        if (!Utils.IsEnemy(c)) continue;
                        HitData hit = new HitData();
                        hit.m_damage.m_fire = damage / 2f;
                        hit.m_damage.m_blunt = damage / 2f;
                        hit.SetAttacker(Player.m_localPlayer);
                        hit.m_point = c.m_collider.ClosestPointOnBounds(transform.position) + Vector3.up;
                        c.DamageMH(hit);
                    }
                    ZNetScene.instance.Destroy(this.gameObject);
                }
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
        Vector3 grenadeStart = p.transform.position + p.transform.forward * 0.5f + Vector3.up * 1.5f;
        GameObject grenade = UnityEngine.Object.Instantiate(_Prefab, grenadeStart, p.transform.rotation);
        grenade.GetComponent<GrenadeComponent>().Setup(GameCamera.instance.m_camera.transform.forward, 8f, 2f, this.CalculateSkillValue());
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

        builder.AppendLine($"Damage: <color=red>Fire {Math.Round(currentValue / 2, 1)}</color> + <color=yellow>Blunt {Math.Round(currentValue / 2, 1)}</color>");
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

            double roundedValueDiff = Math.Round(valueDiff / 2, 1);
            double roundedCooldownDiff = Math.Round(cooldownDiff, 1);
            double roundedManacostDiff = Math.Round(manacostDiff, 1);

            builder.AppendLine("\nNext Level:");
            builder.AppendLine($"Damage: <color=red>Fire {Math.Round(nextValue / 2f, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color></color> + <color=yellow>Blunt {Math.Round(nextValue / 2f, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color></color>");
            builder.AppendLine($"Cooldown: {Math.Round(nextCooldown, 1)} <color=green>({(roundedCooldownDiff > 0 ? "+" : "")}{roundedCooldownDiff})</color>");
            builder.AppendLine($"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }


        return builder.ToString();
    }

    public override bool CanRightClickCast => false;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => new Color(1f, 0.76f, 0.21f);
}