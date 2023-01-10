using System.Text;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Classes;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;
using MagicHeim.UI_s;
using Logger = MagicHeim_Logger.Logger;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Druid_Heal : MH_Skill
{
    private static GameObject Prefab;
    private static GameObject Explosion;

    public Druid_Heal()
    {
        _definition._InternalName = "Druid_Heal";
        _definition.Name = "$mh_druid_heal";
        _definition.Description = "$mh_druid_heal_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Heal", 15f,
            "Heal amount (Min Lvl)");

        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Heal", 60f,
            "Heal amount (Max Lvl)");

        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Manacost", 20f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Manacost", 55f,
            "Manacost amount (Max Lvl)");

        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Cooldown", 60f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Cooldown", 20f,
            "Cooldown amount (Max Lvl)");


        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Max Level", 10,
            "Max Skill Level");


        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Required Level To Learn",
            8, "Required Level");

        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            $"Leveling Step", 4,
            "Leveling Step");

        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_Heal_Icon");
        _definition.Video = "https://kg-dev.xyz/skills/MH_Druid_Heal.mp4";
        _definition.Animation = ClassAnimationReplace.MH_AnimationNames[ClassAnimationReplace.MH_Animation.MageWave];
        _definition.AnimationTime = 0.6f;
        Prefab = MagicHeim.asset.LoadAsset<GameObject>("Druid_Heal_Projectle");
        Explosion = MagicHeim.asset.LoadAsset<GameObject>("Druid_Heal_Impact");

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


    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        Player p = Player.m_localPlayer;
        var heal = this.CalculateSkillValue();
        var data = new Dictionary<GameObject, KeyValuePair<Character, Vector3>>();
        var list = Player.GetAllCharacters().Where(d =>
                Vector3.Distance(p.transform.position, d.transform.position) <= 20f &&
                !global::MagicHeim.Utils.IsEnemy(d))
            .ToList();
        foreach (var character in list)
        {
            if(character.IsPlayer() && !Utils.IsPlayerInGroup((Player)character)) continue;
            var go = UnityEngine.Object.Instantiate(Prefab, p.transform.position + Vector3.up, Quaternion.identity);
            var kvp = new KeyValuePair<Character, Vector3>(character, go.transform.position);
            data.Add(go, kvp);
        }

        if (data.Count > 0)
        {
            p.StartCoroutine(CannonballMovement(data));
            StartCooldown(this.CalculateSkillCooldown());
        }
        else
        {
            p.AddEitr(this.CalculateSkillManacost());
        }
    }

    private IEnumerator CannonballMovement(Dictionary<GameObject, KeyValuePair<Character, Vector3>> instance)
    {
        float val = this.CalculateSkillValue();
        float count = 0;
        while (count < 1f)
        {
            if (!Player.m_localPlayer) break;
            count += 1f * Time.deltaTime;
            foreach (var c in instance)
            {
                if (!c.Value.Key) continue;

                var point = c.Value.Value + (c.Value.Key.transform.position - c.Value.Value) / 2 + Vector3.up * 7.0f;
                var m1 = Vector3.Lerp(c.Value.Value, point, count);
                var m2 = Vector3.Lerp(point, c.Value.Key.transform.position + Vector3.up, count);
                c.Key.transform.position = Vector3.Lerp(m1, m2, count);
            }

            yield return null;
        }
        
        foreach (var go in instance)
        {
            if (go.Value.Key)
            {
                go.Value.Key.Heal(val);
                UnityEngine.Object.Instantiate(Explosion, go.Value.Key.transform.position, Quaternion.identity);
            }

            ZNetScene.instance.Destroy(go.Key.gameObject);
        }
    }

    public override bool CanExecute()
    {
        return !Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Heal, Affects Party Members Nearby</color>";
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

        builder.AppendLine($"Healing: {Math.Round(currentValue, 1)}");
        builder.AppendLine($"Cooldown: {Math.Round(currentCooldown, 1)}");
        builder.AppendLine($"Manacost: {Math.Round(currentManacost, 1)}");

        if (this.Level < maxLevel && this.Level > 0)
        {
            float nextValue = this.CalculateSkillValue(forLevel + 1);
            float nextCooldown = this.CalculateSkillCooldown(forLevel + 1);
            float nextManacost = this.CalculateSkillManacost(forLevel + 1);
            float cooldownDiff = nextCooldown - currentCooldown;
            float manacostDiff = nextManacost - currentManacost;
            float valueDiff = nextValue - currentValue;
            
            var roundedCooldownDiff = Math.Round(cooldownDiff, 1);
            var roundedManacostDiff = Math.Round(manacostDiff, 1);
            var roundedValueDiff = Math.Round(valueDiff, 1);

            builder.AppendLine($"\nNext Level:");
            builder.AppendLine(
                $"Healing: {Math.Round(nextValue, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
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
    public override Color SkillColor => new Color(0.05f, 1f, 0.04f);
}