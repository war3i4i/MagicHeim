using System.Text;
using MagicHeim.MH_Interfaces;
using MagicHeim.UI_s;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Mage_Teleport : MH_Skill
{
    private static GameObject Teleport_Prefab;
    public static GameObject Teleport_Explosion;
    private static GameObject Teleport_RangeShowup;
    private static GameObject Teleport_TargetPoint;

    public Mage_Teleport()
    {
        _definition._InternalName = "Mage_Teleport";
        _definition.Name = "$mh_mage_teleport";
        _definition.Description = "$mh_mage_teleport_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Distance", 15f,
            "Value amount (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Distance", 50f,
            "Value amount (Max Lvl)");

        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Manacost", 25f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Manacost", 45f,
            "Manacost amount (Max Lvl)");
 
        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Cooldown", 18f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Cooldown", 8f,
            "Cooldown amount (Max Lvl)");


        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 10,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn",
            14, "Required Level");


        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 6,
            "Leveling Step");

        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Mage_Teleport_Icon");
        _definition.Video = "https://kg.sayless.eu/skills/MH_Mage_Teleport.mp4";
        Teleport_Prefab = MagicHeim.asset.LoadAsset<GameObject>("Mage_Teleport");
        Teleport_Explosion = MagicHeim.asset.LoadAsset<GameObject>("Mage_Teleport_Explosion");
        Teleport_RangeShowup = MagicHeim.asset.LoadAsset<GameObject>("Mage_AreaShowup");
        Teleport_TargetPoint = MagicHeim.asset.LoadAsset<GameObject>("Mage_TargetShowup");

        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
        this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
        this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScene_Awake_Patch
    {
        static void Postfix(ZNetScene __instance)
        {
            __instance.m_namedPrefabs[Teleport_Prefab.name.GetStableHashCode()] = Teleport_Prefab;
            __instance.m_namedPrefabs[Teleport_Explosion.name.GetStableHashCode()] = Teleport_Explosion;
        }
    }


    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        MagicHeim._thistype.StartCoroutine(Charge(Cond));
    }

    private static readonly int JumpMask =
        LayerMask.GetMask("terrain", "Default", "piece", "piece_nonsolid", "static_solid");

    private static readonly Vector3 NON_Vector = new Vector3(-100000, 0, 0);

    private IEnumerator MageMovement(Vector3 targetPos)
    {
        Player p = Player.m_localPlayer;
        Vector3 startPos = p.transform.position;
        UnityEngine.Object.Instantiate(Teleport_Prefab, startPos, Quaternion.identity);
        p.m_nview.InvokeRPC(ZNetView.Everybody, "MH_HideCharacter", true);
        p.m_nview.m_zdo.Set("MH_HideCharacter", true);
        p.m_zanim.SetTrigger("emote_stop");
        p.m_collider.isTrigger = true;
        float distance = global::Utils.DistanceXZ(startPos, targetPos);
        float time = 0.5f;
        float count = 0;
        Player.m_localPlayer.transform.rotation = Quaternion.LookRotation((targetPos - startPos).normalized);
        while (count < 1f)
        {
            if (!p || p.IsDead())
            {
                yield break;
            }

            p.m_body.velocity = Vector3.zero;
            p.m_body.angularVelocity = Vector3.zero;
            p.m_lastGroundTouch = 0;
            p.m_maxAirAltitude = 0f;
            count += Time.deltaTime / time;
            Vector3 point = startPos + (targetPos - startPos) / 2 + Vector3.up;
            Vector3 m1 = Vector3.Lerp(startPos, point, count);
            Vector3 m2 = Vector3.Lerp(point, targetPos, count);
            p.m_body.position = Vector3.Lerp(m1, m2, count);
            yield return null;
        }

        p.m_collider.isTrigger = false;
        p.m_nview.m_zdo.Set("MH_HideCharacter", false);
        p.m_body.velocity = Vector3.zero;
        p.m_body.useGravity = true;
        p.m_lastGroundTouch = 0f;
        p.m_maxAirAltitude = 0f;
        p.m_nview.InvokeRPC(ZNetView.Everybody, "MH_HideCharacter", false);
        UnityEngine.Object.Instantiate(Teleport_Explosion, p.transform.position, Quaternion.identity);
    }

    private IEnumerator Charge(Func<bool> Cond)
    {
        bool cancel = false;
        SkillChargeUI.ShowCharge(this);
        Player p = Player.m_localPlayer;
        float maxDistance = this.CalculateSkillValue();
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
            bool castHit = Physics.Raycast(Utils.GetPerfectEyePosition(), p.GetLookDir(), out RaycastHit raycast,
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
            SkillChargeUI.ShowCharge(this, 0.5f);
            p.StartCoroutine(MageMovement(target));
        }
        else
        {
            if (!cancel)
            {
                p.AddEitr(this.CalculateSkillManacost());
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "<color=#00FFFF>Too far</color>");
            }
        }

        UnityEngine.Object.Destroy(rangeShowup);
        UnityEngine.Object.Destroy(targetPoint);
    }

 
    public override bool CanExecute()
    {
        return !Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Precast, Move Position</color>";
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

        builder.AppendLine($"Distance: {Math.Round(currentValue, 1)}");
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
                $"Distance: {Math.Round(nextValue, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
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
    public override Color SkillColor => new Color(0.18f, 0.72f, 1f);
}