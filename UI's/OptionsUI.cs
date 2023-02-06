using MagicHeim.MH_Interfaces;
using UnityEngine.PlayerLoop;
using Logger = MagicHeim_Logger.Logger;
using Object = UnityEngine.Object;

namespace MagicHeim.UI_s;

public static class OptionsUI
{
    private static GameObject LatestUI;
    private static readonly List<Image> toDropColor = new();


    public static int CurrentChoosenButton = -1;
    public static int CurrentChoosenButtonAdditional = -1;

    private static void SetCurrentActiveHotkeySwap(int index)
    {
        ClassSelectionUI.AUsrc.Play();
        toDropColor.ForEach(x => x.color = Color.white);
        toDropColor[index * 2].color = Color.green;
        CurrentChoosenButton = index;
    }

    private static void SetCurrentActiveHotkeySwapAdditional(int index)
    {
        ClassSelectionUI.AUsrc.Play();
        toDropColor.ForEach(x => x.color = Color.white);
        toDropColor[index * 2 + 1].color = Color.green;
        CurrentChoosenButtonAdditional = index;
    }

    public static void ButtonPressed(KeyCode key)
    {
        if (CurrentChoosenButton == -1) return;
        SkillPanelUI.MH_Hotkeys[CurrentChoosenButton].Value = key;
        CurrentChoosenButton = -1;
        toDropColor.ForEach(x => x.color = Color.white);
        MagicHeim._thistype.Config.Save();
        UpdateValues();
        SkillPanelUI.Status = SkillPanelUI.Change.ToUpdate;
    }

    public static void ButtonPressedAdditional(KeyCode key)
    {
        if (CurrentChoosenButtonAdditional == -1) return;
        SkillPanelUI.MH_AdditionalHotkeys[CurrentChoosenButtonAdditional].Value = key;
        CurrentChoosenButtonAdditional = -1;
        toDropColor.ForEach(x => x.color = Color.white);
        MagicHeim._thistype.Config.Save();
        UpdateValues();
    }


    public static void UpdateValues()
    {
        if (!LatestUI) return;
        var hotkeys = SkillPanelUI.MH_Hotkeys;
        var additional = SkillPanelUI.MH_AdditionalHotkeys;
        for (int i = 0; i < 10; ++i)
        {
            LatestUI.transform.Find($"Keys/Key{i + 1}/Label").GetComponent<Text>().text = $"Use Skill {i + 1}";
            LatestUI.transform.Find($"Keys/Key{i + 1}/Background/key").GetComponent<Text>().text =
                hotkeys[i].Value.ToString().Replace("Alpha", "").Replace("Mouse", "M");

            LatestUI.transform.Find($"AdditionalButtons/Key{i + 1}/Background/key").GetComponent<Text>().text =
                additional[i].Value.ToString().Replace("Alpha", "").Replace("Mouse", "M");

            LatestUI.transform.Find($"Alts/Alt{i + 1}/Background/Checkmark").gameObject
                .SetActive(SkillPanelUI.UseAltHotkey[i].Value);
        }

        LatestUI.transform.Find($"Keys/Key11/Label").GetComponent<Text>().text = $"Open SkillBook";
        LatestUI.transform.Find($"Keys/Key11/Background/key").GetComponent<Text>().text =
            hotkeys[10].Value.ToString().Replace("Alpha", "").Replace("Mouse", "M");
    }

    [HarmonyPatch(typeof(Settings), nameof(Settings.Awake))]
    static class Settings_Awake_Patch
    {
        static void Postfix(Settings __instance)
        {
            var hotkeys = SkillPanelUI.MH_Hotkeys;
            var additional = SkillPanelUI.MH_AdditionalHotkeys;
            var MH = __instance.transform.Find("panel/Tabs/MagicHeim");
            toDropColor.Clear();
            for (int i = 0; i < 10; ++i)
            {
                MH.transform.Find($"Keys/Key{i + 1}/Label").GetComponent<Text>().text = $"Use Skill {i + 1}";
                MH.transform.Find($"Keys/Key{i + 1}/Background/key").GetComponent<Text>().text =
                    hotkeys[i].Value.ToString().Replace("Alpha", "").Replace("Mouse", "M");
                int i1 = i;
                MH.transform.Find($"Keys/Key{i + 1}/Background").GetComponent<Button>().onClick
                    .AddListener(() => SetCurrentActiveHotkeySwap(i1));
                toDropColor.Add(MH.transform.Find($"Keys/Key{i + 1}/Background").GetComponent<Image>());

                MH.transform.Find($"AdditionalButtons/Key{i + 1}/Background/key").GetComponent<Text>().text =
                    additional[i].Value.ToString().Replace("Alpha", "").Replace("Mouse", "M");
                MH.transform.Find($"AdditionalButtons/Key{i + 1}/Background").GetComponent<Button>().onClick
                    .AddListener(() => SetCurrentActiveHotkeySwapAdditional(i1));
                toDropColor.Add(MH.transform.Find($"AdditionalButtons/Key{i + 1}/Background").GetComponent<Image>());

                MH.transform.Find($"Alts/Alt{i + 1}/Background/Checkmark").gameObject
                    .SetActive(SkillPanelUI.UseAltHotkey[i].Value);
                MH.transform.Find($"Alts/Alt{i + 1}/Background").GetComponent<Button>().onClick.AddListener(() =>
                {
                    ClassSelectionUI.AUsrc.Play();
                    SkillPanelUI.UseAltHotkey[i1].Value = !SkillPanelUI.UseAltHotkey[i1].Value;
                    MH.transform.Find($"Alts/Alt{i1 + 1}/Background/Checkmark").gameObject
                        .SetActive(SkillPanelUI.UseAltHotkey[i1].Value);
                    MagicHeim._thistype.Config.Save();
                });
            }

            MH.transform.Find("Keys/Key11/Label").GetComponent<Text>().text = $"Open Skillbook";
            MH.transform.Find("Keys/Key11/Background/key").GetComponent<Text>().text =
                hotkeys[10].Value.ToString().Replace("Alpha", "").Replace("Mouse", "M");
            MH.transform.Find("Keys/Key11/Background").GetComponent<Button>().onClick
                .AddListener(() => SetCurrentActiveHotkeySwap(10));
            toDropColor.Add(MH.transform.Find("Keys/Key11/Background").GetComponent<Image>());

            MH.transform.Find("DefaultButtons").GetComponent<Button>().onClick.AddListener(() =>
            {
                ClassSelectionUI.AUsrc.Play();
                Reset();
            });
            
            MH.transform.Find("DefaultUI").GetComponent<Button>().onClick.AddListener(() =>
            {
                ClassSelectionUI.AUsrc.Play();
                ResetUI();
            });

            MH.transform.Find("Ok").GetComponent<Button>().onClick.AddListener(() =>
            { 
                ClassSelectionUI.AUsrc.Play();
                __instance.OnOk();
            });

            LatestUI = MH.gameObject;
        }
    }

    private static void Reset()
    {
        var hotkeys = SkillPanelUI.MH_Hotkeys;
        var additional = SkillPanelUI.MH_AdditionalHotkeys;
        var useAlt = SkillPanelUI.UseAltHotkey;
        for (int i = 0; i < 10; i++)
        {
            hotkeys[i].Value = SkillPanelUI.DefaultHotkeys[i];
            additional[i].Value = KeyCode.LeftAlt;
            useAlt[i].Value = true;
        }
        hotkeys[10].Value = KeyCode.K;
        MagicHeim._thistype.Config.Save();
        UpdateValues();
    }

    private static void ResetUI()
    {
        ResizeUI.Default();
        DragUI.Default();
    }


    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    static class Menu_Start_Patch
    {
        private static bool firstInit = true;

        static void Postfix(FejdStartup __instance)
        {
            if (!firstInit) return;
            firstInit = false;
            var settingsPrefab = __instance.m_settingsPrefab;
            var controls = settingsPrefab.transform.Find("panel/TabButtons/Controlls");
            var newButton = UnityEngine.Object.Instantiate(controls);
            newButton.SetParent(controls.parent, false);
            newButton.name = "MagicHeim";
            newButton.SetAsLastSibling();
            newButton.GetComponent<RectTransform>().anchoredPosition +=
                new Vector2(0, newButton.GetComponent<RectTransform>().sizeDelta.y);
            newButton.transform.Find("Text").GetComponent<Text>().text =
                "<color=cyan>Magic</color><color=yellow>Heim</color>";
            var tabHandler = settingsPrefab.transform.Find("panel/TabButtons").GetComponent<TabHandler>();
            var page = settingsPrefab.transform.Find("panel/Tabs");
            GameObject newPage =
                UnityEngine.Object.Instantiate(MagicHeim.asset.LoadAsset<GameObject>("MagicHeimSettings"));
            newPage.transform.SetParent(page);
            newPage.name = "MagicHeim";
            newPage.SetActive(false);
            TabHandler.Tab newTab = new TabHandler.Tab();
            newTab.m_default = false;
            newTab.m_button = newButton.GetComponent<Button>();
            newTab.m_page = newPage.GetComponent<RectTransform>();
            tabHandler.m_tabs.Add(newTab);
        }
    }
}