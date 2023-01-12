using MagicHeim.AnimationHelpers;
using MagicHeim.MH_Interfaces;
using UnityEngine.EventSystems;
using Logger = MagicHeim_Logger.Logger;

namespace MagicHeim.UI_s;

public class DragUI : MonoBehaviour, IDragHandler, IEndDragHandler
{
    public RectTransform dragRect;
    private static ConfigEntry<float> UI_X;
    private static ConfigEntry<float> UI_Y;

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
    public RectTransform dragRect;
    private static ConfigEntry<float> UI_X;
    private static ConfigEntry<float> UI_Y;
    public Vector3 Scale => new Vector3(dragRect.localScale.x, dragRect.localScale.y, 1f);
    
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
        NeedToChange,
        Changed
    }

    public static Change Status = Change.NeedToChange;

    private static GameObject UI;
    private static GameObject SkillElement;
    private static Transform SkillsTransform;

    private static ConfigEntry<int> MaxSlots;
    public static ConfigEntry<bool> UseAltHotkey;
    public static ConfigEntry<KeyCode>[] MH_Hotkeys;

    public static ResizeUI Resizer;

    private static readonly List<KeyCode> DefaultHotkeys = new List<KeyCode>()
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
        KeyCode.K,
    };

    private static MH_ClassDefinition _currentClass;

    public static bool IsVisible() => UI && UI.activeSelf;

    private static readonly Dictionary<int, PanelButton> _skillSlots = new Dictionary<int, PanelButton>();


    private static Image ExpBar;
    private static long LastEXP;


    public static void Init()
    {
        MaxSlots = MagicHeim.config("SkillPanel", "MaxSlots", 10, "Max slots in skill panel");
        UseAltHotkey = MagicHeim._thistype.Config.Bind("SkillPanel", "UseAltHotkey", true, "Use alt hotkey for skills");
        MH_Hotkeys = new ConfigEntry<KeyCode>[11];
        for (int i = 0; i < 10; ++i)
        {
            MH_Hotkeys[i] = MagicHeim._thistype.Config.Bind("SkillPanel", "SkillHotkey_" + (i + 1), DefaultHotkeys[i],
                "Hotkey for skill " + (i + 1));
        }

        MH_Hotkeys[10] = MagicHeim._thistype.Config.Bind("SkillPanel", "Open Skillbook", DefaultHotkeys[10],
            "Hotkey for SkillBook");

        UI = UnityEngine.Object.Instantiate(MagicHeim.asset.LoadAsset<GameObject>("SkillPanelUI"));
        Localization.instance.Localize(UI.transform);
        UI.name = "SkillPanelUI";
        UnityEngine.Object.DontDestroyOnLoad(UI);
        UI.SetActive(false);
        SkillElement = MagicHeim.asset.LoadAsset<GameObject>("SkillPanelSkill");
        SkillsTransform = UI.transform.Find("Canvas/Background/skills");
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
        for (var i = 0; i < Mathf.Min(10, MaxSlots.Value); i++)
        {
            PanelButton button = new();
            var gameObject = UnityEngine.Object.Instantiate(SkillElement, SkillsTransform);
            int i1 = i;
            gameObject.GetComponent<UIInputHandler>().m_onLeftClick = _ => DragSkill(i1);
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

        Status = Change.NeedToChange;
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

    private static void DragSkill(int index)
    {
        ClassSelectionUI.AUsrc.Play();
        if (index > _skillSlots.Count) return;
        //if (_skillSlots[index].Skill != null && _skillSlots[index].Skill.GetCooldown() > 0) return; // can replace skill if on CD ???
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

        Status = Change.NeedToChange;
    }

    public static void SetSkill(int slot, int skill)
    {
        if (slot > _skillSlots.Count)
            return;
        _skillSlots[slot].Skill = ClassManager.CurrentClassDef.GetSkills().TryGetValue(skill, out var skillDef)
            ? skillDef
            : null;
        Status = Change.NeedToChange;
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
                if (UseAltHotkey.Value)
                {
                    if (!Input.GetKey(KeyCode.LeftAlt) || !Input.GetKeyDown(MH_Hotkeys[button.Key].Value)) continue;
                    if (button.Value.Skill != null && button.Value.Skill.Level > 0)
                    {
                        SkillCastHelper.CastSkill(button.Value.Skill,
                            cond: () => Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(MH_Hotkeys[button.Key].Value));
                    }
                }
                else
                {
                    if (!Input.GetKeyDown(MH_Hotkeys[button.Key].Value)) continue;
                    if (button.Value.Skill != null && button.Value.Skill.Level > 0)
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
                    if (Status == Change.NeedToChange)
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

                    if (Status == Change.NeedToChange)
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
        foreach (Transform transform in SkillsTransform)
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
}