﻿using System.Text;
using JetBrains.Annotations;
using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Interfaces;

namespace MagicHeim.SkillsDatabase.DruidSkills;

public sealed class Druid_Eagle : MH_Skill
{
    public static int CachedKey;
    private static GameObject Eagle_Prefab;
    private static GameObject Eagle_Explosion;
    private static GameObject RevealAbility;
    private static GameObject RevealAbility_Character;

    public Druid_Eagle()
    {
        _definition._InternalName = "Druid_Eagle";
        _definition.Name = "$mh_druid_eagle";
        _definition.Description = "$mh_druid_eagle_desc";
        CachedKey = _definition.Key;
        _definition.MinLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Manacost", 10f,
            "Manacost amount (Min Lvl)");
        _definition.MaxLvlManacost = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Manacost", 5f,
            "Manacost amount (Max Lvl)");

        _definition.MinLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            "MIN Lvl Cooldown", 12f,
            "Cooldown amount (Min Lvl)");
        _definition.MaxLvlCooldown = MagicHeim.config($"{_definition._InternalName}",
            "MAX Lvl Cooldown", 3f,
            "Cooldown amount (Max Lvl)");

        _definition.MaxLevel = MagicHeim.config($"{_definition._InternalName}",
            "Max Level", 5,
            "Max Skill Level");

        _definition.RequiredLevel = MagicHeim.config($"{_definition._InternalName}",
            "Required Level To Learn",
            40, "Required Level");

        _definition.LevelingStep = MagicHeim.config($"{_definition._InternalName}",
            "Leveling Step", 5,
            "Leveling Step");
        
        _definition.Icon = MagicHeim.asset.LoadAsset<Sprite>("Druid_Eagle_Icon");
        _definition.Video = "https://kg.sayless.eu/skills/MH_Druid_Eagle.mp4";
        Eagle_Prefab = MagicHeim.asset_addition.LoadAsset<GameObject>("DRAGONRIDE");
        
        
        Eagle_Explosion = MagicHeim.asset.LoadAsset<GameObject>("Druid_Eagle_Explosion");
        RevealAbility = MagicHeim.asset.LoadAsset<GameObject>("Druid_Eagle_Reveal");
        RevealAbility_Character = MagicHeim.asset.LoadAsset<GameObject>("Druid_Eagle_Reveal_Icon");
        RevealAbility.AddComponent<RevealController>();
        RevealAbility_Character.AddComponent<BillboardObject>();
    }

    

    
    private static readonly Dictionary<string, GameObject> ObjectToTrophy = new();
    
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScene_Awake_Patch
    {
        [UsedImplicitly]
        static void Postfix(ZNetScene __instance)
        {
            __instance.m_namedPrefabs[Eagle_Prefab.name.GetStableHashCode()] = Eagle_Prefab;
            __instance.m_namedPrefabs[Eagle_Explosion.name.GetStableHashCode()] = Eagle_Explosion;
            __instance.m_namedPrefabs[RevealAbility.name.GetStableHashCode()] = RevealAbility;


            Eagle_Prefab.GetComponentInChildren<SkinnedMeshRenderer>(true).materials = ZNetScene.instance.GetPrefab("Dragon")
                .GetComponentInChildren<SkinnedMeshRenderer>(true).materials;

            Eagle_Prefab.transform.Find("Dragon").localScale = Vector3.one * 0.15f;
            
            foreach (CharacterDrop prefab in __instance.m_prefabs.Where(c => c.GetComponent<CharacterDrop>()).Select(c => c.GetComponent<CharacterDrop>()))
            {
                if (prefab.m_drops.Count <= 0) continue;
                foreach (CharacterDrop.Drop drop in prefab.m_drops)
                {
                    if (drop.m_prefab.GetComponent<ItemDrop>() is { } item && item.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy)
                    {
                        ObjectToTrophy[prefab.name] = drop.m_prefab;
                    }
                }
            }
        }
    }
    
    private class BillboardObject : MonoBehaviour
    {
        private void FixedUpdate()
        {
            transform.rotation = Quaternion.LookRotation(GameCamera.instance.transform.forward);
        }
    }
    
     
    private class RevealController : MonoBehaviour
    {
        private ZNetView znv;
        private readonly HashSet<Character> list = new();

        private void Awake()
        { 
            znv = GetComponent<ZNetView>();
        }

        private float counter;
        private void Update()
        {
            counter += Time.deltaTime;
            float scale = counter * 150f; 
            transform.localScale = new Vector3(scale, scale, scale);
            if (counter >= 2f)
            {
                znv.ClaimOwnership(); 
                ZNetScene.instance.Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!znv.IsOwner()) return;
            if (other.TryGetComponent(out Character c))
            {
                if (list.Add(c) && Utils.IsEnemy(c) && c.m_nview.IsValid())
                {
                    string goName = global::Utils.GetPrefabName(c.gameObject);
                    GameObject go = ObjectToTrophy.TryGetValue(goName, out GameObject trophyGo) ? trophyGo : ZNetScene.instance.GetPrefab("Trophy" + goName);
                    if (c.transform.Find("RevealIcon"))
                        Destroy(c.transform.Find("RevealIcon").gameObject);
                    
                    GameObject reveal = Instantiate(RevealAbility_Character, c.transform);
                    reveal.name = "RevealIcon";
                    if (!go || go.GetComponent<ItemDrop>() is not { } trophy)
                    {
                        reveal.transform.GetChild(0).gameObject.SetActive(false);
                        return;
                    }
                    float height = c.m_collider.height - 1.7f;
                    reveal.transform.GetChild(0).transform.localPosition += Vector3.up * height; 
                    SpriteRenderer sr = reveal.transform.GetChild(0).GetComponent<SpriteRenderer>();
                    sr.sprite = trophy.m_itemData.m_shared.m_icons[0];
                    sr.size = new Vector2(6f, 6f);
                }
            }
        }
    }

    public override void Execute(Func<bool> Cond)
    {
        if (!Player.m_localPlayer) return;
        Player p = Player.m_localPlayer;
        float manacost = this.CalculateSkillManacost();
        if (!Toggled)
        {
            if (p.HaveEitr(manacost * 3f))
            {
                MagicHeim._thistype.StartCoroutine(EagleForm());
            }
        }
        else
        {
            Toggled = false;
        }
    }


    /*private IEnumerator EagleForm()
    {
        float manacost = this.CalculateSkillManacost(); 
        Toggled = true;
        Player p = Player.m_localPlayer; 
        float stamina = p.GetStamina();
        p.m_nview.InvokeRPC(ZNetView.Everybody, "MH_HideCharacter", true);
        p.m_nview.m_zdo.Set("MH_HideCharacter", true); 
        p.m_zanim.SetTrigger("emote_stop");
        p.m_collider.isTrigger = true;
        p.m_body.useGravity = false;
        p.m_body.position += Vector3.up;
        Vector3 position = p.transform.position;
        UnityEngine.Object.Instantiate(Eagle_Explosion, position + Vector3.up, Quaternion.identity);
        GameObject go = UnityEngine.Object.Instantiate(Eagle_Prefab, position, Quaternion.LookRotation(GameCamera.instance.transform.forward));
        Rigidbody rbody = go.GetComponent<Rigidbody>();
        go.transform.position = position;
        for (;;)
        {
            if(!p) yield break; 
            Vector3 pos = p.m_body.position;
            Vector3 fwd = GameCamera.instance.transform.forward;
            float mod = ZInput.GetButton("Run") || ZInput.GetButton("JoyRun") ? 12f : 8f;
            if (ZInput.GetButton("Forward") || ZInput.GetJoyLeftStickY() < 0)
                pos += fwd * (mod * Time.deltaTime);
            if (ZInput.GetButton("Backward") || ZInput.GetJoyLeftStickY() > 0)
                pos -= fwd * (mod * Time.deltaTime);
            Quaternion rot = Quaternion.LookRotation(fwd);
            if (ZInput.GetButton("Crouch")) 
            {
                rot = Quaternion.LookRotation(-Vector3.up + fwd * 2f);
                pos.y -= mod * Time.deltaTime;
            }
            else
            if (ZInput.GetButton("Jump")) 
            {
                rot = Quaternion.LookRotation(Vector3.up + fwd * 2f);
                pos.y += mod * Time.deltaTime;
            }
 
            if (Input.GetKeyDown(KeyCode.Mouse1) && !Player.m_localPlayer.TakeInput()) 
            {
                UnityEngine.Object.Instantiate(RevealAbility, p.transform.position, Quaternion.identity);
            }

            pos.y = pos.y < ZoneSystem.instance.GetGroundHeight(pos) + 1
                ? ZoneSystem.instance.GetGroundHeight(pos) + 1
                : pos.y;
            if (pos.y < 31) pos.y = 31;
            p.m_body.position = pos;
            Transform transform = rbody.transform;
            transform.position = pos;
            transform.rotation = rot; 
            p.m_maxAirAltitude = 0f;
            p.m_lastGroundTouch = 0f;
            rbody.angularVelocity = Vector3.zero;
            rbody.velocity = Vector3.zero;
            float useMana = manacost * Time.deltaTime;
            if (!p.HaveEitr(useMana) || !Toggled || p.IsDead())
            {
                Toggled = false;
                p.m_collider.isTrigger = false;
                p.m_nview.m_zdo.Set("MH_HideCharacter", false);
                p.m_body.velocity = Vector3.zero;
                p.m_body.useGravity = true;
                p.m_lastGroundTouch = 0f;
                p.m_maxAirAltitude = 0f;
                p.m_nview.InvokeRPC(ZNetView.Everybody, "MH_HideCharacter", false);
                UnityEngine.Object.Instantiate(Eagle_Explosion, p.transform.position, Quaternion.identity);
                if (go) ZNetScene.instance.Destroy(go.gameObject);
                StartCooldown(this.CalculateSkillCooldown());
                yield break;
            }

            p.UseEitr(useMana);
            p.m_stamina = stamina;
            if (p.m_stamina > p.m_maxStamina) p.m_stamina = p.m_maxStamina;
            yield return null;
        }
    }*/
    
    private DateTime LastRevewal = DateTime.Now;
    
        private IEnumerator EagleForm()
    {
        var manacost = this.CalculateSkillManacost(); 
        Toggled = true;
        Player p = Player.m_localPlayer; 
        float stamina = p.GetStamina();
        p.m_nview.InvokeRPC(ZNetView.Everybody, "MH_HideCharacter", true);
        p.m_nview.m_zdo.Set("MH_HideCharacter", true); 
        p.m_zanim.SetTrigger("emote_stop");
        p.m_collider.isTrigger = true;
        UnityEngine.Object.Instantiate(Eagle_Explosion, p.transform.position + Vector3.up, Quaternion.identity);
        GameObject go;
        go = UnityEngine.Object.Instantiate(Eagle_Prefab, p.transform.position + Vector3.up,
            Quaternion.LookRotation(GameCamera.instance.transform.forward));
        var rbody = go.GetComponent<Rigidbody>();
        go.transform.position = p.transform.position + Vector3.up;
        for (;;)
        {
            if(!p) yield break;
            rbody.angularVelocity = Vector3.zero;
            rbody.velocity = Vector3.zero;
            go.transform.rotation = Quaternion.LookRotation(GameCamera.instance.transform.forward);
            var pos = go.transform.position;
            var fwd = GameCamera.instance.transform.forward;
            float mod = ZInput.GetButton("Run") || ZInput.GetButton("JoyRun") ? 10f : 6f;
            if (ZInput.GetButton("Jump"))
            {
                go.transform.rotation = Quaternion.LookRotation(go.transform.up + go.transform.forward * 2f);
                pos.y += mod * Time.deltaTime;
            }
            else
            if (ZInput.GetButton("Crouch"))
            {
                go.transform.rotation = Quaternion.LookRotation(-go.transform.up + go.transform.forward * 2f);
                pos.y -= mod * Time.deltaTime;
            }
            else
            if (ZInput.GetButton("Forward") || ZInput.GetJoyLeftStickY() < 0)
            {
                pos += fwd * (mod * Time.deltaTime);
            }
            else if (ZInput.GetButton("Backward") || ZInput.GetJoyLeftStickY() > 0)
            {
                pos -= fwd * (mod * Time.deltaTime); 
            }
          
            
            
            if (Input.GetKeyDown(KeyCode.Mouse1) && Player.m_localPlayer.TakeInput())
            {
                int secondsDiff = (DateTime.Now - LastRevewal).Seconds;
                float groundHeight = ZoneSystem.instance.GetGroundHeight(p.transform.position);
                float heightDiff = p.transform.position.y - groundHeight;
                if (heightDiff < 50)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"Go {50 - (int)heightDiff} meters higher");
                }
                else
                {
                    if (secondsDiff >= 30)
                    {
                        UnityEngine.Object.Instantiate(RevealAbility, p.transform.position, Quaternion.identity);
                        LastRevewal = DateTime.Now;
                    }
                    else
                    {
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"You can use this ability in {30 - secondsDiff} seconds");
                    }
                }
            }

            pos.y = pos.y < ZoneSystem.instance.GetGroundHeight(pos) + 1 
                ? ZoneSystem.instance.GetGroundHeight(pos) + 1 
                : pos.y;
            if (pos.y < 31) pos.y = 31;
            go.transform.position = pos;
            p.m_body.position = pos;
            p.transform.rotation = go.transform.rotation;
            p.m_maxAirAltitude = 0f;
            p.m_lastGroundTouch = 0f;
            var useMana = manacost * Time.deltaTime;
            if (!p.HaveEitr(useMana) || !Toggled || p.IsDead())
            {
                Toggled = false;
                p.m_collider.isTrigger = false;
                p.m_nview.m_zdo.Set("MH_HideCharacter", false);
                p.m_body.velocity = Vector3.zero;
                p.m_body.useGravity = true;
                p.m_lastGroundTouch = 0f;
                p.m_maxAirAltitude = 0f;
                p.m_nview.InvokeRPC(ZNetView.Everybody, "MH_HideCharacter", false);
                UnityEngine.Object.Instantiate(Eagle_Explosion, p.transform.position, Quaternion.identity);
                if (go) ZNetScene.instance.Destroy(go.gameObject);
                StartCooldown(this.CalculateSkillCooldown());
                yield break;
            }

            p.UseEitr(useMana);
            p.m_stamina = stamina;
            if (p.m_stamina > p.m_maxStamina) p.m_stamina = p.m_maxStamina;
            yield return null;
        }
    }

    public override bool CanExecute()
    {
        return !Utils.InWater();
    }

    public override string GetSpecialTags()
    {
        return "<color=red>Transform, Fly, Toggle</color>";
    }

    public override string BuildDescription()
    {
        StringBuilder builder = new();
        builder.AppendLine(Localization.instance.Localize(Description));
        builder.AppendLine("\n");

        int maxLevel = MaxLevel;
        int forLevel = Level > 0 ? Level : 1;
        float currentManacost = this.CalculateSkillManacost(forLevel);
        float currentCooldown = this.CalculateSkillCooldown(forLevel);
        builder.AppendLine($"Manacost: {Math.Round(currentManacost, 1)}");
        builder.AppendLine($"Cooldown: {Math.Round(currentCooldown, 1)}");

        if (Level < maxLevel && Level > 0)
        {
            float nextManacost = this.CalculateSkillManacost(forLevel + 1);
            float nextCooldown = this.CalculateSkillCooldown(forLevel + 1);
            float manacostDiff = nextManacost - currentManacost;
            float cooldownDiff = nextCooldown - currentCooldown;
            double roundedManacostDiff = Math.Round(manacostDiff, 1);
            double roundedCooldownDiff = Math.Round(cooldownDiff, 1);

            builder.AppendLine("\nNext Level:");
            builder.AppendLine($"Manacost: {Math.Round(nextManacost, 1)} <color=green>({(roundedManacostDiff > 0 ? "+" : "")}{roundedManacostDiff})</color>");
            builder.AppendLine($"Cooldown: {Math.Round(nextCooldown, 1)} <color=green>({(roundedCooldownDiff > 0 ? "+" : "")}{roundedCooldownDiff})</color>");
        }


        return builder.ToString();
    }

    public override bool CanRightClickCast => true;
    public override bool IsPassive => false;
    public override CostType _costType => CostType.Eitr;
    public override Color SkillColor => Color.green;
}