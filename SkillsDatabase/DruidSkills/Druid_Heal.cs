using System.Text;
using JetBrains.Annotations;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Interfaces;

namespace MagicHeim.SkillsDatabase.DruidSkills;

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
            "MIN Lvl Heal", 1f,
            "Heal amount (Min Lvl)");

        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Heal", 10f,
            "Heal amount (Max Lvl)");

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

        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_Heal_Icon");
        _definition.Video = "https://kg.sayless.eu/skills/MH_Druid_Heal.mp4";
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
        [UsedImplicitly]
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
        float heal = this.CalculateSkillValue();
        Dictionary<GameObject, KeyValuePair<Character, Vector3>> data = new Dictionary<GameObject, KeyValuePair<Character, Vector3>>();
        List<Character> list = Character.GetAllCharacters().Where(d =>
                Vector3.Distance(p.transform.position, d.transform.position) <= 20f &&
                !Utils.IsEnemy(d))
            .ToList();
        foreach (Character character in list)
        {
            if (character.IsPlayer() && !Utils.IsPlayerInGroup((Player)character)) continue;
            GameObject go = UnityEngine.Object.Instantiate(Prefab, p.transform.position + Vector3.up, Quaternion.identity);
            KeyValuePair<Character, Vector3> kvp = new KeyValuePair<Character, Vector3>(character, go.transform.position);
            data.Add(go, kvp);
        }

        if (data.Count > 0)
        {
            p.StartCoroutine(CannonballMovement(data, heal));
            StartCooldown(this.CalculateSkillCooldown());
        }
        else
        {
            p.AddEitr(this.CalculateSkillManacost());
        }
    }

    private IEnumerator CannonballMovement(Dictionary<GameObject, KeyValuePair<Character, Vector3>> instance, float val)
    {
        float count = 0;
        while (count < 1f)
        {
            if (!Player.m_localPlayer) break;
            count += 1f * Time.deltaTime;
            foreach (KeyValuePair<GameObject, KeyValuePair<Character, Vector3>> c in instance)
            {
                if (!c.Value.Key) continue;

                Vector3 point = c.Value.Value + (c.Value.Key.transform.position - c.Value.Value) / 2 + Vector3.up * 7.0f;
                Vector3 m1 = Vector3.Lerp(c.Value.Value, point, count);
                Vector3 m2 = Vector3.Lerp(point, c.Value.Key.transform.position + Vector3.up, count);
                c.Key.transform.position = Vector3.Lerp(m1, m2, count);
            }

            yield return null;
        }

        foreach (KeyValuePair<GameObject, KeyValuePair<Character, Vector3>> go in instance)
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
        builder.AppendLine("\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentValue = this.CalculateSkillValue(forLevel);
        float currentCooldown = this.CalculateSkillCooldown(forLevel);
        float currentManacost = this.CalculateSkillManacost(forLevel);

        builder.AppendLine($"Healing: <color=#00FF00>{Math.Round(currentValue, 1)}</color>");
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
            builder.AppendLine($"Healing: <color=#00FF00>{Math.Round(nextValue, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color></color>");
            builder.AppendLine($"Cooldown: {Math.Round(nextCooldown, 1)} <color=green>({(roundedCooldownDiff > 0 ? "+" : "")}{roundedCooldownDiff})</color>");
            builder.AppendLine($"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }


        return builder.ToString();
    }


    public override bool CanRightClickCast => true;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => new Color(0.05f, 1f, 0.04f);
}