using System.Text;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Classes;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;
using MagicHeim.UI_s;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Mage_ArcaneSpikes : MH_Skill
{
    private static GameObject Explosion;
    private static GameObject Prefab;

    public Mage_ArcaneSpikes()
    {
        _definition._InternalName = "Mage_Arcanespikes";
        _definition.Animation = ClassAnimationReplace.MH_AnimationNames[ClassAnimationReplace.MH_Animation.MageSummon];
        _definition.Name = "$mh_mage_arcanespikes";
        _definition.Description = "$mh_mage_arcanespikes_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Damage", 20f,
            "Damage amount (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Damage", 40f,
            "Damage amount (Max Lvl)");

        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Manacost", 10f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Manacost", 25f,
            "Manacost amount (Max Lvl)");

        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Cooldown", 14f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Cooldown", 8f,
            "Cooldown amount (Max Lvl)");


        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Max Level", 10,
            "Max Skill Level");

        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Required Level To Learn",
            36, "Required Level");


        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            $"Leveling Step", 4,
            "Leveling Step");

        _definition.AnimationTime = 0.5f;
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Mage_ArcaneSpikes_Icon");
        _definition.Video = "https://kg-dev.xyz/skills/MH_Mage_ArcaneSpikes.mp4";
        Prefab = MagicHeim.asset.LoadAsset<GameObject>("Mage_ArcaneSpikes_Prefab");
        Prefab.AddComponent<ArcaneSpikesComponent>();
        Explosion = MagicHeim.asset.LoadAsset<GameObject>("Mage_ArcaneSpikes_Preload");
        Prefab.GetComponentInChildren<Collider>().gameObject.layer = 3;

        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
        this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
        this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);
    }


    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScene_Awake_Patch
    {
        static void Postfix(ZNetScene __instance)
        {
            __instance.m_namedPrefabs[Prefab.name.GetStableHashCode()] = Prefab;
            __instance.m_namedPrefabs[Explosion.name.GetStableHashCode()] = Explosion;
        }
    }


    public class ArcaneSpikesComponent : MonoBehaviour
    {
        private float _damage;
        private readonly HashSet<Character> list = new();
        private ZNetView znv;

        private void Awake()
        {
            znv = GetComponent<ZNetView>();
        }


        public void Setup(float damage)
        {
            _damage = damage;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!znv.IsOwner()) return;
            if (other.TryGetComponent<Character>(out var c))
            {
                if (!list.Contains(c))
                {
                    if (global::MagicHeim.Utils.IsEnemy(c))
                    {
                        HitData hit = new();
                        hit.m_skill = Skills.SkillType.ElementalMagic;
                        hit.m_damage.m_blunt = _damage;
                        hit.m_point = c.m_collider.ClosestPoint(transform.position);
                        hit.m_ranged = true;
                        hit.m_dir = (c.transform.position - transform.position).normalized;
                        hit.SetAttacker(Player.m_localPlayer);
                        c.DamageMH(hit);
                        c.Stagger(hit.m_dir);
                    }

                    list.Add(c);
                }
            }
        }
    }


    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        var cooldown = this.CalculateSkillCooldown();
        UnityEngine.Object.Instantiate(Explosion, Player.m_localPlayer.transform.position, Quaternion.identity);
        var dir = GameCamera.instance.transform.forward;
        var go = UnityEngine.Object.Instantiate(Prefab, Player.m_localPlayer.transform.position + dir,
            Quaternion.LookRotation(dir));
        go.GetComponent<ArcaneSpikesComponent>().Setup(this.CalculateSkillValue());
        StartCooldown(cooldown);
    }


    public override bool CanExecute()
    {
        return !Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>AoE, Damage, Stagger</color>";
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

        builder.AppendLine($"Damage: <color=yellow>Blunt  {Math.Round(currentValue, 1)}</color>");
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
                $"Damage: <color=yellow>Blunt  {Math.Round(nextValue, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color></color>");
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
    public override Color SkillColor => new Color(1f, 0.04f, 0.93f);
}