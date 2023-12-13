using System.Text;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Interfaces;
using MagicHeim.UI_s;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Mage_IceWall : MH_Skill
{
    private static GameObject Prefab;
    private static GameObject RangeShowup;
    private static GameObject TargetPoint;

    public Mage_IceWall()
    {
        _definition._InternalName = "Mage_Icewall";
        _definition.Name = "$mh_mage_icewall";
        _definition.Description = "$mh_mage_icewall_desc";  

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Distance", 10f,
            "Value amount (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Distance", 40f,
            "Value amount (Max Lvl)");

        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Manacost", 40f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Manacost", 80f,
            "Manacost amount (Max Lvl)");

        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Cooldown", 30f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Cooldown", 18f,
            "Cooldown amount (Max Lvl)");


        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Max Level", 10,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Required Level To Learn",
            46, "Required Level");

        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            $"Leveling Step", 3,
            "Leveling Step");


        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Mage_IceWall_Icon");
        _definition.Video = "https://kg.sayless.eu/skills/MH_Mage_IceWall.mp4";
        Prefab = MagicHeim.asset.LoadAsset<GameObject>("Mage_IceWall_Prefab");
        RangeShowup = MagicHeim.asset.LoadAsset<GameObject>("Mage_IceWall_AreaShowup");
        TargetPoint = MagicHeim.asset.LoadAsset<GameObject>("Mage_IceWall_TargetShowup");

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


    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        MagicHeim._thistype.StartCoroutine(Charge(Cond));
    }

    private static readonly int JumpMask =
        LayerMask.GetMask("terrain", "Default", "piece", "piece_nonsolid", "static_solid");

    private static readonly Vector3 NON_Vector = new Vector3(-100000, 0, 0);


    private IEnumerator Charge(Func<bool> Cond)
    {
        bool cancel = false;
        SkillChargeUI.ShowCharge(this);
        Player p = Player.m_localPlayer;
        float maxDistance = this.CalculateSkillValue();
        GameObject rangeShowup =
            UnityEngine.Object.Instantiate(RangeShowup, p.transform.position, Quaternion.identity);
        GameObject targetPoint =
            UnityEngine.Object.Instantiate(TargetPoint, p.transform.position, Quaternion.identity);
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
            bool castHit = Physics.Raycast(Utils.GetPerfectEyePosition(), p.GetLookDir(), out var raycast,
                _definition.MaxLvlValue.Value + 10f,
                JumpMask);
            if (castHit && raycast.collider)
            {
                targetPoint.SetActive(true);
                target = raycast.point;
                targetPoint.transform.position = target;

                Quaternion setRotation = Quaternion.LookRotation(p.GetLookDir());

                targetPoint.transform.rotation = setRotation;

                //rotate balls
                foreach (Transform child in targetPoint.transform.Find("Scaler"))
                {
                    if (child.name != "Ball") continue;
                    Vector3 ballPos = child.position;
                    ZoneSystem.instance.FindFloor(new Vector3(ballPos.x, target.y, ballPos.z) + Vector3.up * 3f,
                        out var height);
                    child.position = new Vector3(ballPos.x, height, ballPos.z);
                }
            }
            else
            {
                targetPoint.SetActive(false);
                target = NON_Vector;
            }

            yield return null;
        }

        SkillChargeUI.RemoveCharge(this);
        if (targetPoint && !cancel && p && !p.IsDead() && target != NON_Vector &&
            global::Utils.DistanceXZ(target, p.transform.position) <= maxDistance)
        {
            Vector3 rot = (target - p.transform.position).normalized;
            rot.y = 0;
            p.transform.rotation = Quaternion.LookRotation(rot);
            StartCooldown(this.CalculateSkillCooldown());
            p.m_zanim.SetTrigger(ClassAnimationReplace.MH_AnimationNames[ClassAnimationReplace.MH_Animation.MageSummon]);
            foreach (Transform child in targetPoint.transform.Find("Scaler"))
            {
                if (child.name != "Ball") continue;
                Vector3 ballPos = child.position;
                if (ballPos.y < 30) continue;
                var wallRot = p.transform.rotation;
                wallRot.y += UnityEngine.Random.Range(-0.5f, 0.5f);
                UnityEngine.Object.Instantiate(Prefab, ballPos, wallRot);
            }
        }
        else
        {
            if (!cancel)
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "<color=#00FFFF>Too far</color>");
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
        return "<color=red>Precast, Obstacle Creation</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine($"\n");

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

            var roundedValueDiff = Math.Round(valueDiff, 1);
            var roundedCooldownDiff = Math.Round(cooldownDiff, 1);
            var roundedManacostDiff = Math.Round(manacostDiff, 1);

            builder.AppendLine($"\nNext Level:");
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