using System.Text;
using JetBrains.Annotations;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Interfaces;

namespace MagicHeim.SkillsDatabase.DruidSkills;

public sealed class Druid_AspeedBuff : MH_Skill
{
    private static GameObject Prefab;

    static Druid_AspeedBuff()
    {
        AnimationSpeedManager.Add(AnimSpeedManager);
    }

    private static readonly int CachedAnimHash = "Druid_AspeedBuff_Buff".GetStableHashCode();
    
    private static double AnimSpeedManager(Character c, double speed)
    {
        if (!c.InAttack() || !c.m_nview.IsOwner()) return speed;
        SE_Druid_AspeedBuff se = c.m_seman.GetStatusEffect(CachedAnimHash) as SE_Druid_AspeedBuff;
        if (se == null) return speed;
        return speed * (1 + se.asBonus / 100f);
    }
     
    public Druid_AspeedBuff()
    {
        _definition._InternalName = "Druid_AspeedBuff";
        _definition.Name = "$mh_druid_aspeedbuff";
        _definition.Description = "$mh_druid_aspeedbuff_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl AttackSpeed (Percentage)", 20f,
            "AttackSpeed percentage (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl AttackSpeed (Percentage)", 60f,
            "AttackSpeed percentage (Max Lvl)");

        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Manacost", 20f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Manacost", 60f,
            "Manacost amount (Max Lvl)");

        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Cooldown", 240f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Cooldown", 120f,
            "Cooldown amount (Max Lvl)");

        _definition.MinLvlDuration = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Duration", 10f,
            "Duration amount (Min Lvl)");
        _definition.MaxLvlDuration = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Duration", 15f,
            "Duration amount (Max Lvl)");

        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 10,
            "Max Skill Level");
        
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn",
            45, "Required Level");

        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 3,
            "Leveling Step");


        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_AspeedBuff_Icon");
        _definition.Video = "https://kg.sayless.eu/skills/MH_Druid_AspeedBuff.mp4";
        _definition.Animation = ClassAnimationReplace.MH_AnimationNames[ClassAnimationReplace.MH_Animation.TwoHandedSummon];
        _definition.AnimationTime = 1.2f;
        Prefab = MagicHeim.asset.LoadAsset<GameObject>("Druid_AspeedBuff_Buff");
        CachedIcon = _definition.Icon;
        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
        this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
        this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);
    }

    private static Sprite CachedIcon;

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))] 
    static class ZNetScene_Awake_Patch
    {
        [UsedImplicitly]
        static void Postfix(ZNetScene __instance)
        { 
            __instance.m_namedPrefabs[Prefab.name.GetStableHashCode()] = Prefab;
        }
    }


    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        List<Player> list = Player.GetAllPlayers().Where(player => Vector3.Distance(player.transform.position, Player.m_localPlayer.transform.position) <= 20f && Utils.IsPlayerInGroup(player)).ToList();
        foreach (Player player in list)
        {
            player.m_seman.AddStatusEffect("Druid_AspeedBuff_Buff".GetStableHashCode(), true, (int)this.CalculateSkillDuration(), this.CalculateSkillValue());
        }
        StartCooldown(this.CalculateSkillCooldown());
    }


    public class SE_Druid_AspeedBuff : StatusEffect
    {
        public int asBonus;

        public SE_Druid_AspeedBuff()
        { 
            name = "Druid_AspeedBuff_Buff";
            m_tooltip = "";
            m_icon = CachedIcon;
            m_name = "$mh_druid_aspeedbuff";
            m_ttl = 100;
            m_startEffects = new EffectList
            {
                m_effectPrefabs = new[]
                {
                    new EffectList.EffectData()
                    {
                        m_attach = true, m_enabled = true, m_inheritParentRotation = true,
                        m_inheritParentScale = true, m_scale = true, m_prefab = Prefab
                    }
                }
            };
        }

        public override string GetTooltipString()
        {
            return $"\nAttackSpeed Increase: {asBonus}%".Localize();
        }

        public override void SetLevel(int itemLevel, float skillLevel)
        {
            m_ttl = itemLevel;
            asBonus = (int)skillLevel;
        }
    }

    public static class Mage_ThunderSock_DB_Patches
    {
        private static void Add_SE(ObjectDB odb)
        {
            if (ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0 ||
                ObjectDB.instance.GetItemPrefab("Amber") == null) return;

            if (!odb.m_StatusEffects.Find(se => se.name == "Druid_AspeedBuff_Buff"))
                odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Druid_AspeedBuff>());
        }

        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        public static class ObjectDBAwake
        {
            public static void Postfix(ObjectDB __instance)
            {
                Add_SE(__instance);
            }
        }

        [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
        public static class ObjectDBCopyOtherDB
        {
            public static void Postfix(ObjectDB __instance)
            {
                Add_SE(__instance);
            }
        }
    }


    public override bool CanExecute()
    {
        return !Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Buff, Affects Party Members Nearby, Attack Speed bonus</color>";
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
        float duration = this.CalculateSkillDuration(forLevel);

        builder.AppendLine($"AttackSpeed Increase: {Math.Round(currentValue, 1)}%");
        builder.AppendLine($"Duration: {Math.Round(duration, 1)}s");
        builder.AppendLine($"Cooldown: {Math.Round(currentCooldown, 1)}");
        builder.AppendLine($"Manacost: {Math.Round(currentManacost, 1)}");

        if (Level < maxLevel && Level > 0)
        {
            float nextValue = this.CalculateSkillValue(forLevel + 1);
            float nextCooldown = this.CalculateSkillCooldown(forLevel + 1); 
            float nextManacost = this.CalculateSkillManacost(forLevel + 1);
            float nextDuration = this.CalculateSkillDuration(forLevel + 1);
            float cooldownDiff = nextCooldown - currentCooldown;
            float manacostDiff = nextManacost - currentManacost;
            float valueDiff = nextValue - currentValue;
            float durationDiff = nextDuration - duration;

            double roundedCooldownDiff = Math.Round(cooldownDiff, 1);
            double roundedManacostDiff = Math.Round(manacostDiff, 1);
            double roundedValueDiff = Math.Round(valueDiff, 1);

            builder.AppendLine("\nNext Level:");
            builder.AppendLine($"AttackSpeed Increase: {Math.Round(nextValue, 1)}% <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
            builder.AppendLine($"Duration: {Math.Round(nextDuration, 1)}s <color=green>({(durationDiff > 0 ? "+" : "")}{Math.Round(durationDiff, 1)})</color>");
            builder.AppendLine($"Cooldown: {Math.Round(nextCooldown, 1)} <color=green>({(roundedCooldownDiff > 0 ? "+" : "")}{roundedCooldownDiff})</color>");
            builder.AppendLine($"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }

        return builder.ToString();
    }

    public override bool CanRightClickCast => true;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => new Color(0.02f, 0.77f, 1f);
}