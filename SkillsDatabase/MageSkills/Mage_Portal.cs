using System.Text;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Classes;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;
using MagicHeim.UI_s;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Mage_Portal : MH_Skill
{
    private static GameObject Portal_Prefab;
    private static GameObject Teleport_RangeShowup;
    private static GameObject Teleport_TargetPoint;

    public Mage_Portal()
    {
        _definition._InternalName = "Mage_Portal";
        _definition.Name = "$mh_mage_portal";
        _definition.Description = "$mh_mage_portal_desc";

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Distance", 30f,
            "Value amount (Min Lvl)");
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Distance", 60f,
            "Value amount (Max Lvl)");

        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Manacost", 40f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Manacost", 40f,
            "Manacost amount (Max Lvl)");

        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}", 
            $"MIN Lvl Cooldown", 30f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Cooldown", 18f,
            "Cooldown amount (Max Lvl)");


        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Max Level", 10,
            "Max Skill Level");
        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Required Level To Learn",
            60, "Required Level");

        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            $"Leveling Step", 2,
            "Leveling Step");
        
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Mage_Portal_Icon");
        _definition.Video = "https://kg-dev.xyz/skills/MH_Mage_Portal.mp4";
        Portal_Prefab = MagicHeim.asset.LoadAsset<GameObject>("Mage_Portal_Prefab");
        Portal_Prefab.AddComponent<PortalComponent>();
        Teleport_RangeShowup = MagicHeim.asset.LoadAsset<GameObject>("Mage_AreaShowup");
        Teleport_TargetPoint = MagicHeim.asset.LoadAsset<GameObject>("Mage_TargetShowup");
        
        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScene_Awake_Patch
    {
        static void Postfix(ZNetScene __instance) 
        {
            __instance.m_namedPrefabs[Portal_Prefab.name.GetStableHashCode()] = Portal_Prefab;
        }
    }

    public class PortalComponent : MonoBehaviour, Interactable, Hoverable
    {
        private ZNetView _znv;

        private void Awake()
        {
            _znv = GetComponent<ZNetView>();
        }

        public void SetTarget(Vector3 target) 
        { 
            _znv.m_zdo.Set("target", target);
        } 

        private void FixedUpdate()
        {
            Quaternion rot =  Quaternion.LookRotation(Camera.main.transform.forward);
            rot.x = 0;
            rot.z = 0;
            transform.rotation = rot;
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            Player.m_localPlayer.m_lastGroundTouch = 0f;
            Player.m_localPlayer.m_maxAirAltitude = 0f;
            Player.m_localPlayer.transform.position = _znv.m_zdo.GetVec3("target", Player.m_localPlayer.transform.position);
            Instantiate(Mage_Teleport.Teleport_Explosion, Player.m_localPlayer.transform.position, Quaternion.identity);
            Player.m_localPlayer.m_lastGroundTouch = 0f;
            Player.m_localPlayer.m_maxAirAltitude = 0f;
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        { 
            return false;
        }

        public string GetHoverText()
        {
            return Localization.instance.Localize($"[<color=yellow><b>$KEY_Use</b></color>] Use Portal");
        }

        public string GetHoverName()
        {
            return "Portal";
        }
    }
    
    
    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        MagicHeim._thistype.StartCoroutine(Charge(Cond));
    }

    private static readonly int JumpMask =
        LayerMask.GetMask("terrain", "Default", "piece", "piece_nonsolid", "static_solid");

    private static Vector3 NON_Vector = new Vector3(-100000, 0, 0);
    
    private IEnumerator Charge(Func<bool> Cond)
    {
        bool cancel = false;
        SkillChargeUI.ShowCharge(this);
        Player p = Player.m_localPlayer;
        float maxDistance = this.CalculateSkillValue();
        GameObject rangeShowup =
            UnityEngine.Object.Instantiate(Teleport_RangeShowup, p.transform.position, Quaternion.identity);
        GameObject targetPoint =
            UnityEngine.Object.Instantiate(Teleport_TargetPoint, p.transform.position, Quaternion.identity);
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
            bool castHit = Physics.Raycast(Utils.GetPerfectEyePosition(), p.GetLookDir(), out var raycast,
                _definition.MaxLvlValue.Value + 10f,
                JumpMask);
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
        if (!cancel && p && !p.IsDead() && target != NON_Vector &&
            global::Utils.DistanceXZ(target, p.transform.position) <= maxDistance)
        {
            Vector3 rot = (target - p.transform.position).normalized;
            rot.y = 0;
            p.transform.rotation = Quaternion.LookRotation(rot);
            StartCooldown(this.CalculateSkillCooldown());
            Vector3 dir = (target - p.transform.position).normalized;
            var fromPortal = UnityEngine.Object.Instantiate(Portal_Prefab, p.transform.position + dir * 2f, Quaternion.identity);
            var toPortal = UnityEngine.Object.Instantiate(Portal_Prefab, target, Quaternion.identity);
            fromPortal.GetComponent<PortalComponent>().SetTarget(target + dir * 2f + Vector3.up * 0.2f);
            toPortal.GetComponent<PortalComponent>().SetTarget(p.transform.position);
            p.m_zanim.SetTrigger("staff_summon");
        }
        else
        {
            if (!cancel)
            {
                p.AddEitr(this.CalculateSkillManacost());
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "<color=cyan>Too far</color>");
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
        return "<color=red>Precast, Move Position, Portal Creation</color>";
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

        builder.AppendLine($"Distance: {Math.Round(currentValue, 1)}");
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
            builder.AppendLine($"Distance: {Math.Round(nextValue, 1)} <color=green>({(roundedValueDiff > 0 ? "+" : "")}{roundedValueDiff})</color>");
            builder.AppendLine($"Cooldown: {Math.Round(nextCooldown, 1)} <color=green>({(roundedCooldownDiff > 0 ? "+" : "")}{roundedCooldownDiff})</color>");
            builder.AppendLine($"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }

        

        return builder.ToString();
    }

    public override Class PreferableClass => Class.Mage;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => new Color(0.18f, 0.72f, 1f);
}