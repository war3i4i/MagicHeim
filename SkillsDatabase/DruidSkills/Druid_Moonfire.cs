using System.Text;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Classes;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;
using MagicHeim.SkillsDatabase.GlobalMechanics;
using MagicHeim.UI_s;
using Object = UnityEngine.Object;

namespace MagicHeim.SkillsDatabase.DruidSkills;

public sealed class Druid_Moonfire : MH_Skill
{
    private static GameObject _Prefab;

    public Druid_Moonfire()
    {
        _definition._InternalName = "Druid_Moonfire";
        _definition.Name = "$mh_druid_moonfire";
        _definition.Description = "$mh_druid_moonfire_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Damage", 4f,
            "Damage amount (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Damage", 30f,
            "Damage amount (Max Lvl)");

        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Manacost", 5f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Manacost", 12f,
            "Manacost amount (Max Lvl)");

        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Cooldown", 9f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Cooldown", 3f,
            "Cooldown amount (Max Lvl)");

        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Max Level", 10,
            "Max Skill Level");

        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Required Level To Learn",
            1, "Required Level");


        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            $"Leveling Step", 6,
            "Leveling Step");
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_Moonfire_Icon");
        _definition.Video = "https://kg-dev.xyz/skills/MH_Druid_Moonfire.mp4";
        _Prefab = MagicHeim.asset.LoadAsset<GameObject>("Druid_Moonfire_Prefab");
        _Prefab.AddComponent<MH_FollowTargetComponent>();
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
        }
    }


    public static int Script_Layermask = LayerMask.GetMask("character", "character_noenv", "character_net",
        "character_ghost", "piece", "piece_nonsolid", "terrain");


    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        Player p = Player.m_localPlayer;
        float cooldown = this.CalculateSkillCooldown();
        p.m_collider.enabled = false;
        bool castHit = Physics.Raycast(GameCamera.instance.transform.position, p.GetLookDir(), out var raycast, 60f, Script_Layermask);
        p.m_collider.enabled = true;
        if (castHit && raycast.collider && raycast.collider.GetComponentInParent<Character>() is {} enemy)
        {
            if (Vector3.Distance(enemy.transform.position, p.transform.position) > 50f)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                    $"<color=#00FFFF>Too</color><color=yellow> far</color>");
                p.AddEitr(this.CalculateSkillManacost());
                return;
            }
            if(!Utils.IsEnemy(enemy)) return;
            var vfx = UnityEngine.Object.Instantiate(_Prefab, enemy.transform.position, Quaternion.identity);
            vfx.GetComponent<MH_FollowTargetComponent>().Setup(enemy);
            HitData hit = new();
            hit.m_skill = Skills.SkillType.ElementalMagic;
            hit.m_damage.m_pierce = this.CalculateSkillValue();
            hit.m_point = enemy.m_collider.ClosestPoint(enemy.transform.position);
            hit.m_ranged = true;
            hit.SetAttacker(p); 
            enemy.DamageMH(hit);
            p.m_zanim.SetTrigger(ClassAnimationReplace.MH_AnimationNames[ClassAnimationReplace.MH_Animation.MageWave]);
            StartCooldown(cooldown);
        }
        else
        {
            p.AddEitr(this.CalculateSkillManacost());
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                $"<color=#00FFFF>No</color><color=yellow> target</color>");
        }
    }

    public override bool CanExecute()
    {
        return !Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Target Hover Skill, Single Target, Damage</color>";
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

        builder.AppendLine($"Damage: <color=#FF00FF>Piercing {Math.Round(currentValue, 1)}</color>");
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
            builder.AppendLine($"Damage: <color=#FF00FF>Piercing {Math.Round(nextValue, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color></color>");
            builder.AppendLine(
                $"Cooldown: {Math.Round(nextCooldown, 1)} <color=green>({(roundedCooldownDiff > 0 ? "+" : "")}{roundedCooldownDiff})</color>");
            builder.AppendLine(
                $"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }


        return builder.ToString();
    }

    public override Class PreferableClass => Class.Druid;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => new Color(1f, 0.76f, 0.21f);
}