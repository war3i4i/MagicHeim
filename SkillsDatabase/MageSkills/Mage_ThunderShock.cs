using System.Text;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Interfaces;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Mage_ThunderShock : MH_Skill
{
    private static GameObject Thunder_Prefab;
    private static GameObject Thunder_Debuff;

    public Mage_ThunderShock()
    {
        _definition._InternalName = "Mage_Thundershock";
        _definition.Name = "$mh_mage_thundershock";
        _definition.Description = "$mh_mage_thundershock_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Damage", 25f,
            "Damage amount (Min Lvl)");

        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Damage", 50f,
            "Damage amount (Max Lvl)");

        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Manacost", 15f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Manacost", 30f,
            "Manacost amount (Max Lvl)");

        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Cooldown", 24f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Cooldown", 12f,
            "Cooldown amount (Max Lvl)");

        _definition.MinLvlDuration = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Duration", 3f,
            "Duration amount (Min Lvl)");
        _definition.MaxLvlDuration = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Duration", 6f,
            "Duration amount (Max Lvl)");

        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 10,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn",
            62, "Required Level");


        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 1,
            "Leveling Step");

        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Mage_ThunderShock_Icon");
        CachedIcon = _definition.Icon;
        _definition.Video = "https://kg.sayless.eu/skills/MH_Mage_ThunderShock.mp4";
        _definition.Animation = ClassAnimationReplace.MH_AnimationNames[ClassAnimationReplace.MH_Animation.MageSlam];
        _definition.AnimationTime = 0.8f;
        Thunder_Prefab = MagicHeim.asset.LoadAsset<GameObject>("Mage_ThunderShock_Prefab");
        Thunder_Debuff = MagicHeim.asset.LoadAsset<GameObject>("Mage_ThunderShock_Debuff");

        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
        this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
        this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);
    }

    private static Sprite CachedIcon;

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScene_Awake_Patch
    {
        static void Postfix(ZNetScene __instance)
        {
            __instance.m_namedPrefabs[Thunder_Prefab.name.GetStableHashCode()] = Thunder_Prefab;
            __instance.m_namedPrefabs[Thunder_Debuff.name.GetStableHashCode()] = Thunder_Debuff;
        }
    }

    public static readonly int Script_Layermask2 =
        LayerMask.GetMask("Default", "character", "character_noenv", "character_net", "character_ghost", "piece",
            "piece_nonsolid", "terrain", "static_solid");

    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        Player p = Player.m_localPlayer;
        bool castHit = Physics.Raycast(Utils.GetPerfectEyePosition(), p.GetLookDir(),
            out RaycastHit raycast, 30f, Script_Layermask2);
        Vector3 target;
        if (castHit && raycast.collider)
        {
            float duration = this.CalculateSkillDuration();
            float damage = this.CalculateSkillValue();
            target = raycast.point;
            Collider[] array = Physics.OverlapSphere(target, 8f, Script_Layermask2, QueryTriggerInteraction.UseGlobal);
            HashSet<GameObject> hashSet = new HashSet<GameObject>();
            foreach (Collider collider in array)
            {
                GameObject gameObject = Projectile.FindHitObject(collider);
                IDestructible component = gameObject.GetComponent<IDestructible>();
                if (component != null && !hashSet.Contains(gameObject))
                {
                    hashSet.Add(gameObject);
                    if (component is Character character)
                    {
                        if (!Utils.IsEnemy(character)) continue;
                        HitData hit = new();
                        hit.m_skill = Skills.SkillType.ElementalMagic;
                        hit.m_damage.m_lightning = damage;
                        hit.m_point = collider.ClosestPoint(target);
                        hit.m_ranged = true;
                        hit.SetAttacker(Player.m_localPlayer);
                        character.GetSEMan().AddStatusEffect("Mage_ThunderShock_Debuff".GetStableHashCode(), false, 0, duration);
                        component.DamageMH(hit);
                    }
                }
            }

            UnityEngine.Object.Instantiate(Thunder_Prefab, target, Quaternion.identity);
            StartCooldown(this.CalculateSkillCooldown());
        }
        else
        {
            p.AddEitr(this.CalculateSkillManacost());
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                "<color=#00FFFF>Too Far</color>");
        }
    }

    public override bool CanExecute()
    {
        return !Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>AoE, Debuff, Damage, Blind</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine("\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentValue = this.CalculateSkillValue(forLevel);
        float currentDuration = this.CalculateSkillDuration(forLevel);
        float currentCooldown = this.CalculateSkillCooldown(forLevel);
        float currentManacost = this.CalculateSkillManacost(forLevel);


        builder.AppendLine($"Damage: <color=blue>Lightning {Math.Round(currentValue, 1)}</color>");
        builder.AppendLine($"Duration: {Math.Round(currentDuration, 1)}");
        builder.AppendLine($"Cooldown: {Math.Round(currentCooldown, 1)}");
        builder.AppendLine($"Manacost: {Math.Round(currentManacost, 1)}");

        if (Level < maxLevel && Level > 0)
        {
            float nextValue = this.CalculateSkillValue(forLevel + 1);
            float nextDuration = this.CalculateSkillDuration(forLevel + 1);
            float nextCooldown = this.CalculateSkillCooldown(forLevel + 1);
            float nextManacost = this.CalculateSkillManacost(forLevel + 1);
            float durationDiff = nextDuration - currentDuration;
            float cooldownDiff = nextCooldown - currentCooldown;
            float manacostDiff = nextManacost - currentManacost;
            float valueDiff = nextValue - currentValue;


            double roundedDurationDiff = Math.Round(durationDiff, 1);
            double roundedCooldownDiff = Math.Round(cooldownDiff, 1);
            double roundedManacostDiff = Math.Round(manacostDiff, 1);
            double roundedValueDiff = Math.Round(valueDiff, 1);

            builder.AppendLine("\nNext Level:");
            builder.AppendLine(
                $"Damage: <color=blue>Lightning {Math.Round(nextValue, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color></color>");
            builder.AppendLine(
                $"Duration: {Math.Round(nextDuration, 1)} <color=green>({(roundedDurationDiff > 0 ? "+" : "")}{roundedDurationDiff})</color>");
            builder.AppendLine(
                $"Cooldown: {Math.Round(nextCooldown, 1)} <color=green>({(roundedCooldownDiff > 0 ? "+" : "")}{roundedCooldownDiff})</color>");
            builder.AppendLine(
                $"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }


        return builder.ToString();
    }

    public class SE_Mage_ThunderShock_Debuff : StatusEffect
    {
        public SE_Mage_ThunderShock_Debuff()
        {
            name = "Mage_ThunderShock_Debuff";
            m_tooltip = "Blind";
            m_icon = CachedIcon;
            m_name = "Blind";
            m_ttl = 100;
            m_startEffects = new EffectList
            {
                m_effectPrefabs = new[]
                {
                    new EffectList.EffectData()
                    {
                        m_attach = true, m_enabled = true, m_inheritParentRotation = true,
                        m_inheritParentScale = true,
                        m_prefab = Thunder_Debuff
                    }
                }
            };
        }

        public override void SetLevel(int itemLevel, float skillLevel)
        {
            m_ttl = skillLevel;
        }

        public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
        {
            hitData.ApplyModifier(0);
        }
    }

    public static class Mage_ThunderSock_DB_Patches
    {
        private static void Add_SE(ObjectDB odb)
        {
            if (ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0 ||
                ObjectDB.instance.GetItemPrefab("Amber") == null) return;

            if (!odb.m_StatusEffects.Find(se => se.name == "Mage_ThunderShock_Debuff"))
                odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Mage_ThunderShock_Debuff>());
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

    public override bool CanRightClickCast => false;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => new Color(0.05f, 0.08f, 1f);
}