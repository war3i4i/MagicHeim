using JetBrains.Annotations;
using TMPro;
using Logger = MagicHeim_Logger.Logger;
using Object = UnityEngine.Object;

namespace MagicHeim.UI_s;

public static class OptionsUI
{
    private static readonly List<GameObject> mainHotkeys = new();
    private static readonly List<GameObject> additionalHotkeys = new();
    private static readonly List<GameObject> useAdditionalBools = new();
    private static readonly List<KeyCode> MainButtons = new();
    private static readonly List<KeyCode> AdditionalButtons = new();
    private static readonly List<bool> AdditionalButtonsEnabled = new();


    public static int CurrentChoosenButton = -1;
    public static int CurrentChoosenButtonAdditional = -1;

    private static void SetCurrentActiveHotkeySwap(int index)
    {
        ClassSelectionUI.AUsrc.Play();
        mainHotkeys.ForEach(x => x.transform.Find("Background").GetComponent<Image>().color = Color.white);
        mainHotkeys[index].transform.Find("Background").GetComponent<Image>().color = Color.green;
        CurrentChoosenButton = index;
    }

    private static void SetCurrentActiveHotkeySwapAdditional(int index)
    {
        ClassSelectionUI.AUsrc.Play();
        additionalHotkeys.ForEach(x => x.transform.Find("Background").GetComponent<Image>().color = Color.white);
        additionalHotkeys[index].transform.Find("Background").GetComponent<Image>().color = Color.green;
        CurrentChoosenButtonAdditional = index;
    }

    public static void ButtonPressed(KeyCode key)
    {
        if (CurrentChoosenButton == -1) return;
        mainHotkeys.ForEach(x => x.transform.Find("Background").GetComponent<Image>().color = Color.white);
        mainHotkeys[CurrentChoosenButton].transform.Find("Background/key").GetComponent<Text>().text = key.ToString().Replace("Alpha", "");
        MainButtons[CurrentChoosenButton] = key;
        CurrentChoosenButton = -1;
    }

    public static void ButtonPressedAdditional(KeyCode key)
    {
        if (CurrentChoosenButtonAdditional == -1) return;
        additionalHotkeys.ForEach(x => x.transform.Find("Background").GetComponent<Image>().color = Color.white);
        additionalHotkeys[CurrentChoosenButtonAdditional].transform.Find("Background/key").GetComponent<Text>().text = key.ToString().Replace("Alpha", "");
        AdditionalButtons[CurrentChoosenButtonAdditional] = key;
        CurrentChoosenButtonAdditional = -1;
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    static class Menu_Start_Patch
    {
        private static bool firstInit = true;

        [UsedImplicitly]
        private static void Postfix(FejdStartup __instance)
        {
            if (!firstInit) return;
            firstInit = false;
            GameObject settingsPrefab = __instance.m_settingsPrefab;
            Transform gameplay = settingsPrefab.transform.Find("Panel/TabButtons/Gameplay");
            if (!gameplay) gameplay = settingsPrefab.transform.Find("Panel/TabButtons/Tabs/Gameplay");
            if (!gameplay) return;
            Transform newButton = Object.Instantiate(gameplay);
            newButton.transform.Find("KeyHint").gameObject.SetActive(false);
            newButton.SetParent(gameplay.parent, false); 
            newButton.name = "MagicHeim_Settings";
            newButton.SetAsLastSibling();
            Transform textTransform = newButton.transform.Find("Label");
            Transform textTransform_Selected = newButton.transform.Find("Selected/LabelSelected");
            if (!textTransform || !textTransform_Selected) return;
            textTransform.GetComponent<TMP_Text>().text = "<color=#00FFFF>Magic</color><color=yellow>Heim</color>";
            textTransform_Selected.GetComponent<TMP_Text>().text = "<color=#00FFFF>Magic</color><color=yellow>Heim</color>";
            TabHandler tabHandler = settingsPrefab.transform.Find("Panel/TabButtons").GetComponent<TabHandler>();
            Transform page = settingsPrefab.transform.Find("Panel/TabContent");
            GameObject newPage = Object.Instantiate(MagicHeim.asset.LoadAsset<GameObject>("MagicHeimSettings"));
            newPage.AddComponent<MH_Settings>();
            Localization.instance.Localize(newPage.transform);
            newPage.transform.SetParent(page);
            newPage.name = "MagicHeim_Settings";
            newPage.SetActive(false);
            TabHandler.Tab newTab = new TabHandler.Tab
            {
                m_default = false,
                m_button = newButton.GetComponent<Button>(),
                m_page = newPage.GetComponent<RectTransform>()
            };
            tabHandler.m_tabs.Add(newTab);
            newPage.transform.localScale *= 1.2f;
        }
    }

 
    public class MH_Settings : Valheim.SettingsGui.SettingsBase
    {
        public override void FixBackButtonNavigation(Button backButton) { }
        public override void FixOkButtonNavigation(Button okButton) { }
        
        public override void LoadSettings()
        {
            mainHotkeys.Clear();
            additionalHotkeys.Clear();
            useAdditionalBools.Clear();
            MainButtons.Clear();
            AdditionalButtons.Clear();
            AdditionalButtonsEnabled.Clear();
            Transform main = this.transform.Find("Background");
            
            var currentMainHotkeys = SkillPanelUI.MH_Hotkeys;
            var currentAdditionalHotkeys = SkillPanelUI.MH_AdditionalHotkeys;
            var currentUseAdditionalHotkeys = SkillPanelUI.UseAltHotkey;

            var firstTen = main.Find("FirstTen");
            for (int i = 1; i <= 10; ++i)
            {
                var key = firstTen.Find("KeysFirstTen/Key" + i);
                mainHotkeys.Add(key.gameObject);
                MainButtons.Add(currentMainHotkeys[i - 1].Value);
                key.Find("Label").GetComponent<Text>().text = "Use Skill " + i;
                key.Find("Background/key").GetComponent<Text>().text = currentMainHotkeys[i - 1].Value.ToString().Replace("Alpha", "");
                var i1 = i;
                key.Find("Background").GetComponent<Button>().onClick.AddListener(() => SetCurrentActiveHotkeySwap(i1 - 1));
                
                var additionalKey = firstTen.Find("AdditionalButtons/Key" + i);
                additionalHotkeys.Add(additionalKey.gameObject);
                AdditionalButtons.Add(currentAdditionalHotkeys[i - 1].Value);
                additionalKey.Find("Background/key").GetComponent<Text>().text = currentAdditionalHotkeys[i - 1].Value.ToString().Replace("Alpha", "");
                additionalKey.Find("Background").GetComponent<Button>().onClick.AddListener(() => SetCurrentActiveHotkeySwapAdditional(i1 - 1));
                
                var useAlt = firstTen.Find("Alts/Alt" + i);
                AdditionalButtonsEnabled.Add(currentUseAdditionalHotkeys[i - 1].Value);
                useAlt.Find("Background/Checkmark").gameObject.SetActive(currentUseAdditionalHotkeys[i - 1].Value);
                useAlt.Find("Background").GetComponent<Button>().onClick.AddListener(() =>
                {
                    AdditionalButtonsEnabled[i1 - 1] = !AdditionalButtonsEnabled[i1 - 1];
                    useAlt.Find("Background/Checkmark").gameObject.SetActive(AdditionalButtonsEnabled[i1 - 1]);
                    ClassSelectionUI.AUsrc.Play();
                });
                useAdditionalBools.Add(useAlt.gameObject);
            }
            
            var secondTen = main.Find("SecondTen");
            for (int i = 11; i <= 20; ++i)
            {
                var key = secondTen.Find("KeysFirstTen/Key" + i);
                mainHotkeys.Add(key.gameObject); 
                MainButtons.Add(currentMainHotkeys[i - 1].Value);
                key.Find("Label").GetComponent<Text>().text = "Use Skill " + i;
                key.Find("Background/key").GetComponent<Text>().text = currentMainHotkeys[i - 1].Value.ToString().Replace("Alpha", "");
                var i1 = i;
                key.Find("Background").GetComponent<Button>().onClick.AddListener(() => SetCurrentActiveHotkeySwap(i1 - 1));
                 
                var additionalKey = secondTen.Find("AdditionalButtons/Key" + i);
                additionalHotkeys.Add(additionalKey.gameObject);
                AdditionalButtons.Add(currentAdditionalHotkeys[i - 1].Value); 
                additionalKey.Find("Background/key").GetComponent<Text>().text = currentAdditionalHotkeys[i - 1].Value.ToString().Replace("Alpha", "");
                additionalKey.Find("Background").GetComponent<Button>().onClick.AddListener(() => SetCurrentActiveHotkeySwapAdditional(i1 - 1));
                
                var useAlt = secondTen.Find("Alts/Alt" + i);
                AdditionalButtonsEnabled.Add(currentUseAdditionalHotkeys[i - 1].Value);
                useAlt.Find("Background/Checkmark").gameObject.SetActive(currentUseAdditionalHotkeys[i - 1].Value);
                useAlt.Find("Background").GetComponent<Button>().onClick.AddListener(() =>
                {
                    AdditionalButtonsEnabled[i1 - 1] = !AdditionalButtonsEnabled[i1 - 1];
                    useAlt.Find("Background/Checkmark").gameObject.SetActive(AdditionalButtonsEnabled[i1 - 1]);
                    ClassSelectionUI.AUsrc.Play();
                });
                useAdditionalBools.Add(useAlt.gameObject);
            }
            
            var openSkillBook = main.Find("OpenBook");
            mainHotkeys.Add(openSkillBook.gameObject); 
            openSkillBook.Find("Label").GetComponent<Text>().text = "Open Skill Book";
            MainButtons.Add(SkillPanelUI.MH_Hotkeys[20].Value);
            openSkillBook.Find("Background/key").GetComponent<Text>().text = SkillPanelUI.MH_Hotkeys[20].Value.ToString().Replace("Alpha", "");
            openSkillBook.Find("Background").GetComponent<Button>().onClick.AddListener(() => SetCurrentActiveHotkeySwap(20));
            
            main.Find("DefaultUI").GetComponent<Button>().onClick.AddListener(() =>
            {
                ClassSelectionUI.AUsrc.Play();
                ResizeUI.Default();
                DragUI.Default();
            });
            
            main.Find("DefaultButtons").GetComponent<Button>().onClick.AddListener(() =>
            {
                ClassSelectionUI.AUsrc.Play();
                for (int i = 0; i < 20; ++i)
                {
                    MainButtons[i] = (KeyCode)SkillPanelUI.MH_Hotkeys[i].DefaultValue; 
                    mainHotkeys[i].transform.Find("Background/key").GetComponent<Text>().text = MainButtons[i].ToString().Replace("Alpha", "");
                    AdditionalButtons[i] = (KeyCode)SkillPanelUI.MH_AdditionalHotkeys[i].DefaultValue;
                    additionalHotkeys[i].transform.Find("Background/key").GetComponent<Text>().text = AdditionalButtons[i].ToString().Replace("Alpha", "");
                    AdditionalButtonsEnabled[i] = (bool)SkillPanelUI.UseAltHotkey[i].DefaultValue;
                    useAdditionalBools[i].transform.Find("Background/Checkmark").gameObject.SetActive(AdditionalButtonsEnabled[i]);
                }
            });
        }

        public override void SaveSettings()
        {
            for (int i = 0; i < 20; ++i)
            {
                SkillPanelUI.MH_Hotkeys[i].Value = MainButtons[i];
                SkillPanelUI.MH_AdditionalHotkeys[i].Value = AdditionalButtons[i];
                SkillPanelUI.UseAltHotkey[i].Value = AdditionalButtonsEnabled[i];
            }
            SkillPanelUI.MH_Hotkeys[20].Value = MainButtons[20];
            MagicHeim._thistype.Config.Save();
            SkillPanelUI.Status = SkillPanelUI.Change.ToUpdate;
        }
    }
}