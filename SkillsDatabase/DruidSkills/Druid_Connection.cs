using System.Text;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Interfaces;
using MagicHeim.SkillsDatabase.GlobalMechanics;
using Logger = MagicHeim_Logger.Logger;
using Random = UnityEngine.Random;

namespace MagicHeim.SkillsDatabase.MageSkills;

public sealed class Druid_Connection : MH_Skill
{
    private static GameObject Prefab;
    private static GameObject LR;
    private static Sprite CachedSprite;

    public Druid_Connection()
    {
        _definition._InternalName = "Druid_Connection";
        _definition.Name = "$mh_druid_connection";
        _definition.Description = "$mh_druid_connection_desc";

        _definition.AnimationTime = 0.3f;
        _definition.Animation = ClassAnimationReplace.MH_AnimationNames[ClassAnimationReplace.MH_Animation.MageSummon];

        _definition.MinLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Value", 0.01f,
            "Healing amount (Min Lvl)");
        
        _definition.MaxLvlValue = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Value", 0.08f,
            "Healing amount (Max Lvl)");
        
        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MIN Lvl Manacost", 10f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            $"MAX Lvl Manacost", 2f,
            "Manacost amount (Max Lvl)");

        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Max Level", 10,
            "Max Skill Level");

        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            $"Required Level To Learn",
            1, "Required Level");
        
        
        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            $"Leveling Step", 3,
            "Leveling Step");
        

        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_Connection_Icon");
        CachedSprite = _definition.Icon;
        _definition.Video = "https://kg.sayless.eu/skills/MH_Druid_Connection.mp4";
        Prefab = MagicHeim.asset.LoadAsset<GameObject>("Druid_Connection_Prefab");
        LR = MagicHeim.asset.LoadAsset<GameObject>("Druid_Connection_LR");
        LR.AddComponent<ConnectionLineRenderer>();

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
            __instance.m_namedPrefabs[LR.name.GetStableHashCode()] = LR;
        }
    }

    public static int Script_Layermask = LayerMask.GetMask("character", "character_noenv", "character_net", "character_ghost", "piece", "piece_nonsolid", "terrain");
    
    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        Player p = Player.m_localPlayer;
        if (Toggled)
        {
            Toggled = false;
            return; 
        }
        p.m_collider.enabled = false;
        bool castHit = Physics.Raycast(GameCamera.instance.transform.position, p.GetLookDir(), out var raycast, 40f, Script_Layermask);
        p.m_collider.enabled = true;
        if (castHit && raycast.collider && raycast.collider.GetComponentInParent<Character>() is {} enemy)
        {
            if (Vector3.Distance(enemy.transform.position, p.transform.position) > 50f)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                    $"<color=#00FF00>Too</color><color=yellow> far</color>");
                p.AddEitr(this.CalculateSkillManacost());
                return;
            }
            
            if (!Toggled)
            {
                float percentHeal = this.CalculateSkillValue();
                MagicHeim._thistype.StartCoroutine(ConnectionCorout(enemy, percentHeal));
            }
            else 
            {
                Toggled = false;
            }
        }
        else
        {
            p.AddEitr(this.CalculateSkillManacost());
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                $"<color=#00FFFF>No</color><color=yellow> target</color>");
        }
    }

    private class ConnectionLineRenderer : MonoBehaviour
    {
        private ZNetView znv;
        private LineRenderer lr;

        private Character OWNER;
        private Character TARGET;

        private ZDOID targetId
        {
            get => znv.GetZDO().GetZDOID("targetId");
            set => znv.GetZDO().Set("targetId", value);
        } 
        
        private ZDOID ownerId
        {
            get => znv.GetZDO().GetZDOID("ownerId");
            set => znv.GetZDO().Set("ownerId", value);
        }
        
        public void Setup(Character ownwer, Character target)
        {
            ownerId = ownwer.GetZDOID();
            targetId = target.GetZDOID();
        } 

        private void Awake()
        {
            znv = GetComponent<ZNetView>();
            lr = GetComponentInChildren<LineRenderer>();
            lr.gameObject.SetActive(false);
        }

        private float counter = 0f;

        private void FixedUpdate()
        {
            counter -= Time.fixedDeltaTime;
            if (counter > 0f) return;
            counter = 1f;
            OWNER = ownerId.IsNone() ? null : ZNetScene.instance.FindInstance(ownerId).GetComponent<Character>();
            TARGET = targetId.IsNone() ? null : ZNetScene.instance.FindInstance(targetId).GetComponent<Character>();
        }

        private void Update()
        {
            if (!TARGET || !OWNER || TARGET.IsDead() || OWNER.IsDead())
            {
                lr.gameObject.SetActive(false);
                return;
            }
            
            lr.gameObject.SetActive(true);
            lr.SetPosition(0, OWNER.transform.position + Vector3.up * 1.3f);
            lr.SetPosition(1, TARGET.transform.position + Vector3.up);
        }
    } 
    
    private GameObject LineRenderer;
    
    private IEnumerator ConnectionCorout(Character target, float healingPercent)
    {
        var manacost = this.CalculateSkillManacost();
        if (!target.IsPlayer() && target.IsTamed()) healingPercent *= 4f;
        Toggled = true;
        float periodic = 0f;
        Player p = Player.m_localPlayer;
        if (LineRenderer)
        {
            LineRenderer.GetComponent<ZNetView>().ClaimOwnership();
            ZNetScene.instance.Destroy(LineRenderer.gameObject);
        }
        LineRenderer = UnityEngine.Object.Instantiate(LR, p.transform);
        LineRenderer.GetComponent<ConnectionLineRenderer>().Setup(p, target);
        for (;;)
        {
            var useMana = manacost * Time.deltaTime; 
            if (!Toggled || p.IsDead() || !p.HaveEitr(useMana) || p.InWater() || !target || target.IsDead() || Vector3.Distance(target.transform.position, p.transform.position) > 45f)
            {
                Toggled = false; 
                StartCooldown(3);
                if (LineRenderer)
                {
                    LineRenderer.GetComponent<ZNetView>().ClaimOwnership();
                    ZNetScene.instance.Destroy(LineRenderer.gameObject);
                }
                yield break;  
            }
            
            periodic -= Time.deltaTime;
            if (periodic <= 0)
            {
                periodic = 1f;
                target.m_seman.AddStatusEffect("SE_DruidConnection_Buff".GetStableHashCode(), true);
                var maxHP = target.GetMaxHealth();
                var healAmount = maxHP * healingPercent;
                target.Heal(healAmount);
            } 
            p.UseEitr(useMana); 
            yield return null;
        }
    }

    public class SE_DruidConnection_Buff : StatusEffect
    {
        public SE_DruidConnection_Buff()
        {
            name = "SE_DruidConnection_Buff";
            m_tooltip = "";
            m_icon = CachedSprite;
            m_name = "";
            m_ttl = 3;
            m_startEffects = new EffectList
            {
                m_effectPrefabs = new[]
                {
                    new EffectList.EffectData()
                    {
                        m_attach = true, m_enabled = true, m_inheritParentRotation = true,
                        m_inheritParentScale = true,
                        m_prefab = Prefab, m_randomRotation = false, m_scale = true
                    }
                }
            };
        }

        public override void OnDamaged(HitData hit, Character attacker)
        {
            float reduction = 0.9f;
            if (!this.m_character.IsPlayer())
            {
                reduction = 0.5f;
            }
            hit.ApplyModifier(reduction);
        }

        public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
        {
            float mult = 1.1f;
            if (!this.m_character.IsPlayer())
            {
                mult = 1.5f;
            }
            hitData.m_damage.Modify(mult);
        }
    }
    
    public static class DRUID_CONNECTION_DB
    {
        private static void Add_SE(ObjectDB odb)
        {
            if (ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0 ||
                ObjectDB.instance.GetItemPrefab("Amber") == null) return;

            if (!odb.m_StatusEffects.Find(se => se.name == "SE_DruidConnection_Buff"))
                odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_DruidConnection_Buff>());
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
        return "<color=red>Walk On Water, Toggle</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine($"\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentManacost = this.CalculateSkillManacost(forLevel);
        builder.AppendLine($"Manacost (Per Second): {Math.Round(currentManacost, 1)}");

        if (Level < maxLevel && Level > 0)
        {
            float nextManacost = this.CalculateSkillManacost(forLevel + 1);
            float manacostDiff = nextManacost - currentManacost;
            var roundedManacostDiff = Math.Round(manacostDiff, 1);

            builder.AppendLine($"\nNext Level:");
            builder.AppendLine(
                $"Manacost (Per Second): {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }


        return builder.ToString();
    }

    public override bool CanRightClickCast => true;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => new Color(0.09f, 0.05f, 0.17f);
}