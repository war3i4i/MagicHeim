using System.Text;
using MagicHeim.MH_Interfaces;
using MagicHeim.UI_s;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Mage_ThunderWrath : MH_Skill
{
    private static GameObject Thunder_Prefab;
    private static GameObject Thunder_Explosion;
    private static GameObject Teleport_RangeShowup;

    public Mage_ThunderWrath()
    {
        _definition._InternalName = "Mage_Thunderwrath";
        _definition.Name = "$mh_mage_thunderwrath";
        _definition.Description = "$mh_mage_thunderwrath_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Damage", 35f,
            "Value amount (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Damage", 90f,
            "Value amount (Max Lvl)");

        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Manacost", 10f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Manacost", 25f,
            "Manacost amount (Max Lvl)");

        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Cooldown", 45f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Cooldown", 18f,
            "Cooldown amount (Max Lvl)");


        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 10,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn",
            64, "Required Level");


        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 1,
            "Leveling Step");

        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Mage_ThunderWrath_Icon");
        _definition.Video = "https://kg.sayless.eu/skills/MH_Mage_ThunderWrath.mp4";
        _definition.Animation = "staff_shield";
        _definition.AnimationTime = 0.5f;
        Thunder_Prefab = MagicHeim.asset.LoadAsset<GameObject>("Mage_ThunderWrath_Thunder");
        Teleport_RangeShowup = MagicHeim.asset.LoadAsset<GameObject>("Mage_AreaShowup");
        Thunder_Explosion = MagicHeim.asset.LoadAsset<GameObject>("Mage_ThunderWrath_Explosion");

        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
        this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
        this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScene_Awake_Patch
    {
        static void Postfix(ZNetScene __instance)
        {
            __instance.m_namedPrefabs[Thunder_Prefab.name.GetStableHashCode()] = Thunder_Prefab;
            __instance.m_namedPrefabs[Thunder_Explosion.name.GetStableHashCode()] = Thunder_Explosion;
        }
    }

    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        MagicHeim._thistype.StartCoroutine(Charge(Cond));
    }

    private static readonly int Script_Layermask =
        LayerMask.GetMask("Default", "piece_nonsolid", "terrain", "piece", "piece_nonsolid");

    public static readonly int Script_Layermask2 =
        LayerMask.GetMask("Default", "character", "character_noenv", "character_net", "character_ghost", "piece",
            "piece_nonsolid", "terrain", "static_solid");

    private IEnumerator Charge(Func<bool> Cond)
    {
        Player p = Player.m_localPlayer;
        int maxUsages = 5;
        GameObject rangeShowup =
            UnityEngine.Object.Instantiate(Teleport_RangeShowup, p.transform.position, Quaternion.identity);
        rangeShowup.GetComponent<CircleProjector>().m_radius = 50f;
        rangeShowup.GetComponent<CircleProjector>().Update();
        float damage = this.CalculateSkillValue();
        Vector3 initTarget;
        if (Physics.Raycast(Utils.GetPerfectEyePosition(), p.GetLookDir(), out RaycastHit testRaycast,
                40f, Script_Layermask) && testRaycast.collider)
        {
            initTarget = testRaycast.point;
        }
        else
        {
            if (rangeShowup) UnityEngine.Object.Destroy(rangeShowup);
            p.AddEitr(this.CalculateSkillManacost());
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "<color=#00FFFF>Too far</color>");
            yield break;
        }

        SkillChargeUI.ShowCharge(this, maxUsages);
        GameObject go = UnityEngine.Object.Instantiate(Thunder_Prefab, initTarget, Quaternion.identity);
        float count = 0;
        while (Cond())
        {
            rangeShowup.transform.position = p.transform.position;
            if (Physics.Raycast(Utils.GetPerfectEyePosition(), p.GetLookDir(), out RaycastHit raycast, 50f,
                    Script_Layermask2) && raycast.collider)
            {
                Vector3 target = raycast.point;
                Vector3 nPos = Vector3.MoveTowards(go.transform.position, target, 5f * Time.deltaTime);
                ZoneSystem.instance.FindFloor(nPos + Vector3.up * 5f, out nPos.y);
                go.transform.position = nPos;
            }

            count += Time.deltaTime;
            if (count >= 1)
            {
                if (maxUsages < 5 && !TryUseCost())
                {
                    break;
                }

                UnityEngine.Object.Instantiate(Thunder_Explosion, go.transform.position, Quaternion.identity);
                count = 0;
                List<Character> list = new List<Character>();
                Character.GetCharactersInRange(go.transform.position, 10f, list);
                if (list.Count > 0)
                {
                    foreach (Character c in list)
                    {
                        if (!Utils.IsEnemy(c) && c != Player.m_localPlayer)
                        {
                            continue;
                        }

                        HitData hit = new HitData();
                        hit.m_damage.m_lightning = damage;
                        hit.m_skill = Skills.SkillType.ElementalMagic;
                        if (c != Player.m_localPlayer) hit.SetAttacker(p);
                        hit.m_point = c.m_collider.ClosestPointOnBounds(go.transform.position) + Vector3.up;
                        hit.m_ranged = true;
                        c.DamageMH(hit);
                    }
                }

                maxUsages--;
                if (maxUsages <= 0)
                {
                    break;
                }
            }

            yield return null;
        }

        if (rangeShowup) UnityEngine.Object.Destroy(rangeShowup);
        SkillChargeUI.RemoveCharge(this);
        StartCooldown(this.CalculateSkillCooldown());
        yield return new WaitForEndOfFrame();
        if (go) ZNetScene.instance.Destroy(go);
    }


    public override bool CanExecute()
    {
        return !Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Chargable, Damage, AoE, Can hit owner</color>";
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
            builder.AppendLine(
                $"Damage: <color=blue>Lightning {Math.Round(nextValue, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color></color>");
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
    public override Color SkillColor => new Color(0.05f, 0.08f, 1f);
}