using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;
using UnityEngine.EventSystems;

namespace MagicHeim.UI_s;
 
public class DragUI : MonoBehaviour, IDragHandler, IEndDragHandler
{
    private static RectTransform dragRect;
    private static ConfigEntry<float> UI_X;
    private static ConfigEntry<float> UI_Y;

    public static void Default()
    {
        if(!dragRect) return;
        UI_X.Value = (float)UI_X.DefaultValue;
        UI_Y.Value = (float)UI_Y.DefaultValue;
        MagicHeim._thistype.Config.Save();
        dragRect.anchoredPosition = new Vector2(UI_X.Value, UI_Y.Value);
    }

    private void Awake()
    {
        UI_X = MagicHeim._thistype.Config.Bind("UI", "UI_X", 960f, "UI X position");
        UI_Y = MagicHeim._thistype.Config.Bind("UI", "UI_Y", 0f, "UI Y position");
        dragRect = transform.parent.GetComponent<RectTransform>();
        var configPos = new Vector2(UI_X.Value, UI_Y.Value);
        dragRect.anchoredPosition = configPos;
    }

    public void OnDrag(PointerEventData eventData)
    {
        var vec = dragRect.anchoredPosition + eventData.delta;
        Vector2 sizeDelta = dragRect.sizeDelta * dragRect.localScale + new Vector2(0, 24f * dragRect.localScale.y);
        var ScreenSize = new Vector2(Screen.width, Screen.height);
        if (vec.x < sizeDelta.x / 2f)
        {
            vec.x = sizeDelta.x / 2f;
        }

        if (vec.y < 0)
        {
            vec.y = 0;
        }

        if (vec.x > ScreenSize.x - sizeDelta.x / 2f)
        {
            vec.x = ScreenSize.x - sizeDelta.x / 2f;
        }

        if (vec.y > ScreenSize.y - sizeDelta.y)
        {
            vec.y = ScreenSize.y - sizeDelta.y;
        }

        dragRect.anchoredPosition = vec;
    }

    public void OnEndDrag(PointerEventData data)
    {
        UI_X.Value = dragRect.anchoredPosition.x;
        UI_Y.Value = dragRect.anchoredPosition.y;
        MagicHeim._thistype.Config.Save();
    }
}

public class ResizeUI : MonoBehaviour, IDragHandler, IEndDragHandler 
{
    private static RectTransform dragRect;
    private static ConfigEntry<float> UI_X; 
    private static ConfigEntry<float> UI_Y;
    public Vector3 Scale => new Vector3(dragRect.localScale.x, dragRect.localScale.y, 1f);
    
    public static void Default()
    {
        if(!dragRect) return;
        UI_X.Value = (float)UI_X.DefaultValue;
        UI_Y.Value = (float)UI_Y.DefaultValue;
        MagicHeim._thistype.Config.Save();
        dragRect.localScale = new Vector3(UI_X.Value, UI_Y.Value, 1f);
    }
    
    private void Awake()
    {
        dragRect = transform.parent.GetComponent<RectTransform>();
        UI_X = MagicHeim._thistype.Config.Bind("UI", "UI_sizeX", 1f, "UI X size");
        UI_Y = MagicHeim._thistype.Config.Bind("UI", "UI_sizeY", 1f, "UI Y size");
        dragRect.localScale = new Vector3(UI_X.Value, UI_Y.Value, 1f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        var vec = eventData.delta;
        var sizeDelta = dragRect.sizeDelta;
        vec.x = vec.x / sizeDelta.x;
        var resized = dragRect.localScale + new Vector3(vec.x, vec.x, 0);
        resized.x = Mathf.Clamp(resized.x, 0.6f, 1.5f);
        resized.y = Mathf.Clamp(resized.y, 0.6f, 1.5f);
        resized.z = 1f;
        dragRect.localScale = resized;
    }
 
    public void OnEndDrag(PointerEventData data)
    {
        UI_X.Value = dragRect.localScale.x;
        UI_Y.Value = dragRect.localScale.y;
        MagicHeim._thistype.Config.Save();
    }
}

public class PanelButton
{
    public int index;
    public Text CooldownText;
    public Image Cooldown;
    public Image Icon;
    public Text Hotkey;
    public Image Toggle;
    public MH_Skill Skill;
    public Text Manacost;
    public GameObject go;
}

public static class SkillPanelUI
{
    public enum Change
    {
        ToUpdate,
        Changed
    }

    public static Change Status = Change.ToUpdate;

    private static GameObject UI;
    private static GameObject SkillElement;
    private static Transform BottomSkillsTransform;
    private static Transform TopSkillsTransform;

    private static ConfigEntry<int> MaxSlots;
    public static ConfigEntry<bool>[] UseAltHotkey;
    public static ConfigEntry<KeyCode>[] MH_Hotkeys;
    public static ConfigEntry<KeyCode>[] MH_AdditionalHotkeys;

    public static ResizeUI Resizer;

    public static readonly List<KeyCode> DefaultHotkeys = new List<KeyCode>()
    {
        KeyCode.Alpha1,
        KeyCode.Alpha2,
        KeyCode.Alpha3,
        KeyCode.Alpha4,
        KeyCode.Alpha5,
        KeyCode.Alpha6,
        KeyCode.Alpha7,
        KeyCode.Alpha8,
        KeyCode.Alpha9,
        KeyCode.Alpha0,
        KeyCode.Keypad1,
        KeyCode.Keypad2,
        KeyCode.Keypad3,
        KeyCode.Keypad4,
        KeyCode.Keypad5,
        KeyCode.Keypad6,
        KeyCode.Keypad7,
        KeyCode.Keypad8,
        KeyCode.Keypad9,
        KeyCode.Keypad0,
        KeyCode.K,
    };

    private static MH_ClassDefinition _currentClass;

    public static bool IsVisible() => UI && UI.activeSelf;

    private static readonly Dictionary<int, PanelButton> _skillSlots = new Dictionary<int, PanelButton>();


    private static Image ExpBar;
    private static long LastEXP;


    public static void Init()
    {
        MaxSlots = MagicHeim.config("SkillPanel", "MaxSlots", 20, "Max slots in skill panel");
        MH_Hotkeys = new ConfigEntry<KeyCode>[21];
        UseAltHotkey = new ConfigEntry<bool>[20];
        MH_AdditionalHotkeys = new ConfigEntry<KeyCode>[20];
        for (int i = 0; i < 20; ++i)
        {
            MH_Hotkeys[i] = MagicHeim._thistype.Config.Bind("SkillPanel", "SkillHotkey_" + (i + 1), DefaultHotkeys[i],
                "Hotkey for skill " + (i + 1));
            UseAltHotkey[i] = MagicHeim._thistype.Config.Bind("SkillPanel", "UseAltHotkey_" + (i + 1), true,
                "Use Alt hotkey for skill " + (i + 1));
            MH_AdditionalHotkeys[i] = MagicHeim._thistype.Config.Bind("SkillPanel", "SkillHotkeyAlt_" + (i + 1), KeyCode.LeftAlt,
                "Alt hotkey for skill " + (i + 1));
        }
 
        MH_Hotkeys[20] = MagicHeim._thistype.Config.Bind("SkillPanel", "Open Skillbook", DefaultHotkeys[20],
            "Hotkey for SkillBook");

        UI = UnityEngine.Object.Instantiate(MagicHeim.asset.LoadAsset<GameObject>("SkillPanelUI"));
        Localization.instance.Localize(UI.transform);
        UI.name = "SkillPanelUI";
        UnityEngine.Object.DontDestroyOnLoad(UI);
        UI.SetActive(false);
        SkillElement = MagicHeim.asset.LoadAsset<GameObject>("SkillPanelSkill");
        BottomSkillsTransform = UI.transform.Find("Canvas/Background/skills_bottomrow");
        TopSkillsTransform = UI.transform.Find("Canvas/Background/skills_toprow");
        Default();
        UI.transform.Find("Canvas/Background/move").gameObject.AddComponent<DragUI>();
        Resizer = UI.transform.Find("Canvas/Background/resize").gameObject.AddComponent<ResizeUI>();
        ExpBar = UI.transform.Find("Canvas/Background/ExpBar/Image").GetComponent<Image>();
        LastEXP = -1;
    }

    private static Coroutine _coroutine;

    private static void LoadData(string data)
    {
        if (_coroutine != null) Player.m_localPlayer.StopCoroutine(_coroutine);
        if (_currentClass == null)
        {
            Hide();
            return;
        }

        ExpBar.color = _currentClass.GetColor;
        _skillSlots.Clear();
        for (var i = 0; i < Mathf.Min(20, MaxSlots.Value); i++)
        {
            PanelButton button = new();
            Transform t = i < 10 ? BottomSkillsTransform : TopSkillsTransform;
            var gameObject = UnityEngine.Object.Instantiate(SkillElement, t);
            int i1 = i;
            gameObject.GetComponent<UIInputHandler>().m_onLeftClick = _ => DragSkill(i1);
            gameObject.GetComponent<UIInputHandler>().m_onRightClick = _ => TryUseSkill(i1);
            button.go = gameObject;
            button.index = i;
            button.Icon = gameObject.GetComponent<Image>();
            button.Cooldown = gameObject.transform.Find("CooldownImage").GetComponent<Image>();
            button.CooldownText = gameObject.transform.Find("CooldownText").GetComponent<Text>();
            button.Hotkey = gameObject.transform.Find("Hotkey").GetComponent<Text>();
            button.Toggle = gameObject.transform.Find("Cancel").GetComponent<Image>();
            button.Manacost = gameObject.transform.Find("Cost").GetComponent<Text>();
            gameObject.name = $"Skill{i}";
            _skillSlots.Add(i, button);
        }

        Status = Change.ToUpdate;
        if (!string.IsNullOrEmpty(data))
        {
            var skills = data.Split(';');
            for (var i = 0; i < skills.Length; i++)
            {
                if (i >= _skillSlots.Count)
                    break;
                int.TryParse(skills[i], out var skillID);
                SetSkill(i, skillID);
            } 
        }

        _coroutine = Player.m_localPlayer.StartCoroutine(Global_Routine());
    }

    private static void TryUseSkill(int index )
    {
        if (index > _skillSlots.Count) return;
        if (_skillSlots[index].Skill is not { CanRightClickCast: true, Level: > 0 }) return;
        SkillCastHelper.CastSkill(_skillSlots[index].Skill, skipInput: true);
    }
 
    private static void DragSkill(int index)
    {
        ClassSelectionUI.AUsrc.Play();
        if (index > _skillSlots.Count) return;
        if (Time.unscaledTime - SkillDrag.LastDragTime <= 0.2f)
        {
            var skill = SkillDrag.LastDragSkill;
            if (skill == null) return;
            SetSkill(index, skill.Key);
            SkillDrag.LastDragSkill = null;
            SkillDrag.LastDragTime = 0;
        }
        else
        {
            SkillDrag.StartDrag(_skillSlots[index].Skill, index);
        }
    }

    public static void Reset()
    {
        foreach (var skillSlot in _skillSlots)
        {
            skillSlot.Value.Skill = null;
        }

        Status = Change.ToUpdate;
    }

    public static void SetSkill(int slot, int skill)
    {
        if (slot > _skillSlots.Count)
            return;
        _skillSlots[slot].Skill = ClassManager.CurrentClassDef.GetSkills().TryGetValue(skill, out var skillDef)
            ? skillDef
            : null;
        Status = Change.ToUpdate;
    }


    private static IEnumerator Global_Routine()
    {
        while (true)
        {
            //exp bar set
            var currentExp = ClassManager.EXP;
            if (LastEXP != currentExp)
            {
                LastEXP = currentExp;
                ExpBar.fillAmount = (float)currentExp / ClassManager.GetExpForLevel(ClassManager.Level);
            }

            //skill usage
            foreach (var button in _skillSlots)
            {
                if (UseAltHotkey[button.Value.index].Value)
                {
                    if (!Input.GetKey(MH_AdditionalHotkeys[button.Value.index].Value) || !Input.GetKeyDown(MH_Hotkeys[button.Key].Value)) continue;
                    if (button.Value.Skill is { Level: > 0 })
                    {
                        SkillCastHelper.CastSkill(button.Value.Skill,
                            cond: () => Input.GetKey(MH_AdditionalHotkeys[button.Value.index].Value) && Input.GetKey(MH_Hotkeys[button.Key].Value));
                    }
                }
                else
                {
                    if (!Input.GetKeyDown(MH_Hotkeys[button.Key].Value)) continue;
                    if (button.Value.Skill is { Level: > 0 })
                    {
                        SkillCastHelper.CastSkill(button.Value.Skill,
                            cond: () => Input.GetKey(MH_Hotkeys[button.Key].Value));
                    }
                }
            }

            //main update routine
            restart:
            foreach (var button in _skillSlots)
            {
                if (button.Value.Skill == null)
                {
                    if (Status == Change.ToUpdate)
                    {
                        button.Value.Icon.enabled = false;
                        button.Value.Cooldown.fillAmount = 0;
                        button.Value.CooldownText.text = "";
                        button.Value.Hotkey.text = MH_Hotkeys[button.Value.index].Value.ToString()
                            .Replace("Alpha", "").Replace("Mouse", "M");
                        button.Value.Toggle.gameObject.SetActive(false);
                        button.Value.Manacost.gameObject.SetActive(false);
                    }
                }
                else
                {
                    if (button.Value.Skill.Level <= 0)
                    {
                        SetSkill(button.Value.index, -1);
                        goto restart;
                    }

                    if (Status == Change.ToUpdate)
                    {
                        button.Value.Icon.enabled = true;
                        button.Value.Manacost.gameObject.SetActive(true);
                        button.Value.Manacost.color = button.Value.Skill._costType switch
                        {
                            MH_Skill.CostType.Eitr => new Color(1f, 0f, 0.87f),
                            MH_Skill.CostType.Stamina => Color.yellow,
                            MH_Skill.CostType.Health => Color.red,
                            _ => Color.cyan
                        };
                        button.Value.Manacost.text = ((int)button.Value.Skill.CalculateSkillManacost()).ToString();
                        button.Value.Icon.sprite = button.Value.Skill.Icon;
                        button.Value.Hotkey.text = MH_Hotkeys[button.Value.index].Value.ToString()
                            .Replace("Alpha", "").Replace("Mouse", "M");
                    }

                    button.Value.Toggle.gameObject.SetActive(button.Value.Skill.Toggled);
                    if (button.Value.Skill.GetCooldown() > 0)
                    {
                        button.Value.Cooldown.fillAmount =
                            button.Value.Skill.GetCooldown() / button.Value.Skill.GetMaxCooldown();
                        button.Value.CooldownText.text = ((int)(button.Value.Skill.GetCooldown())).ToString();
                    }
                    else
                    { 
                        button.Value.Cooldown.fillAmount = 0;
                        button.Value.CooldownText.text = "";
                    }
                }
            }

            Status = Change.Changed;
            yield return null;
        }
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    static class FejdStartup_Awake_Patch
    {
        static void Postfix(FejdStartup __instance)
        {
            Hide();
        }
    }

    public static string Serialize()
    {
        if (_currentClass == null) return "-";
        string data = "";
        foreach (var skill in _skillSlots)
        {
            data += skill.Value.Skill == null ? "-;" : skill.Value.Skill.Key + ";";
        }

        data = data.TrimEnd(';');
        return data;
    }

    private static void Default()
    {
        _skillSlots.Clear();
        foreach (Transform transform in BottomSkillsTransform)
        {
            UnityEngine.Object.Destroy(transform.gameObject);
        }
        foreach (Transform transform in TopSkillsTransform)
        {
            UnityEngine.Object.Destroy(transform.gameObject);
        }
    }


    public static void Show(MH_ClassDefinition definition, string data = "")
    {
        _currentClass = definition;
        Default();
        UI.SetActive(true);
        LoadData(data);
    }

    public static void Hide()
    {
        Default();
        UI.SetActive(false);
    }

    public static void ShowVisualOnly()
    {
        UI.SetActive(true);
    }
    
    public static void HideVisualOnly()
    {
        UI.SetActive(false);
    }
    
    [HarmonyPatch(typeof(Hud),nameof(Hud.Update))]
    static class Hud_Update_Patch
    {
        static void HideUImethod()
        {
            if(ClassManager.CurrentClass == Class.None) return;
            if (Hud.instance.m_userHidden)
            {
                HideVisualOnly();
            }
            else
            {
                ShowVisualOnly();
            }
        }
        
        //hide UI on Ctrl + F3 button. IL offest (51) may be changed in future, aware
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> HideUI(IEnumerable<CodeInstruction> code)
        {
            List<CodeInstruction> list = new(code);
            list.Insert(146, CodeInstruction.Call(()=> HideUImethod()));
            return list.AsEnumerable();
        }
    }
}