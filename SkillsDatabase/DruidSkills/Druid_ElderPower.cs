using System.Text;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Interfaces;
using MagicHeim.UI_s;
using Random = UnityEngine.Random;

namespace MagicHeim.SkillsDatabase.DruidSkills;

public sealed class Druid_ElderPower : MH_Skill
{
    private static readonly GameObject InactiveGO = new(){name = "Elder_GO_DruidElderPower_Inactive", hideFlags = HideFlags.HideAndDontSave};
    private static GameObject TentaRoot_GO;
    private static GameObject Preload;
    
    private static GameObject RangeShowup;
    private static GameObject TargetPoint;
    

    public Druid_ElderPower()
    {
        InactiveGO.SetActive(false);
        _definition._InternalName = "Druid_ElderPower";
        _definition.Name = "$mh_druid_elderpower";
        _definition.Description = "$mh_druid_elderpower_desc";
        
        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Damage", 1f,
            "Value amount (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Damage", 10f,
            "Value amount (Max Lvl)");

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
        
        _definition.MinLvlDuration = MagicHeim.config($"{_definition._InternalName}", 
            "MIN Lvl Duration", 5f,
            "Duration amount (Min Lvl)");
        _definition.MaxLvlDuration = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Duration", 15f,
            "Duration amount (Max Lvl)");

        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 10,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn",
            1, "Required Level");


        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 1,
            "Leveling Step");
        
        _definition.ExternalValues =
        [
            MagicHeim.config($"{_definition._InternalName}", "MIN Lvl HP", 50f, "HP"),
            MagicHeim.config($"{_definition._InternalName}", "MAX Lvl HP", 300f, "HP"),
            MagicHeim.config($"{_definition._InternalName}", "MIN Lvl AMOUNT", 2f, "HP"),
            MagicHeim.config($"{_definition._InternalName}", "MAX Lvl AMOUNT", 10f, "HP")
        ];

        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_ElderPower_Icon");
        _definition.Video = "https://kg.sayless.eu/skills/MH_Druid_ElderPower.mp4";
        RangeShowup = MagicHeim.asset.LoadAsset<GameObject>("Druid_AreaShowup");
        TargetPoint = MagicHeim.asset.LoadAsset<GameObject>("Druid_ElderPower_AreaShowup");
        Preload = MagicHeim.asset.LoadAsset<GameObject>("Druid_ElderPower_Preload");
        this.InitRequiredItemFirstHalf("Wood", 1, 1.88f);
        this.InitRequiredItemSecondHalf("Coins", 1, 1.88f);
        this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScene_Awake_Patch
    {
        static void Postfix(ZNetScene __instance)
        { 
            if (!TentaRoot_GO)
            {
                GameObject _origGO = __instance.GetPrefab("TentaRoot");
                TentaRoot_GO = UnityEngine.Object.Instantiate(_origGO, InactiveGO.transform);
                TentaRoot_GO.name = "TentaRoot_GO_DruidElderPower";
                TentaRoot_GO.gameObject.SetActive(true);
                TentaRoot_GO.gameObject.transform.localPosition = Vector3.zero;
                TentaRoot_GO.gameObject.transform.localRotation = Quaternion.identity;

                Material mat = MagicHeim.asset.LoadAsset<Material>("DruidMat");
                foreach (Renderer renderer in TentaRoot_GO.GetComponentsInChildren<Renderer>())
                    renderer.sharedMaterial = mat;

                CharacterTimedDestruction timed = TentaRoot_GO.GetComponent<CharacterTimedDestruction>();
                timed.m_timeoutMin = 1f;
                timed.m_timeoutMax = 1f;
                timed.m_triggerOnAwake = true;

                TentaRoot_GO.GetComponent<Humanoid>().m_faction = Character.Faction.Players;
                TentaRoot_GO.GetComponent<ZNetView>().m_persistent = false;

            }
            __instance.m_namedPrefabs[Preload.name.GetStableHashCode()] = Preload;
            __instance.m_namedPrefabs[TentaRoot_GO.name.GetStableHashCode()] = TentaRoot_GO;
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

    
    private static void Add_SE(ObjectDB odb)
    {
        if (ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0 ||
            ObjectDB.instance.GetItemPrefab("Amber") == null) return;

        if (!odb.m_StatusEffects.Find(se => se.name == "TentaRootIncreaseDMG_Druid"))
            odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_IncreaseTentaDamage>());
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

    private class SE_IncreaseTentaDamage : StatusEffect
    {
        public SE_IncreaseTentaDamage()
        {
            name = "TentaRootIncreaseDMG_Druid";
            m_name = "TentaRootIncreaseDMG";
        }
        
        private int _damage;

        public override void SetLevel(int itemLevel, float skillLevel)
        {
            _damage = itemLevel;
        }

        public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
        {
            hitData.ApplyModifier(0f);
            hitData.m_damage.m_blunt = _damage;
        }
    }

    private IEnumerator Charge(Func<bool> Cond)
    {
        bool cancel = false;
        SkillChargeUI.ShowCharge(this);
        Player p = Player.m_localPlayer;
        float maxDistance = 40f;
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
            bool castHit = Physics.Raycast(Utils.GetPerfectEyePosition(), p.GetLookDir(), out RaycastHit raycast, 100f, JumpMask);
            if (castHit && raycast.collider)
            {
                targetPoint.SetActive(true);
                target = raycast.point;
                targetPoint.transform.position = target;
            }
            else
            {
                targetPoint.SetActive(false);
                target = NON_Vector;
            }

            yield return null;
        }

        SkillChargeUI.RemoveCharge(this);
        if (!cancel && p && !p.IsDead() && target != NON_Vector && global::Utils.DistanceXZ(target, p.transform.position) <= maxDistance)
        {
            Vector3 rot = (target - p.transform.position).normalized;
            rot.y = 0;
            p.transform.rotation = Quaternion.LookRotation(rot);
            StartCooldown(this.CalculateSkillCooldown());
            p.m_zanim.SetTrigger(ClassAnimationReplace.MH_AnimationNames[ClassAnimationReplace.MH_Animation.MageSummon]);
            UnityEngine.Object.Instantiate(Preload, target, Quaternion.identity);
            float hp = this.CalculateSkillExternalValue(0);
            int dmg = (int)this.CalculateSkillValue();
            int amount = Mathf.CeilToInt(this.CalculateSkillExternalValue(2));
            float duration = this.CalculateSkillDuration();
            CharacterTimedDestruction timed = TentaRoot_GO.GetComponent<CharacterTimedDestruction>();
            timed.m_timeoutMin = duration;
            timed.m_timeoutMax = duration;
            for (int i = 0; i < amount; ++i)
            {
                float randomX = Random.Range(-6f, 6f);
                float randomz = Random.Range(-6f, 6f);
                Vector3 randomSpawn = new Vector3(target.x + randomX, target.y, target.z + randomz);
                ZoneSystem.instance.FindFloor(randomSpawn + Vector3.up * 3f, out float height);
                randomSpawn.y = height;
                GameObject tentaRoot = UnityEngine.Object.Instantiate(TentaRoot_GO, randomSpawn, Quaternion.identity);
                Humanoid tentaHumanoid = tentaRoot.GetComponent<Humanoid>();
                tentaHumanoid.SetMaxHealth(hp);
                tentaHumanoid.m_seman.AddStatusEffect("TentaRootIncreaseDMG_Druid".GetStableHashCode(), false, dmg);
            }
            timed.m_timeoutMin = 1f;
            timed.m_timeoutMax = 1f;
            
        }
        else
        {
            if (!cancel)
            {
                p.AddEitr(this.CalculateSkillManacost());
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "<color=#00FF00>Too far</color>");
            }
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
        return "<color=red>Precast, Creatures Spawn, AoE</color>";
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
        float currentDuration = this.CalculateSkillDuration(forLevel);
        float currentHP = this.CalculateSkillExternalValue(0, forLevel);
        int currentAmount = Mathf.CeilToInt(this.CalculateSkillExternalValue(2, forLevel));

        builder.AppendLine($"Damage: <color=yellow>Blunt {Math.Round(currentValue, 1)}</color>");
        builder.AppendLine($"Cooldown: {Math.Round(currentCooldown, 1)}");
        builder.AppendLine($"Manacost: {Math.Round(currentManacost, 1)}");
        builder.AppendLine($"Duration: {Math.Round(currentDuration, 1)}");
        builder.AppendLine($"HP: {Math.Round(currentHP, 1)}");
        builder.AppendLine($"Amount: {currentAmount}");

        if (Level < maxLevel && Level > 0)
        {
            float nextValue = this.CalculateSkillValue(forLevel + 1);
            float nextCooldown = this.CalculateSkillCooldown(forLevel + 1);
            float nextManacost = this.CalculateSkillManacost(forLevel + 1);
            float nextDuration = this.CalculateSkillDuration(forLevel + 1);
            float nextHP = this.CalculateSkillExternalValue(0, forLevel + 1);
            int nextAmount = Mathf.CeilToInt(this.CalculateSkillExternalValue(2, forLevel + 1));
            float valueDiff = nextValue - currentValue;
            float cooldownDiff = nextCooldown - currentCooldown;
            float manacostDiff = nextManacost - currentManacost;
            float durationDiff = nextDuration - currentDuration;
            float hpDiff = nextHP - currentHP;
            int amountDiff = nextAmount - currentAmount;

            double roundedValueDiff = Math.Round(valueDiff, 1);
            double roundedCooldownDiff = Math.Round(cooldownDiff, 1);
            double roundedManacostDiff = Math.Round(manacostDiff, 1);
            double roundedDurationDiff = Math.Round(durationDiff, 1);
            double roundedHPDiff = Math.Round(hpDiff, 1);
            

            builder.AppendLine("\nNext Level:");
            builder.AppendLine($"Damage: <color=yellow>Blunt {Math.Round(nextValue, 1)}</color> <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
            builder.AppendLine($"Cooldown: {Math.Round(nextCooldown, 1)} <color=green>({(roundedCooldownDiff > 0 ? "+" : "")}{roundedCooldownDiff})</color>");
            builder.AppendLine($"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
            builder.AppendLine($"Duration: {Math.Round(nextDuration, 1)} <color=green>({(roundedDurationDiff > 0 ? "+" : "")}{roundedDurationDiff})</color>");
            builder.AppendLine($"HP: {Math.Round(nextHP, 1)} <color=green>({(roundedHPDiff > 0 ? "+" : "")}{roundedHPDiff})</color>");
            builder.AppendLine($"Amount: {nextAmount} <color=green>({(amountDiff > 0 ? "+" : "")}{amountDiff})</color>");
        }


        return builder.ToString();
    }

    public override bool CanRightClickCast => false;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => new Color(0.35f, 1f, 0.33f);
}