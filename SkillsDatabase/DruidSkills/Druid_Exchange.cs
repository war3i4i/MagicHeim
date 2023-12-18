using System.Text;
using MagicHeim.MH_Interfaces;
using Object = UnityEngine.Object;

namespace MagicHeim.SkillsDatabase.DruidSkills;

public sealed class Druid_Exchange : MH_Skill
{
    public static int CachedKey;
    private static GameObject _Prefab;
    private static GameObject Explosion;

    public Druid_Exchange()
    {
        _definition._InternalName = "Druid_Exchange";
        _definition.Name = "$mh_druid_exchange";
        _definition.Description = "$mh_druid_exchange_desc";
        CachedKey = _definition.Key;
        
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
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_Exchange_Icon");
        _definition.Video = "https://kg.sayless.eu/skills/MH_Druid_Exchange.mp4";
        _Prefab = MagicHeim.asset.LoadAsset<GameObject>("Druid_Exchange_Prefab");
        Explosion = MagicHeim.asset.LoadAsset<GameObject>("Druid_Exchange_Explosion");
        this.InitRequiredItemFirstHalf("Wood", 10, 1.88f);
        this.InitRequiredItemSecondHalf("Coins", 10, 1.88f);
        this.InitRequiredItemFinal("MH_Tome_Mistlands", 3);
    }
 
 
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScene_Awake_Patch
    {
        static void Postfix(ZNetScene __instance)
        {
            __instance.m_namedPrefabs[_Prefab.name.GetStableHashCode()] = _Prefab;
            __instance.m_namedPrefabs[Explosion.name.GetStableHashCode()] = Explosion;
        }
    }


    public static readonly int Script_Layermask = LayerMask.GetMask("character", "character_noenv", "character_net",
        "character_ghost", "piece", "piece_nonsolid", "terrain");


    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        Player p = Player.m_localPlayer;
        float cooldown = this.CalculateSkillCooldown();
        p.m_collider.enabled = false;
        bool castHit = Physics.Raycast(GameCamera.instance.transform.position, p.GetLookDir(), out RaycastHit raycast, 70f, Script_Layermask);
        p.m_collider.enabled = true;
        if (castHit && raycast.collider && raycast.collider.GetComponentInParent<Character>() is {} enemy && enemy.m_nview.m_persistent)
        {
            if (Vector3.Distance(enemy.transform.position, p.transform.position) > 50f)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                    "<color=#00FF00>Too</color><color=yellow> far</color>");
                p.AddEitr(this.CalculateSkillManacost());
                return;
            }
            MagicHeim._thistype.StartCoroutine(SwapPlaces(enemy));
            StartCooldown(cooldown);
            
        }
        else
        {
            p.AddEitr(this.CalculateSkillManacost());
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                "<color=#00FFFF>No</color><color=yellow> target</color>");
        }
    } 
     
    [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
    public static class Druid_Exchange_RPC 
    {
        private static void Postfix(Character __instance)
        {
            if (!__instance.m_nview.IsValid()) return;
            __instance.m_nview.Register("Druid_Exchange_RPC", (long _, Vector3 vec) =>
            {
                if (!__instance.m_nview.IsOwner()) return;
                __instance.m_body.position = vec;
            });
        }
    }


    private IEnumerator SwapPlaces(Character target)
    {
        Player p = Player.m_localPlayer;
        GameObject toEnemy = Object.Instantiate(_Prefab, p.transform.position, Quaternion.identity);
        GameObject toPlayer = Object.Instantiate(_Prefab, target.transform.position, Quaternion.identity);
        if (!target.IsPlayer()) target.m_nview.ClaimOwnership();
        float counter = 0f;
        while (counter < 1f)
        {
            if (!p || p.IsDead() || !target || target.IsDead())
            {
                toEnemy.GetComponent<ZNetView>().ClaimOwnership();
                toPlayer.GetComponent<ZNetView>().ClaimOwnership();
                ZNetScene.instance.Destroy(toEnemy);
                ZNetScene.instance.Destroy(toPlayer);
                yield break;
            }
            counter += Time.deltaTime;
            float parabola = Utils.GetParabolaHeight(4f, counter, 1f);
            Vector3 toEnemyPos = Vector3.Lerp(p.transform.position + Vector3.up * 1.4f, target.transform.position + Vector3.up * 1.4f, counter);
            Vector3 toPlayerPos = Vector3.Lerp(target.transform.position + Vector3.up * 1.4f, p.transform.position + Vector3.up * 1.4f, counter);
            Vector3 direction = (toEnemyPos - p.transform.position).normalized;
            toEnemyPos += Vector3.Cross(direction, Vector3.up) * parabola;
            toPlayerPos -= Vector3.Cross(direction, Vector3.up) * parabola;
            toEnemy.transform.position = toEnemyPos;
            toPlayer.transform.position = toPlayerPos;
            yield return null;
        }
        toEnemy.GetComponent<ZNetView>().ClaimOwnership(); 
        toPlayer.GetComponent<ZNetView>().ClaimOwnership(); 
        ZNetScene.instance.Destroy(toEnemy);
        ZNetScene.instance.Destroy(toPlayer);
        
        Vector3 playerpos = p.transform.position;
        Vector3 targetpos = target.transform.position; 
        Object.Instantiate(Explosion, playerpos, Quaternion.identity);
        Object.Instantiate(Explosion, targetpos, Quaternion.identity);
        Physics.IgnoreCollision(p.m_collider, target.m_collider, true);
        p.m_nview.InvokeRPC("Druid_Exchange_RPC", targetpos);
        target.m_nview.InvokeRPC("Druid_Exchange_RPC", playerpos);
        target.m_body.position = playerpos;
        Physics.IgnoreCollision(p.m_collider, target.m_collider, false);
    } 
    
    public override bool CanExecute()
    {
        return !Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Target Enemy, Single Target, Swap Positions</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine("\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentCooldown = this.CalculateSkillCooldown(forLevel);
        float currentManacost = this.CalculateSkillManacost(forLevel);
        
        builder.AppendLine($"Cooldown: {Math.Round(currentCooldown, 1)}");
        builder.AppendLine($"Manacost: {Math.Round(currentManacost, 1)}");

        if (Level < maxLevel && Level > 0)
        {
            float nextCooldown = this.CalculateSkillCooldown(forLevel + 1);
            float nextManacost = this.CalculateSkillManacost(forLevel + 1);
            float cooldownDiff = nextCooldown - currentCooldown;
            float manacostDiff = nextManacost - currentManacost;
            
            double roundedCooldownDiff = Math.Round(cooldownDiff, 1);
            double roundedManacostDiff = Math.Round(manacostDiff, 1);

            builder.AppendLine("\nNext Level:");
            builder.AppendLine($"Cooldown: {Math.Round(nextCooldown, 1)} <color=green>({(roundedCooldownDiff > 0 ? "+" : "")}{roundedCooldownDiff})</color>");
            builder.AppendLine($"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
        }


        return builder.ToString();
    }

    public override bool CanRightClickCast => false;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => new Color(0.4f, 1f, 0.64f);
}