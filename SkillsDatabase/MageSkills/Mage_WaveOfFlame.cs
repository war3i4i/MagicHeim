using System.Text;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Interfaces;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Mage_WaveOfFlame : MH_Skill
{
    private static GameObject Prefab;

    public Mage_WaveOfFlame()
    {
        _definition._InternalName = "Mage_Waveofflame";
        _definition.Name = "$mh_mage_waveofflame";
        _definition.Description = "$mh_mage_waveofflame_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Damage", 50f,
            "Damage amount (Min Lvl)");

        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Damage", 100f,
            "Damage amount (Max Lvl)");

        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Manacost", 40f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Manacost", 70f,
            "Manacost amount (Max Lvl)");

        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Cooldown", 30f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Cooldown", 12f,
            "Cooldown amount (Max Lvl)");


        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 10,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn",
            50, "Required Level");


        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 2,
            "Leveling Step");

        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Mage_WaveOfFlame_Icon");
        _definition.Video = "https://kg.sayless.eu/skills/MH_Mage_WaveOfFlame.mp4";
        _definition.Animation = ClassAnimationReplace.MH_AnimationNames[ClassAnimationReplace.MH_Animation.MageSlam];
        _definition.AnimationTime = 0.8f;
        Prefab = MagicHeim.asset.LoadAsset<GameObject>("Mage_WaveOfFlame_Prefab");

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
        }
    }

    public static int Script_Layermask2 =
        LayerMask.GetMask("Default", "character", "character_noenv", "character_net", "character_ghost", "piece",
            "piece_nonsolid", "terrain", "static_solid");

    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        Player p = Player.m_localPlayer;
        float damage = this.CalculateSkillValue();
        UnityEngine.Object.Instantiate(Prefab, p.transform.position, Quaternion.identity);
        Collider[] array = Physics.OverlapSphere(p.transform.position + Vector3.up * 1f, 8.5f, Script_Layermask2,
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
                    hit.m_damage.m_blunt = damage / 2f;
                    hit.m_damage.m_fire = damage / 2f;
                    hit.m_point = collider.ClosestPoint(p.transform.position);
                    hit.m_ranged = true;
                    hit.SetAttacker(Player.m_localPlayer);
                    character.DamageMH(hit);
                    character.Stagger(Vector3.zero);
                }
            }
        }

        StartCooldown(this.CalculateSkillCooldown());
    }

    public override bool CanExecute()
    {
        return !Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Damage, Stagger, AoE</color>";
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
            $"Damage: <color=red>Fire {Math.Round(currentValue / 2f, 1)}</color> + <color=yellow>Blunt {Math.Round(currentValue / 2f, 1)}</color>");
        builder.AppendLine($"Cooldown: {Math.Round(currentCooldown, 1)}");
        builder.AppendLine($"Manacost: {Math.Round(currentManacost, 1)}");

        if (Level < maxLevel && Level > 0)
        {
            float nextValue = this.CalculateSkillValue(forLevel + 1);
            float nextCooldown = this.CalculateSkillCooldown(forLevel + 1);
            float nextManacost = this.CalculateSkillManacost(forLevel + 1);
            float cooldownDiff = nextCooldown - currentCooldown;
            float manacostDiff = nextManacost - currentManacost;
            float valueDiff = nextValue - currentValue;

            double roundedCooldownDiff = Math.Round(cooldownDiff, 1);
            double roundedManacostDiff = Math.Round(manacostDiff, 1);
            double roundedValueDiff = Math.Round(valueDiff, 1);

            builder.AppendLine("\nNext Level:");
            builder.AppendLine(
                $"Damage: <color=red>Fire {Math.Round(nextValue / 2f, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color></color> + <color=yellow>Blunt {Math.Round(nextValue / 2f, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color></color>");
            builder.AppendLine(
                $"Cooldown: {Math.Round(nextCooldown, 1)} <color=green>({(roundedCooldownDiff > 0 ? "+" : "")}{roundedCooldownDiff})</color>");
            builder.AppendLine(
                $"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }


        return builder.ToString();
    }

    public override bool CanRightClickCast => true;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => new Color(1f, 0.53f, 0.24f);
}