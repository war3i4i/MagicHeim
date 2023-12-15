using System.Text;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Interfaces;
using MagicHeim.SkillsDatabase.GlobalMechanics;
using Random = UnityEngine.Random;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Druid_Eclipse : MH_Skill
{
    private static GameObject Prefab;
    private static GameObject Explosion;

    public Druid_Eclipse()
    {
        _definition._InternalName = "Druid_Eclipse";
        _definition.Name = "$mh_druid_eclipse";
        _definition.Description = "$mh_druid_eclipse_desc";

        _definition.AnimationTime = 0.5f;
        _definition.Animation = ClassAnimationReplace.MH_AnimationNames[ClassAnimationReplace.MH_Animation.TwoHandedSummon];

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Damage", 1f,
            "Damage amount (Min Lvl)");
        
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Damage", 10f,
            "Damage amount (Max Lvl)");
        
        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Manacost", 10f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Manacost", 1f,
            "Manacost amount (Max Lvl)");

        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 10,
            "Max Skill Level");

        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn",
            1, "Required Level");
        
        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 1,
            "Leveling Step");
        
        _definition.MinLvlDuration = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Tick Speed", 1f,
            "Tick Speed (Min Lvl)");
        
        _definition.MaxLvlDuration = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Tick Speed", 0.5f,
            "Tick Speed (Max Lvl)");

        _definition.ExternalValues = new()
        {
            MagicHeim.config($"{_definition._InternalName}", "MIN LVl max targets", 1f, "Max targets per tick"),
            MagicHeim.config($"{_definition._InternalName}", "MAX Lvl max target", 4f, "Max targets per tick"),
        };
        

        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_Eclipse_Icon");
        _definition.Video = "https://kg.sayless.eu/skills/MH_Druid_Eclipse.mp4";
        Prefab = MagicHeim.asset.LoadAsset<GameObject>("Druid_Eclipse_Prefab");
        Prefab.AddComponent<MH_FollowTargetComponent>();
        Explosion = MagicHeim.asset.LoadAsset<GameObject>("Druid_Eclipse_Explosion");
        Explosion.AddComponent<MH_FollowTargetComponent>();

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
        if (!Toggled)
        {
            MagicHeim._thistype.StartCoroutine(EclipseCorout(this.CalculateSkillValue(), Mathf.FloorToInt(this.CalculateSkillExternalValue(0)), this.CalculateSkillDuration()));
        }
        else 
        {
            Toggled = false;
        }
    }
    
    
    private static GameObject eclipse;

    private IEnumerator EclipseCorout(float dmg, int maxTargets, float periodicTime)
    {
        float manacost = this.CalculateSkillManacost();
        Toggled = true;
        float periodic = periodicTime;
        Player p = Player.m_localPlayer;
        if (eclipse) ZNetScene.instance.Destroy(eclipse.gameObject);
        eclipse = UnityEngine.Object.Instantiate(Prefab, p.transform.position, Quaternion.identity);
        eclipse.GetComponent<MH_FollowTargetComponent>().Setup(p);
        for (;;)
        {
            float useMana = manacost * Time.deltaTime; 
            if (!Toggled || p.IsDead() || !p.HaveEitr(useMana) || p.InWater())
            {
                Toggled = false; 
                StartCooldown(3);
                if (eclipse)
                {
                    eclipse.GetComponent<ZNetView>().ClaimOwnership();
                    ZNetScene.instance.Destroy(eclipse.gameObject);
                }
                yield break;  
            }
            
            periodic -= Time.deltaTime;
            if (periodic <= 0)
            {
                periodic = periodicTime;
                
                IEnumerable<Character> characters8M = Character.s_characters.Where(x => Utils.IsEnemy(x) && Vector3.Distance(x.transform.position, p.transform.position) <= 12f);
                characters8M = characters8M.OrderBy(x => Random.Range(0, 100)).Take(maxTargets);
                Vector3 pPos = p.transform.position;
                foreach (Character character in characters8M)
                {
                    GameObject explosion = UnityEngine.Object.Instantiate(Explosion, character.transform.position, Quaternion.identity);
                    explosion.GetComponent<MH_FollowTargetComponent>().Setup(character);
                    HitData hitData = new();
                    hitData.m_skill = Skills.SkillType.ElementalMagic;
                    hitData.m_damage.m_pierce = dmg;
                    hitData.m_point = character.m_collider.ClosestPoint(pPos);
                    hitData.m_ranged = true;
                    hitData.SetAttacker(Player.m_localPlayer); 
                    character.DamageMH(hitData);
                }
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
        return "<color=red>Toggle, AoE, Damage</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine("\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentManacost = this.CalculateSkillManacost(forLevel);
        float currentValue = this.CalculateSkillValue(forLevel);
        int maxTargets = Mathf.FloorToInt(this.CalculateSkillExternalValue(0, forLevel));
        float periodicTime = this.CalculateSkillDuration(forLevel);
        builder.AppendLine($"Damage: <color=#FF00FF>Piercing  {Math.Round(currentValue, 1)}</color>");
        builder.AppendLine($"Max Targets: {maxTargets}");
        builder.AppendLine($"Tick Speed: {Math.Round(periodicTime, 1)}");
        builder.AppendLine($"Manacost (Per Second): {Math.Round(currentManacost, 1)}");

        if (Level < maxLevel && Level > 0)
        {
            float nextManacost = this.CalculateSkillManacost(forLevel + 1);
            float nextValue = this.CalculateSkillValue(forLevel + 1);
            int nextMaxTargets = Mathf.FloorToInt(this.CalculateSkillExternalValue(0, forLevel + 1));
            float nextPeriodicTime = this.CalculateSkillDuration(forLevel + 1);
            float manacostDiff = nextManacost - currentManacost;
            float valueDiff = nextValue - currentValue;
            int maxTargetsDiff = nextMaxTargets - maxTargets;
            float periodicTimeDiff = nextPeriodicTime - periodicTime;
            double roundedManacostDiff = Math.Round(manacostDiff, 1);
            double roundedValueDiff = Math.Round(valueDiff, 1);
            double roundedPeriodicTimeDiff = Math.Round(periodicTimeDiff, 1);

            builder.AppendLine("\nNext Level:");
            builder.AppendLine($"Damage: <color=#FF00FF>Piercing  {Math.Round(nextValue, 1)}</color> <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
            builder.AppendLine($"Max Targets: {nextMaxTargets} <color=green>({(maxTargetsDiff > 0 ? "+" : "")}{maxTargetsDiff})</color>");
            builder.AppendLine($"Tick Speed: {Math.Round(nextPeriodicTime, 1)} <color=green>({(roundedPeriodicTimeDiff > 0 ? "+" : "")}{roundedPeriodicTimeDiff})</color>");
            builder.AppendLine($"Manacost (Per Second): {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }


        return builder.ToString();
    }

    public override bool CanRightClickCast => true;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => new Color(0.09f, 0.05f, 0.17f);
}