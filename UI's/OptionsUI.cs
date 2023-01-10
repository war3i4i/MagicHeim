using MagicHeim.MH_Interfaces;
using UnityEngine.PlayerLoop;
using Logger = MagicHeim_Logger.Logger;
using Object = UnityEngine.Object;

namespace MagicHeim.UI_s;

public static class OptionsUI
{
    private static GameObject LatestUI;
    private static readonly List<Image> toDropColor = new();

    private static void Init(GameObject UI)
    {
        UI.transform.Find("Ok").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -340f);
        UI.transform.Find("Keys").GetComponent<RectTransform>().localScale = new Vector3(1.5f, 1.5f, 1.5f);
        UI.transform.Find("Keys").GetComponent<RectTransform>().anchoredPosition = new Vector2(-200, 225f);
        UI.transform.Find("UseAlt").GetComponent<RectTransform>().localScale = new Vector3(1.5f, 1.5f, 1.5f);
        UI.transform.Find("UseAlt").GetComponent<RectTransform>().anchoredPosition = new Vector2(250, 225f);
    }


    public static int CurrentChoosenButton = -1;

    private static void SetCurrentActiveHotkeySwap(int index)
    {
        ClassSelectionUI.AUsrc.Play();
        toDropColor.ForEach(x => x.color = Color.white);
        toDropColor[index].color = Color.green;
        CurrentChoosenButton = index;
    }

    public static void ButtonPressed(KeyCode key)
    {
        if (CurrentChoosenButton == -1) return;
        SkillPanelUI.MH_Hotkeys[CurrentChoosenButton].Value = key;
        CurrentChoosenButton = -1;
        toDropColor.ForEach(x => x.color = Color.white);
        MagicHeim._thistype.Config.Save();
        UpdateValues();
        SkillPanelUI.Status = SkillPanelUI.Change.NeedToChange;
    }


    public static void UpdateValues()
    {
        if (!LatestUI) return;
        var hotkeys = SkillPanelUI.MH_Hotkeys;
        for (int i = 0; i < 10; ++i)
        {
            LatestUI.transform.Find($"Keys/Key{i + 1}/Label").GetComponent<Text>().text = $"Use Skill {i + 1}";
            LatestUI.transform.Find($"Keys/Key{i + 1}/Background/key").GetComponent<Text>().text =
                hotkeys[i].Value.ToString().Replace("Alpha", "").Replace("Mouse", "M");
        }

        LatestUI.transform.Find($"Keys/Key{11}/Label").GetComponent<Text>().text = $"Open SkillBook";
        LatestUI.transform.Find($"Keys/Key{11}/Background/key").GetComponent<Text>().text =
            hotkeys[10].Value.ToString().Replace("Alpha", "").Replace("Mouse", "M");
    }

    [HarmonyPatch(typeof(Settings), nameof(Settings.Awake))]
    static class Settings_Awake_Patch
    {
        static void Postfix(Settings __instance)
        {
            var hotkeys = SkillPanelUI.MH_Hotkeys;
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
            }

            MH.transform.Find($"Keys/Key{11}/Label").GetComponent<Text>().text = $"Open Skillbook";
            MH.transform.Find($"Keys/Key{11}/Background/key").GetComponent<Text>().text =
                hotkeys[10].Value.ToString().Replace("Alpha", "").Replace("Mouse", "M");
            MH.transform.Find($"Keys/Key{11}/Background").GetComponent<Button>().onClick
                .AddListener(() => SetCurrentActiveHotkeySwap(10));
            toDropColor.Add(MH.transform.Find($"Keys/Key{11}/Background").GetComponent<Image>());


            MH.transform.Find("Ok").GetComponent<Button>().onClick.AddListener(() =>
            {
                ClassSelectionUI.AUsrc.Play();
                __instance.OnOk();
            });
            MH.transform.Find("UseAlt/Background/Checkmark").gameObject.SetActive(SkillPanelUI.UseAltHotkey.Value);
            MH.transform.Find("UseAlt/Background").GetComponent<Button>().onClick.AddListener(() =>
            {
                ClassSelectionUI.AUsrc.Play();
                SkillPanelUI.UseAltHotkey.Value = !SkillPanelUI.UseAltHotkey.Value;
                MH.transform.Find("UseAlt/Background/Checkmark").gameObject.SetActive(SkillPanelUI.UseAltHotkey.Value);
                MagicHeim._thistype.Config.Save();
            });

            LatestUI = MH.gameObject;
        }
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
            Init(newPage);
            newPage.SetActive(false);
            TabHandler.Tab newTab = new TabHandler.Tab();
            newTab.m_default = false;
            newTab.m_button = newButton.GetComponent<Button>();
            newTab.m_page = newPage.GetComponent<RectTransform>();
            tabHandler.m_tabs.Add(newTab);
        }
    }
}