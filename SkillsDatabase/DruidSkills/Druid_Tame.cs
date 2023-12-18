using System.Text;
using JetBrains.Annotations;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Interfaces;
using MagicHeim.UI_s;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Druid_Tame : MH_Skill
{
    private static GameObject Prefab;

    public Druid_Tame()
    {
        _definition._InternalName = "Druid_Tame";
        _definition.Animation = ClassAnimationReplace.MH_AnimationNames[ClassAnimationReplace.MH_Animation.MageSummon];
        _definition.Name = "$mh_druid_tame";
        _definition.Description = "$mh_druid_tame_desc";

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

        _definition.MinLvlChargeTime = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Charge Time", 10,
            "Charge Time amount (Min Lvl)");
        _definition.MaxLvlChargeTime = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Charge Time", 1,
            "Charge Time amount (Max Lvl)");

        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 10,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn",
            1, "Required Level");


        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 1,
            "Leveling Step");

        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
        this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
        this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);

        _definition.AnimationTime = 0.6f;
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_Tame_Icon");
        _definition.Video = "https://kg.sayless.eu/skills/MH_Druid_Tame.mp4";
        Prefab = MagicHeim.asset.LoadAsset<GameObject>("Druid_Tame_Prefab");
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScene_Awake_Patch
    {
        [UsedImplicitly]
        static void Postfix(ZNetScene __instance)
        {
            __instance.m_namedPrefabs[Prefab.name.GetStableHashCode()] = Prefab;
        }
    }

    public static readonly int Script_Layermask = LayerMask.GetMask("character", "character_noenv", "character_net", "character_ghost", "piece", "piece_nonsolid", "terrain");
    
    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        Player p = Player.m_localPlayer;
        p.m_collider.enabled = false;
        bool castHit = Physics.Raycast(GameCamera.instance.transform.position, p.GetLookDir(), out RaycastHit raycast, 50f, Script_Layermask);
        p.m_collider.enabled = true;
        if (castHit && raycast.collider && raycast.collider.GetComponentInParent<Character>() is { } enemy)
        {
            if (Vector3.Distance(enemy.transform.position, p.transform.position) > 30f)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                    "<color=#00FF00>Too</color><color=yellow> far</color>");
                p.AddEitr(this.CalculateSkillManacost());
                return;
            }

            if (enemy.GetComponent<Tameable>() is not { } tame)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                    "<color=#00FF00>Not</color><color=yellow> tameable</color>");
                p.AddEitr(this.CalculateSkillManacost());
                return;
            }

            if (enemy.IsTamed())
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                    "<color=#00FF00>Already</color><color=yellow> tamed</color>");
                p.AddEitr(this.CalculateSkillManacost());
                return;
            }

            MagicHeim._thistype.StartCoroutine(Charge(Cond,enemy));
        }
        else
        {
            p.AddEitr(this.CalculateSkillManacost());
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "<color=#00FFFF>No</color><color=yellow> target</color>");
        }
        
    }

    private GameObject VFX;
    
    private IEnumerator Charge(Func<bool> Cond, Character target)
    {
        float charge = 0f;
        float cooldown = this.CalculateSkillCooldown();
        float maxCharge = Mathf.Max(0.1f, this.CalculateSkillChargeTime());
        float additionalMultiplier = 0.5f * target.GetLevel() - 1;
        maxCharge *= 1f + additionalMultiplier;
        SkillChargeUI.ShowCharge(this, maxCharge);
        float animationTime = _definition.AnimationTime * 1f;
        Player p = Player.m_localPlayer;
        VFX = UnityEngine.Object.Instantiate(Prefab, target.transform);
        VFX.transform.localScale *= target.m_collider.bounds.size.magnitude;
        while (Cond() && charge < maxCharge && p && !p.IsDead() || !target || target.IsDead())
        {
            
            Vector3 rot = (target.transform.position - p.transform.position).normalized;
            rot.y = 0;
            p.transform.rotation = Quaternion.LookRotation(rot);
            charge += Time.deltaTime; 
             
            animationTime -= Time.deltaTime;
            if (animationTime <= 0)
            {
                animationTime = _definition.AnimationTime * 1f;
                SkillCastHelper.PlayAnimation(this);
            }

            yield return null;
        }

        if (VFX)
        {
            VFX.GetComponent<ZNetView>().ClaimOwnership();
            ZNetScene.instance.Destroy(VFX);
        }
        SkillChargeUI.RemoveCharge(this);
        if (!p || p.IsDead()) yield break;
        if (charge < maxCharge)
        {
            p.AddEitr(this.CalculateSkillManacost()); 
            StartCooldown(3f);
            yield break;
        } 
        
        StartCooldown(cooldown);
        if (target)
        {
            UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_creature_tamed"), target.transform.position, Quaternion.identity);
            if (target.GetComponent<Tameable>() is { } tameable)
            {
                target.m_nview.ClaimOwnership();
                tameable.Tame();
            }
        }
    }


    public override bool CanExecute()
    {
        return !Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Target Tameable Creature, Tame on cast finish</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine("\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentChargeTime = this.CalculateSkillChargeTime(forLevel);
        float currentCooldown = this.CalculateSkillCooldown(forLevel);
        float currentManacost = this.CalculateSkillManacost(forLevel);
        
        builder.AppendLine($"Cooldown: {Math.Round(currentCooldown, 1)}");
        builder.AppendLine($"Manacost: {Math.Round(currentManacost, 1)}");
        builder.AppendLine($"Charge Time: {Math.Round(currentChargeTime, 1)}");

        if (Level < maxLevel && Level > 0)
        {
            float nextChargeTime = this.CalculateSkillChargeTime(forLevel + 1);
            float nextCooldown = this.CalculateSkillCooldown(forLevel + 1);
            float nextManacost = this.CalculateSkillManacost(forLevel + 1);
            float chargeTimeDiff = nextChargeTime - currentChargeTime;
            float cooldownDiff = nextCooldown - currentCooldown;
            float manacostDiff = nextManacost - currentManacost;
            double roundedChargeTimeDiff = Math.Round(chargeTimeDiff, 1);
            double roundedCooldownDiff = Math.Round(cooldownDiff, 1);
            double roundedManacostDiff = Math.Round(manacostDiff, 1);

            builder.AppendLine("\nNext Level:");
            builder.AppendLine($"Cooldown: {Math.Round(nextCooldown, 1)} <color=green>({(roundedCooldownDiff > 0 ? "+" : "")}{roundedCooldownDiff})</color>");
            builder.AppendLine($"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
            builder.AppendLine($"Charge Time: {Math.Round(nextChargeTime, 1)} <color=green>({(roundedChargeTimeDiff > 0 ? "+" : "")}{roundedChargeTimeDiff})</color>");
        }


        return builder.ToString();
    }

    public override bool CanRightClickCast => false;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => new Color(1f, 0.27f, 0.15f);
}