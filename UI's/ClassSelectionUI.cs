using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;
using UnityEngine.Video;

namespace MagicHeim.UI_s;

public static class ClassSelectionUI
{
    private static GameObject UI;
    private static GameObject SkillPreviewElement;
    private static Transform SkillPreviewTransform;
    private static Text ClassDescription;
    private static Text ClassName;
    private static Image ClassIcon;
    private static Transform SelectClassButton;
    private static VideoPlayer SkillVideo;
    private static Text SkillDescription;
    private static Class SelectedClass;
    private static bool IsMale => Player.m_localPlayer.m_modelIndex == 0;
    private static Transform MaleClasses;
    private static Transform FemaleClasses;
    public static AudioSource AUsrc;
    private static Transform LOADING;
    private static readonly List<Transform> ResetColors = new List<Transform>();

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
        UI = UnityEngine.Object.Instantiate(MagicHeim.asset.LoadAsset<GameObject>("ClassSelectionUI"));
        Localization.instance.Localize(UI.transform);
        UI.name = "ClassSelectionUI";
        UnityEngine.Object.DontDestroyOnLoad(UI);
        UI.SetActive(false);

        SkillPreviewElement = MagicHeim.asset.LoadAsset<GameObject>("SkillPreviewElement");
        SkillPreviewTransform = UI.transform.Find("Canvas/MainTab/SkillsChooseTab/Scroll View/Viewport/Content");
        ClassDescription = UI.transform.Find("Canvas/MainTab/ClassInfo/Text").GetComponent<Text>();
        ClassName = UI.transform.Find("Canvas/MainTab/ChoosenClassImg/Name").GetComponent<Text>();
        ClassIcon = UI.transform.Find("Canvas/MainTab/ChoosenClassImg/Class/Img").GetComponent<Image>();
        SelectClassButton = UI.transform.Find("Canvas/MainTab/ChoosenClassImg/Accept");
        SelectClassButton.GetComponent<Button>().onClick.AddListener(AcceptClass);
        SkillVideo = UI.transform.Find("Canvas/MainTab/SkillInfo/Video").GetComponent<VideoPlayer>();
        SkillDescription = UI.transform.Find("Canvas/MainTab/SkillInfo/Text").GetComponent<Text>();
        MaleClasses = UI.transform.Find("Canvas/MainTab/ChooseClass/Male");
        FemaleClasses = UI.transform.Find("Canvas/MainTab/ChooseClass/Female");
        LOADING = UI.transform.Find("Canvas/MainTab/SkillInfo/LOADING");
        SelectedClass = Class.Warrior;
        var classes = (Class[])Enum.GetValues(typeof(Class));
        foreach (var @class in classes)
        {
            if (@class == Class.None) continue;
            var classDefintion = ClassesDatabase.ClassesDatabase.GetClassDefinition(@class);
            var maleTransform = MaleClasses.Find(@class.ToString());
            var femaleTransform = FemaleClasses.Find(@class.ToString());
            ResetColors.Add(maleTransform);
            ResetColors.Add(femaleTransform);
            maleTransform.transform.Find("Selection").GetComponent<Image>().color = new Color(classDefintion.GetColor.r,
                classDefintion.GetColor.g, classDefintion.GetColor.b, 0.4f);
            femaleTransform.transform.Find("Selection").GetComponent<Image>().color =
                new Color(classDefintion.GetColor.r, classDefintion.GetColor.g, classDefintion.GetColor.b, 0.4f);
            maleTransform.gameObject.AddComponent<UIInputHandler>();
            femaleTransform.gameObject.AddComponent<UIInputHandler>();

            maleTransform.GetComponent<UIInputHandler>().m_onLeftClick =
                _ =>
                {
                    AUsrc.Play();
                    SelectClass(@class, maleTransform);
                };

            femaleTransform.GetComponent<UIInputHandler>().m_onLeftClick =
                _ =>
                {
                    AUsrc.Play();
                    SelectClass(@class, femaleTransform);
                };

            maleTransform.GetComponent<UIInputHandler>().m_onPointerEnter = _ =>
            {
                maleTransform.transform.Find("Selection").gameObject.SetActive(true);
            };
            femaleTransform.GetComponent<UIInputHandler>().m_onPointerEnter = _ =>
            {
                femaleTransform.transform.Find("Selection").gameObject.SetActive(true);
            };

            maleTransform.GetComponent<UIInputHandler>().m_onPointerExit = _ =>
            {
                maleTransform.transform.Find("Selection").gameObject.SetActive(false);
            };
            femaleTransform.GetComponent<UIInputHandler>().m_onPointerExit = _ =>
            {
                femaleTransform.transform.Find("Selection").gameObject.SetActive(false);
            };
        }
    }

    private static void AcceptClass()
    {
        AUsrc.Play();
        ConfirmationUI.Show($"Are you sure want to become a {SelectedClass}",
            "On class change you will lose all current class progress (Level, Exp, Skills)",
            AcceptClassInternal);
    }

    private static void AcceptClassInternal()
    {
        if (SelectedClass == Class.None) return;
        var player = Player.m_localPlayer;
        if (!player) return;
        ClassManager.SetClass(SelectedClass);
        UnityEngine.Object.Instantiate(ClassesDatabase.ClassesDatabase.GetClassDefinition(SelectedClass).OnSelect_VFX,
            Player.m_localPlayer.transform.position, Quaternion.identity);
        Hide();
    }

    private static void Default()
    {
        foreach (Transform transform in SkillPreviewTransform)
        {
            UnityEngine.Object.Destroy(transform.gameObject);
        }

        SkillDescription.text = "";
        ClassDescription.gameObject.SetActive(false);
        ClassName.gameObject.SetActive(false);
        ClassIcon.gameObject.SetActive(false);
        SelectClassButton.gameObject.SetActive(false);
        SkillVideo.gameObject.SetActive(false);
        MaleClasses.gameObject.SetActive(false);
        FemaleClasses.gameObject.SetActive(false);
        LOADING.gameObject.SetActive(false);
        SkillVideo.Stop();
        ClassIcon.transform.parent.GetComponent<Image>().color = Color.white;
        foreach (var t in ResetColors)
        {
            t.Find("Selection").gameObject.SetActive(false);
            t.GetComponent<Image>().color = Color.white;
        }
    }

    private static void SelectClass(Class @class, Transform toActivate)
    {
        Default();
        MH_ClassDefinition classDefinition = ClassesDatabase.ClassesDatabase.GetClassDefinition(@class);
        if (classDefinition == null) return;
        if (IsMale)
        {
            MaleClasses.gameObject.SetActive(true);
        }
        else
        {
            FemaleClasses.gameObject.SetActive(true);
        }

        if (classDefinition != ClassManager.CurrentClassDef) classDefinition.Init();
        SelectedClass = @class;
        ClassDescription.gameObject.SetActive(true);
        ClassName.gameObject.SetActive(true);
        ClassIcon.gameObject.SetActive(true);
        SelectClassButton.gameObject.SetActive(true);
        toActivate.GetComponent<Image>().color = classDefinition.GetColor;
        ClassName.text = classDefinition.Name;
        ClassName.GetComponent<Text>().color = classDefinition.GetColor;
        ClassIcon.sprite = toActivate.Find("Img").GetComponent<Image>().sprite;
        ClassDescription.text = classDefinition.Description;
        var skills = classDefinition.GetSkills();
        foreach (var skill in skills)
        {
            var skillPreview = UnityEngine.Object.Instantiate(SkillPreviewElement, SkillPreviewTransform);
            skillPreview.name = skill.Value.Name;
            skillPreview.transform.Find("Icon").GetComponent<Image>().sprite = skill.Value.Icon;
            if (skill.Value.Video != null)
            {
                skillPreview.GetComponent<UIInputHandler>().m_onLeftClick = _ =>
                {
                    if (SkillVideo.gameObject.activeSelf) return;
                    LOADING.gameObject.SetActive(true);
                    SkillVideo.gameObject.SetActive(true);
                    SkillVideo.url = skill.Value.Video;
                    SkillVideo.Play();
                };
            }

            skillPreview.GetComponent<UIInputHandler>().m_onPointerEnter = _ =>
            {
                LOADING.gameObject.SetActive(false);
                SkillDescription.text = skill.Value.Description;
                if (skill.Value.Video != null)
                    SkillDescription.text += "\n\n<color=lime>Click to watch video</color>";
            };
            skillPreview.GetComponent<UIInputHandler>().m_onPointerExit = _ =>
            {
                LOADING.gameObject.SetActive(false);
                SkillVideo.gameObject.SetActive(false);
                SkillVideo.Stop();
                ((RenderTexture)SkillVideo.gameObject.GetComponent<RawImage>().texture).Release();
                SkillDescription.text = "";
            };
        }
    }

    public static void Show()
    {
        Default();
        var toChoose = ClassManager.CurrentClass == Class.None ? Class.Mage : ClassManager.CurrentClass;
        if (IsMale)
        {
            MaleClasses.gameObject.SetActive(true);
            SelectClass(toChoose, MaleClasses.Find(toChoose.ToString()));
        }
        else
        {
            FemaleClasses.gameObject.SetActive(true);
            SelectClass(toChoose, FemaleClasses.Find(toChoose.ToString()));
        }

        UI.SetActive(true);
    }

    public static void Hide()
    {
        SkillVideo.Stop();
        UI.SetActive(false);
    }
}