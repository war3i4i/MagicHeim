using MagicHeim.MH_Interfaces;

namespace MagicHeim.UI_s;

public static class SkillBookUI
{
    private static GameObject UI;
    private static GameObject LevelGroup;
    private static GameObject SkillData;


    private static Transform Content;
    private static Transform HoverInfo;

    private static Text Skillpoints_Text;
    private static Text Level_Text;
    private static Image EXP_Fillbar;

    private static bool IsCurrentTabPassives;
    private static bool ShowOnlyLearned;
    private static bool IsAltarClose;

    public static bool IsVisible() => UI && UI.activeSelf;

    [HarmonyPatch(typeof(Menu), nameof(Menu.IsVisible))]
    static class Menu_IsVisible_Patch
    {
        static void Postfix(ref bool __result)
        {
            __result |= IsVisible();
        }
    }

    public static void Init()
    {
        UI = UnityEngine.Object.Instantiate(MagicHeim.asset.LoadAsset<GameObject>("SkillBookUI"));
        Localization.instance.Localize(UI.transform);
        UnityEngine.Object.DontDestroyOnLoad(UI);
        UI.SetActive(false);
        LevelGroup = MagicHeim.asset.LoadAsset<GameObject>("SkillBook_LevelGroup");
        SkillData = MagicHeim.asset.LoadAsset<GameObject>("SkillBook_SkillData");

        Content = UI.transform.Find("Canvas/MainTab/book/Scroll View/Viewport/Content");
        HoverInfo = UI.transform.Find("Canvas/MainTab/book/HoverInfo");

        UI.transform.Find("Canvas/MainTab/buttons/ResetButton").GetComponent<Button>().onClick.AddListener(() =>
        {
            ClassSelectionUI.AUsrc.Play();
            ConfirmationUI.Show("Reset Skills",
                "Are you sure want to reset your skills?\nWasted resource won't recover", () =>
                {
                    ClassManager.ResetSkills();
                    LoadSkills(IsCurrentTabPassives);
                });
        });

        UI.transform.Find("Canvas/MainTab/buttons/SkillsButton").GetComponent<Button>().onClick.AddListener(() =>
        {
            ClassSelectionUI.AUsrc.Play();
            LoadSkills(false);
        });

        UI.transform.Find("Canvas/MainTab/buttons/PassivesButton").GetComponent<Button>().onClick.AddListener(() =>
        {
            ClassSelectionUI.AUsrc.Play();
            LoadSkills(true);
        });

        UI.transform.Find("Canvas/MainTab/book/toggle").GetComponent<Button>().onClick.AddListener(() =>
        {
            ClassSelectionUI.AUsrc.Play();
            ShowOnlyLearned = !ShowOnlyLearned;
            UI.transform.Find("Canvas/MainTab/book/toggle/Image").gameObject.SetActive(ShowOnlyLearned);
            LoadSkills(IsCurrentTabPassives);
        });

        Skillpoints_Text = UI.transform.Find("Canvas/MainTab/infopart/Skillpoints/Text").GetComponent<Text>();
        Level_Text = UI.transform.Find("Canvas/MainTab/infopart/CurrentLevel/Text").GetComponent<Text>();
        EXP_Fillbar = UI.transform.Find("Canvas/MainTab/infopart/CurrentLevel/ExpBar/Image").GetComponent<Image>();
    }

    private static void Default()
    {
        foreach (Transform child in Content)
        {
            UnityEngine.Object.Destroy(child.gameObject);
        }

        HoverInfo.gameObject.SetActive(false);
    }

    private static void UpdateCanvases()
    {
        Canvas.ForceUpdateCanvases();
        var sizeFillers = Content.GetComponentsInChildren<ContentSizeFitter>().ToList();
        sizeFillers.ForEach(filter => filter.enabled = false);
        sizeFillers.ForEach(filter => filter.enabled = true);
    }

    private static void LoadSkills(bool isPassive)
    {
        Default();
        IsCurrentTabPassives = isPassive;
        Level_Text.text = ClassManager.Level.ToString();
        Skillpoints_Text.text = $"Skillpoints: <color=yellow>{ClassManager.SkillPoints.ToString()}</color>";
        EXP_Fillbar.fillAmount = ClassManager.EXP / (float)ClassManager.GetExpForLevel(ClassManager.Level);
        EXP_Fillbar.color = ClassManager.CurrentClassDef.GetColor;

        var currentClass = ClassManager.CurrentClassDef;
        var activeSkills = currentClass.GetSkills().Values.Where(x => x.IsPassive == isPassive).ToList();

        Dictionary<int, List<MH_Skill>> skillsByRequiredLevel = new Dictionary<int, List<MH_Skill>>();
        List<List<MH_Skill>> toOrder = new List<List<MH_Skill>>();
        foreach (var skill in activeSkills)
        {
            if (ShowOnlyLearned && skill.Level == 0)
            {
                continue;
            }

            if (!skillsByRequiredLevel.ContainsKey(skill.RequiredLevel))
            {
                skillsByRequiredLevel.Add(skill.RequiredLevel, new List<MH_Skill>());
                toOrder.Add(skillsByRequiredLevel[skill.RequiredLevel]);
            }

            skillsByRequiredLevel[skill.RequiredLevel].Add(skill);
        }

        for (int i = 0; i < toOrder.Count; i++)
        {
            toOrder[i] = toOrder[i].OrderBy(x => Localization.instance.Localize(x.Name)).ToList();
        }

        toOrder.Clear();

        foreach (var skill in skillsByRequiredLevel.OrderBy(x => x.Key))
        {
            var skillGroup = UnityEngine.Object.Instantiate(LevelGroup, Content);
            skillGroup.transform.Find("BG/Text").GetComponent<Text>().text =
                $"Required Level: <color=yellow>{skill.Key}</color>";
            foreach (var list in skill.Value)
            {
                var skillData = UnityEngine.Object.Instantiate(SkillData, skillGroup.transform);
                skillData.transform.Find("Text").GetComponent<Text>().text = Localization.instance.Localize(list.Name);
                if (list.IsPassive)
                {
                    skillData.transform.Find("Icon_Passive").gameObject.SetActive(true);
                    skillData.transform.Find("Icon_Passive/image").GetComponent<Image>().sprite = list.Icon;
                    skillData.transform.Find("Icon_Passive/image").GetComponent<Image>().color =
                        list.Level > 0 ? Color.white : new Color(0f, 1f, 1f, 1f);
                }
                else
                {
                    skillData.transform.Find("Icon").gameObject.SetActive(true);
                    skillData.transform.Find("Icon/image").GetComponent<Image>().sprite = list.Icon;
                    skillData.transform.Find("Icon/image").GetComponent<UIInputHandler>().m_onLeftClick = (_) =>
                    {
                        ClassSelectionUI.AUsrc.Play();
                        SkillDrag.StartDrag(list);
                    };
                    skillData.transform.Find("Icon/image").GetComponent<Image>().color =
                        list.Level > 0 ? Color.white : new Color(0f, 1f, 1f, 1f);
                }

                skillData.transform.Find("LevelText").GetComponent<Text>().text =
                    $"Level: {list.Level.ToString()} {(list.Level >= list.MaxLevel ? "<color=yellow>(Max)</color>" : "")}";
                skillData.transform.Find("LevelText/Req").GetComponent<Text>().text = "";
                skillData.transform.Find("LevelText/ReqIcon").gameObject.SetActive(false);
                if (IsAltarClose && ClassManager.CanUpgradeSkill(list))
                {
                    string requiredPrefab;
                    if (list.Level + 1 >= list.MaxLevel) requiredPrefab = list.RequiredItemToUpgradeFinal;
                    else if (list.Level > list.MaxLevel / 2) requiredPrefab = list.RequiredItemToUpgradeSecondHalf;
                    else requiredPrefab = list.RequiredItemToUpgrade;

                    int requiredAmount;
                    if (list.Level + 1 >= list.MaxLevel) requiredAmount = list.RequiredItemAmountToUpgradeFinal;
                    else if (list.Level > list.MaxLevel / 2)
                        requiredAmount = list.RequiredItemAmountToUpgradeSecondHalf;
                    else requiredAmount = list.RequiredItemAmountToUpgrade;

                    GameObject upradeItem = ObjectDB.instance.GetItemPrefab(requiredPrefab);
                    if (upradeItem && requiredAmount > 0)
                    {
                        string name =
                            Localization.instance.Localize(upradeItem.GetComponent<ItemDrop>().m_itemData.m_shared
                                .m_name);
                        bool hasItems = ClassManager.HaveEnoughItemsForSkillUpgrade(list, out int amount);
                        skillData.transform.Find("LevelText/Req").GetComponent<Text>().text =
                            $"Requires: <color={(hasItems ? "lime" : "red")}>{name}</color> <color=yellow>x{amount}</color>";
                        skillData.transform.Find("LevelText/ReqIcon").gameObject.SetActive(true);
                        skillData.transform.Find("LevelText/ReqIcon").GetComponent<Image>().sprite =
                            upradeItem.GetComponent<ItemDrop>().m_itemData.GetIcon();
                    }


                    skillData.transform.Find("LevelText/LevelUP_Button").GetComponent<UIInputHandler>().m_onLeftClick =
                        (_) =>
                        {
                            ClassSelectionUI.AUsrc.Play();
                            int useShift = Input.GetKey(KeyCode.LeftShift) ? 10 : 1;
                            if (ClassManager.TryUpgradeSkill(list, useShift))
                            {
                                LoadSkills(isPassive);
                            }

                            HoverInfo.gameObject.SetActive(true);
                            HoverInfo.Find("Name").GetComponent<Text>().text =
                                Localization.instance.Localize(list.Name);
                            HoverInfo.Find("Description").GetComponent<Text>().text =
                                list.BuildDescription() + list.ExternalDescription();

                            HoverInfo.Find("Tags").GetComponent<Text>().text = list.GetSpecialTags();
                        };
                }
                else
                {
                    skillData.transform.Find("LevelText/LevelUP_Button").gameObject.SetActive(false);
                }

                skillData.GetComponent<UIInputHandler>().m_onPointerEnter = (_) =>
                {
                    HoverInfo.gameObject.SetActive(true);
                    HoverInfo.Find("Name").GetComponent<Text>().text = Localization.instance.Localize(list.Name);
                    HoverInfo.Find("Description").GetComponent<Text>().text =
                        list.BuildDescription() + list.ExternalDescription();

                    HoverInfo.Find("Tags").GetComponent<Text>().text = list.GetSpecialTags();

                    if (list.IsPassive)
                    {
                        HoverInfo.Find("Icon_Passive").gameObject.SetActive(true);
                        HoverInfo.Find("Icon").gameObject.SetActive(false);
                        HoverInfo.Find("Icon_Passive/Image").GetComponent<Image>().sprite = list.Icon;
                    }
                    else
                    {
                        HoverInfo.Find("Icon").gameObject.SetActive(true);
                        HoverInfo.Find("Icon_Passive").gameObject.SetActive(false);
                        HoverInfo.Find("Icon/Image").GetComponent<Image>().sprite = list.Icon;
                    }
                };

                skillData.GetComponent<UIInputHandler>().m_onPointerExit = (_) =>
                {
                    HoverInfo.gameObject.SetActive(false);
                };
            }
        }

        UpdateCanvases();
    } 


    public static void Show()
    {
        IsAltarClose = true;//Player.m_debugMode || MH_Altar.IsClose(Player.m_localPlayer.transform.position);
        if (ClassManager.CurrentClassDef == null)
        {
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "You don't have a class yet");
            return;
        }

        UI.transform.Find("Canvas/MainTab/buttons/ResetButton").gameObject.SetActive(IsAltarClose);
        ShowOnlyLearned = false;
        UI.transform.Find("Canvas/MainTab/book/toggle/Image").gameObject.SetActive(ShowOnlyLearned);
        UI.SetActive(true);
        LoadSkills(false);
    }

    public static void Hide()
    {
        Default();
        UI.SetActive(false);
    }
}