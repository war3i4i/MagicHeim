using System.Text;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Classes;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;
using MagicHeim.UI_s;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Mage_FrostBeam : MH_Skill
{
    private static GameObject Thunder_Prefab;
    private static GameObject Thunder_Prepare;
    private static GameObject Thunder_Explosion;

    public Mage_FrostBeam()
    {
        _definition._InternalName = "Mage_Frostbeam";
        _definition.Name = "$mh_mage_frostbeam";
        _definition.Description = "$mh_mage_frostbeam_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Damage (Per Second)", 55f,
            "Value amount (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Damage (Per Second)", 90f,
            "Value amount (Max Lvl)");

        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Manacost", 15f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Manacost", 15f,
            "Manacost amount (Max Lvl)");

        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Cooldown", 30f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Cooldown", 12f,
            "Cooldown amount (Max Lvl)");


        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}", 
            $"Max Level", 10,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Required Level To Learn",
            40, "Required Level");


        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            $"Leveling Step", 3,
            "Leveling Step");
        
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Mage_FrostBeam_Icon");
        _definition.Video = "https://kg-dev.xyz/skills/MH_Mage_FrostBeam.mp4";
        _definition.Animation = "staff_shield";
        _definition.AnimationTime = 0.3f;
        Thunder_Prefab = MagicHeim.asset.LoadAsset<GameObject>("Mage_FrostBeam_Beam");
        Thunder_Prepare = MagicHeim.asset.LoadAsset<GameObject>("Mage_FrostBeam_Prefab2");
        Thunder_Explosion = MagicHeim.asset.LoadAsset<GameObject>("Mage_FrostBeam_Explosion");
        
        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))] 
    static class ZNetScene_Awake_Patch
    {  
        static void Postfix(ZNetScene __instance)
        {
            __instance.m_namedPrefabs[Thunder_Prefab.name.GetStableHashCode()] = Thunder_Prefab;
            __instance.m_namedPrefabs[Thunder_Prepare.name.GetStableHashCode()] = Thunder_Prepare;
            __instance.m_namedPrefabs[Thunder_Explosion.name.GetStableHashCode()] = Thunder_Explosion;
        }
    }

    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        MagicHeim._thistype.StartCoroutine(Charge(Cond));
    }


    public static readonly int Script_Layermask2 =
        LayerMask.GetMask("Default", "character", "character_noenv", "character_net", "character_ghost", "piece",
            "piece_nonsolid", "terrain", "static_solid");

    private IEnumerator Charge(Func<bool> Cond)
    {
        Player p = Player.m_localPlayer;
        int maxDistance = 30;
        int maxUsages = 32;
        float maxTime = (maxUsages + 1) / 4f;
        float count = 0;
        GameObject vfx = UnityEngine.Object.Instantiate(Thunder_Prepare,
            p.transform.position + p.transform.forward * 1f + Vector3.up * 1.5f, p.transform.rotation);
        SkillChargeUI.ShowCharge(this, 1f);
        while (count <= 1.2f)
        {
            if (!Cond())
            {
                SkillChargeUI.RemoveCharge(this);
                ZNetScene.instance.Destroy(vfx.gameObject);
                yield break;
            }

            vfx.transform.position = p.transform.position + p.transform.forward * 0.3f + Vector3.up * 1f;
            Vector3 target = Utils.GetPerfectEyePosition() + p.GetLookDir() * 100f;
            Vector3 rot = (target - p.transform.position).normalized;
            rot.y = 0;
            p.transform.rotation = Quaternion.LookRotation(rot);
            vfx.transform.rotation = p.transform.rotation;
            count += Time.deltaTime;
            yield return null;
        }

        SkillChargeUI.RemoveCharge(this);
        SkillChargeUI.ShowCharge(this, maxTime);
        p.m_zanim.SetTrigger("staff_rapidfire");
        count = 0;
        float damage = this.CalculateSkillValue() / 4f;
        float damageCounter = 0;
        GameObject beam = UnityEngine.Object.Instantiate(Thunder_Prefab, p.m_visEquipment.m_rightHand.transform.position, p.transform.rotation);
        while (Cond() && count <= maxTime)
        {
            damageCounter += Time.deltaTime;
            if (damageCounter >= 0.25f)
            {
                if (!TryUseCost())
                    break; 
            }

            Vector3 target = Utils.GetPerfectEyePosition() + p.GetLookDir() * 100f; 
            bool castHit = Physics.Raycast(Utils.GetPerfectEyePosition(), p.GetLookDir(), out var raycast,
                _definition.MaxLvlValue.Value + 50f, Script_Layermask2);
            if (castHit && raycast.collider && Vector3.Distance(raycast.point, p.transform.position) <= 30f)
            {
                target = raycast.point;
                Vector3 dir = (target - beam.transform.position).normalized;
                beam.transform.rotation = Quaternion.LookRotation(dir);
                beam.transform.position = p.m_visEquipment.m_rightHand.transform.position;
                beam.transform.localScale = new Vector3(1f, 1f, Vector3.Distance(target, beam.transform.position));
                if (damageCounter >= 0.25f)
                {
                    UnityEngine.Object.Instantiate(Thunder_Explosion, target, Quaternion.identity);
                    Collider[] array = Physics.OverlapSphere(target, 3f, Script_Layermask2, QueryTriggerInteraction.UseGlobal);
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
                                hit.m_damage.m_frost = damage;
                                hit.m_point = collider.ClosestPoint(target);
                                hit.m_ranged = true;
                                hit.SetAttacker(Player.m_localPlayer);
                                component.DamageMH(hit);
                            }
                        }
                    }
                }
            }
            else
            {
                Vector3 dir = (target - beam.transform.position).normalized;
                beam.transform.rotation = Quaternion.LookRotation(dir);
                beam.transform.position = p.m_visEquipment.m_rightHand.transform.position;
                beam.transform.localScale = new Vector3(1f, 1f, maxDistance);
            }

            Vector3 rot = (target - p.transform.position).normalized;
            rot.y = 0;
            p.transform.rotation = Quaternion.LookRotation(rot);
            count += Time.deltaTime;
            if (damageCounter >= 0.25f) damageCounter = 0;
            yield return null;
        }

        p.m_zanim.SetTrigger("attack_abort");
        if (beam) ZNetScene.instance.Destroy(beam.gameObject);
        SkillChargeUI.RemoveCharge(this);
        StartCooldown(this.CalculateSkillCooldown());
    }


    public override bool CanExecute()
    {
        return !Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Chargable, AoE, Beam, Damage</color>";
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

        builder.AppendLine($"Damage (Per Second): <color=cyan>Frost  {Math.Round(currentValue, 1)}</color>");
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
            builder.AppendLine($"Damage (Per Second): <color=cyan>Frost  {Math.Round(nextValue, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color></color>");
            builder.AppendLine(
                $"Cooldown: {Math.Round(nextCooldown, 1)} <color=green>({(roundedCooldownDiff > 0 ? "+" : "")}{roundedCooldownDiff})</color>");
            builder.AppendLine(
                $"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }

        
        return builder.ToString();
    }

    public override Class PreferableClass => Class.Mage;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => Color.cyan;
}