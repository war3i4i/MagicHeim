using System.Text;
using JetBrains.Annotations;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Interfaces;
using MagicHeim.SkillsDatabase.GlobalMechanics;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Druid_Shield : MH_Skill
{
    private static GameObject Buff;
    private static GameObject TextPrefab;

    public Druid_Shield()
    {
        _definition._InternalName = "Druid_Shield";
        _definition.Name = "$mh_druid_shield";
        _definition.Description = "$mh_druid_shield_desc";
 
        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Shield Value", 20f,
            "Shield Value (Min Lvl)");

        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Shield Value", 400f,
            "Shield Value (Max Lvl)");

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


        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_Shield_Icon");
        CachedIcon = _definition.Icon;
        _definition.Video = "https://kg.sayless.eu/skills/MH_Druid_Shield.mp4";
        _definition.Animation = ClassAnimationReplace.MH_AnimationNames[ClassAnimationReplace.MH_Animation.MageWave];
        _definition.AnimationTime = 0.8f;
        Buff = MagicHeim.asset.LoadAsset<GameObject>("Druid_Shield_Prefab");
        TextPrefab = MagicHeim.asset.LoadAsset<GameObject>("Druid_Shield_Text");
        TextPrefab.AddComponent<MH_FollowCameraRotation>();
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
            __instance.m_namedPrefabs[Buff.name.GetStableHashCode()] = Buff;
        }
    }


    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        Player p = Player.m_localPlayer;
        int shieldValue = (int)this.CalculateSkillValue();
        List<Player> players = Player.GetAllPlayers()
            .Where(x => Vector3.Distance(x.transform.position, p.transform.position) <= 10f).ToList();
        foreach (Player player in players)
        {
            if (!Utils.IsPlayerInGroup(player)) continue;
            player.GetSEMan().AddStatusEffect("Druid_Shield_Buff".GetStableHashCode(), true, shieldValue);
        }

        StartCooldown(this.CalculateSkillCooldown());
    }

    public override bool CanExecute()
    {
        return !Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Magic Shield, Damage Absorption</color>";
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

        builder.AppendLine($"Damage Absorption: {Math.Round(currentValue, 1)}");
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
            builder.AppendLine(
                $"Damage Absorption: {Math.Round(nextValue, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
            builder.AppendLine(
                $"Cooldown: {Math.Round(nextCooldown, 1)} <color=green>({(roundedCooldownDiff > 0 ? "+" : "")}{roundedCooldownDiff})</color>");
            builder.AppendLine(
                $"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }


        return builder.ToString();
    }

    public class SE_Druid_Shield_Buff : StatusEffect
    {
        public int shieldAmount;
        public float takenDamage;
        private GameObject numbers;
        private Text text;
        
        public SE_Druid_Shield_Buff()
        {
            name = "Druid_Shield_Buff";
            m_tooltip = "Damage Absorption";
            m_icon = CachedIcon;
            m_name = "Shield";
            m_ttl = 30;
            m_startEffects = new EffectList
            {
                m_effectPrefabs = new[]
                {
                    new EffectList.EffectData()
                    {
                        m_attach = true, m_enabled = true, m_inheritParentRotation = true,
                        m_inheritParentScale = true,
                        m_prefab = Buff
                    }
                }
            };
        }

        public override bool IsDone()
        {
            return takenDamage > shieldAmount || base.IsDone();
        }
        
        public override void SetLevel(int itemLevel, float skillLevel)
        {
            shieldAmount = itemLevel;
        }

        public override void OnDamaged(HitData hit, Character attacker)
        {
            float totalDamage = hit.GetTotalDamage(); 
            takenDamage += totalDamage;
            hit.ApplyModifier(0f); 
        }

        public override void Setup(Character character)
        {
            base.Setup(character);
            numbers = Instantiate(TextPrefab, character.transform);
            text = numbers.GetComponentInChildren<Text>();
        }

        public override void UpdateStatusEffect(float dt)
        {
            base.UpdateStatusEffect(dt);
            text.text = $"{Mathf.Max(0,(int)(shieldAmount - takenDamage))}";
            if (!IsDone()) return;
            if(numbers) Destroy(numbers);
        }
    }

    public static class Mage_ThunderSock_DB_Patches
    {
        private static void Add_SE(ObjectDB odb)
        {
            if (ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0 ||
                ObjectDB.instance.GetItemPrefab("Amber") == null) return;

            if (!odb.m_StatusEffects.Find(se => se.name == "Druid_Shield_Buff"))
                odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Druid_Shield_Buff>());
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

    public override bool CanRightClickCast => true;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => new Color(0.43f, 1f, 0.33f);
}