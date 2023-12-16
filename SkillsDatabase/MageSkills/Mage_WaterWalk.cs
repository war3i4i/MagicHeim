using System.Text;
using MagicHeim.MH_Interfaces;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Mage_WaterWalk : MH_Skill
{
    private static GameObject WaterWalk_Prefab;
    private static bool StaticBool_InWater;

    public Mage_WaterWalk()
    {
        _definition._InternalName = "Mage_Waterwalk";
        _definition.Name = "$mh_mage_waterwalk";
        _definition.Description = "$mh_mage_waterwalk_desc";

        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Manacost", 10f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Manacost", 2f,
            "Manacost amount (Max Lvl)");

        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 10,
            "Max Skill Level");

        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn",
            40, "Required Level");


        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 3,
            "Leveling Step");

        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Mage_WaterWalk_Icon");
        _definition.Video = "https://kg.sayless.eu/skills/MH_Mage_WaterWalk.mp4";
        WaterWalk_Prefab = MagicHeim.asset.LoadAsset<GameObject>("Mage_WaterWalk_Prefab");

        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
        this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
        this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScene_Awake_Patch
    {
        static void Postfix(ZNetScene __instance)
        {
            __instance.m_namedPrefabs[WaterWalk_Prefab.name.GetStableHashCode()] = WaterWalk_Prefab;
        }
    }

    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        Player p = Player.m_localPlayer;
        if (!Toggled)
        {
            MagicHeim._thistype.StartCoroutine(WWalk());
        }
        else
        {
            Toggled = false;
        }
    }

    private static GameObject vfx;
    private static bool IsInWaterWalk;

    private IEnumerator WWalk()
    {
        float manacost = this.CalculateSkillManacost();
        Toggled = true;
        StaticBool_InWater = Toggled;
        Player p = Player.m_localPlayer;
        if (vfx) ZNetScene.instance.Destroy(vfx.gameObject);
        vfx = UnityEngine.Object.Instantiate(WaterWalk_Prefab, p.transform.position, Quaternion.identity);
        vfx.transform.SetParent(p.transform);
        for (;;)
        {
            float useMana = manacost * Time.deltaTime;
            if (!Toggled || p.IsDead() || !p.HaveEitr(useMana))
            {
                PossibleSkillFixes.IsFlying_Inject = false;
                Toggled = false;
                StaticBool_InWater = Toggled;
                if (vfx) ZNetScene.instance.Destroy(vfx.gameObject);
                yield break;
            }

            p.UseEitr(useMana);
            yield return null;
        }
    }

    [HarmonyPatch(typeof(Character), "UpdateWater")]
    public static class Swim_Patch
    {
        private static void Postfix(Character __instance, float ___m_waterLevel, Vector3 ___m_moveDir)
        {
            if (StaticBool_InWater && __instance.IsPlayer() && Player.m_localPlayer == __instance &&
                !__instance.IsDead())
            {
                Player p = Player.m_localPlayer;
                float x, z;
                if (___m_waterLevel + 0.08f > __instance.transform.position.y)
                {
                    IsInWaterWalk = true;
                    PossibleSkillFixes.IsFlying_Inject = true;
                    x = p.transform.transform.position.x;
                    z = p.transform.transform.position.z;
                    p.transform.transform.position = new Vector3(x, ___m_waterLevel, z);
                    if (___m_moveDir.magnitude > 0.1f)
                        p.transform.rotation = Quaternion.LookRotation(p.GetMoveDir());
                    if (ZInput.GetButton("Jump") && IsInWaterWalk)
                    {
                        IsInWaterWalk = false;
                        PossibleSkillFixes.IsFlying_Inject = false;
                        p.transform.transform.position = new Vector3(x, ___m_waterLevel + 0.2f, z);
                        Player.m_localPlayer.m_lastGroundTouch = 0f;
                        Player.m_localPlayer.Jump();
                    }
                }
                else
                {
                    IsInWaterWalk = false;
                    PossibleSkillFixes.IsFlying_Inject = false;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Character), "UpdateFlying")]
    public static class UpdateFlying_Patch
    {
        private static void Postfix(Character __instance)
        {
            if (IsInWaterWalk && __instance == Player.m_localPlayer)
            {
                Player.m_localPlayer.m_fallTimer = -10;
                Player.m_localPlayer.m_zanim.SetBool(Character.s_onGround, true);
                Player.m_localPlayer.m_zanim.SetBool("falling", false);
            }
        }
    }


    public override bool CanExecute()
    {
        return !Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Walk On Water, Toggle</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine("\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentManacost = this.CalculateSkillManacost(forLevel);
        builder.AppendLine($"Manacost (Per Second): {Math.Round(currentManacost, 1)}");

        if (Level < maxLevel && Level > 0)
        {
            float nextManacost = this.CalculateSkillManacost(forLevel + 1);
            float manacostDiff = nextManacost - currentManacost;
            double roundedManacostDiff = Math.Round(manacostDiff, 1);

            builder.AppendLine("\nNext Level:");
            builder.AppendLine(
                $"Manacost (Per Second): {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }


        return builder.ToString();
    }

    public override bool CanRightClickCast => true;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => new Color(0.44f, 1f, 0.92f);
}