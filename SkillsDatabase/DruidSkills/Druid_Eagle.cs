using System.Text;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Classes;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;
using MagicHeim.UI_s;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Druid_Eagle : MH_Skill
{
    public static int CachedKey;
    private static GameObject Eagle_Prefab;
    private static GameObject Eagle_Explosion;

    public Druid_Eagle()
    {
        _definition._InternalName = "Druid_Eagle";
        _definition.Name = "$mh_druid_eagle";
        _definition.Description = "$mh_druid_eagle_desc";
        CachedKey = _definition.Key;
        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Manacost", 10f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Manacost", 2f,
            "Manacost amount (Max Lvl)");

        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Cooldown", 12f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Cooldown", 3f,
            "Cooldown amount (Max Lvl)");

        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Max Level", 5,
            "Max Skill Level");

        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Required Level To Learn",
            75, "Required Level");


        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            $"Leveling Step", 1,
            "Leveling Step");

        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_Eagle_Icon");
        _definition.Video = "https://kg-dev.xyz/skills/Mage_EnergyBlast.mp4";
        Eagle_Prefab = MagicHeim.asset.LoadAsset<GameObject>("Druid_Eagle_Prefab");
        Eagle_Explosion = MagicHeim.asset.LoadAsset<GameObject>("Druid_Eagle_Explosion");
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScene_Awake_Patch
    {
        static void Postfix(ZNetScene __instance)
        {
            __instance.m_namedPrefabs[Eagle_Prefab.name.GetStableHashCode()] = Eagle_Prefab;
            __instance.m_namedPrefabs[Eagle_Explosion.name.GetStableHashCode()] = Eagle_Explosion;
        }
    }

    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        Player p = Player.m_localPlayer;
        var manacost = this.CalculateSkillManacost();
        if (!Toggled)
        {
            if (p.HaveEitr(manacost * 3f))
            {
                MagicHeim._thistype.StartCoroutine(EagleForm());
            }
        }
        else
        {
            Toggled = false;
        }
    }


    private IEnumerator EagleForm()
    {
        var manacost = this.CalculateSkillManacost();
        Toggled = true;
        Player p = Player.m_localPlayer;
        p.m_nview.InvokeRPC(ZNetView.Everybody, "MH_HideCharacter", true);
        p.m_nview.m_zdo.Set("MH_HideCharacter", true);
        p.m_zanim.SetTrigger("emote_stop");
        p.m_collider.isTrigger = true;
        UnityEngine.Object.Instantiate(Eagle_Explosion, p.transform.position + Vector3.up, Quaternion.identity);
        GameObject go;
        go = UnityEngine.Object.Instantiate(Eagle_Prefab, p.transform.position + Vector3.up,
            Quaternion.LookRotation(GameCamera.instance.transform.forward));
        var rbody = go.GetComponent<Rigidbody>();
        go.transform.position = p.transform.position + Vector3.up;
        for (;;)
        {
            rbody.angularVelocity = Vector3.zero;
            rbody.velocity = Vector3.zero;
            go.transform.rotation = Quaternion.LookRotation(GameCamera.instance.transform.forward);
            var pos = go.transform.position;
            var fwd = GameCamera.instance.transform.forward;
            float mod = ZInput.GetButton("Run") || ZInput.GetButton("JoyRun") ? 12f : 8f;
            if (ZInput.GetButton("Forward") || ZInput.GetJoyLeftStickY() < 0)
                pos += fwd * (mod * Time.deltaTime);
            if (ZInput.GetButton("Backward") || ZInput.GetJoyLeftStickY() > 0)
                pos -= fwd * (mod * Time.deltaTime);

            if (ZInput.GetButton("Crouch"))
            {
                go.transform.rotation = Quaternion.LookRotation(-go.transform.up + go.transform.forward * 2f);
                pos.y -= mod * Time.deltaTime;
            }

            if (ZInput.GetButton("Jump"))
            {
                go.transform.rotation = Quaternion.LookRotation(go.transform.up + go.transform.forward * 2f);
                pos.y += mod * Time.deltaTime;
            }

            pos.y = pos.y < ZoneSystem.instance.GetGroundHeight(pos) + 1
                ? ZoneSystem.instance.GetGroundHeight(pos) + 1
                : pos.y;
            if (pos.y < 31) pos.y = 31;
            go.transform.position = pos;
            p.transform.position = pos;
            p.transform.rotation = go.transform.rotation;
            p.m_maxAirAltitude = 0f;
            p.m_lastGroundTouch = 0f;
            var useMana = manacost * Time.deltaTime;
            if (!p.HaveEitr(useMana) || !Toggled || p.IsDead())
            {
                Toggled = false;
                p.m_collider.isTrigger = false;
                p.m_nview.m_zdo.Set("MH_HideCharacter", false);
                p.m_body.velocity = Vector3.zero;
                p.m_body.useGravity = true;
                p.m_lastGroundTouch = 0f;
                p.m_maxAirAltitude = 0f;
                p.m_nview.InvokeRPC(ZNetView.Everybody, "MH_HideCharacter", false);
                UnityEngine.Object.Instantiate(Eagle_Explosion, p.transform.position, Quaternion.identity);
                if (go) ZNetScene.instance.Destroy(go.gameObject);
                StartCooldown(this.CalculateSkillCooldown());
                yield break;
            }

            p.UseEitr(useMana);
            yield return null;
        }
    }


    public override bool CanExecute()
    {
        return !Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Transform, Fly, Toggle</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine($"\n");

        int maxLevel = this.MaxLevel;
        int forLevel = this.Level > 0 ? this.Level : 1;
        float currentManacost = this.CalculateSkillManacost(forLevel);
        builder.AppendLine($"Manacost: {Math.Round(currentManacost, 1)}");

        if (this.Level < maxLevel && this.Level > 0)
        {
            float nextManacost = this.CalculateSkillManacost(forLevel + 1);
            float manacostDiff = nextManacost - currentManacost;
            var roundedManacostDiff = Math.Round(manacostDiff, 1);

            builder.AppendLine($"\nNext Level:");
            builder.AppendLine(
                $"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }


        return builder.ToString();
    }

    public override Class PreferableClass => Class.Druid;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => Color.green;
}