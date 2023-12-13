namespace MagicHeim.UI_s;

public static class ConfirmationUI
{
    static ConfirmationUI()
    {
        Init();
    }

    private static GameObject UI;
    private static Text Name;
    private static Text Description;
    private static Action OnConfirm;

    public static bool IsVisible() => UI && UI.activeSelf;

    private static void Init()
    {
        UI = UnityEngine.Object.Instantiate(MagicHeim.asset.LoadAsset<GameObject>("ConfirmationUI"));
        UnityEngine.Object.DontDestroyOnLoad(UI);
        UI.SetActive(false);
        Name = UI.transform.Find("Canvas/MainTab/Name").GetComponent<Text>();
        Description = UI.transform.Find("Canvas/MainTab/Description").GetComponent<Text>();
        UI.transform.Find("Canvas/MainTab/Confirm").GetComponent<Button>().onClick.AddListener(() => Confirm());
        UI.transform.Find("Canvas/MainTab/Cancel").GetComponent<Button>().onClick
            .AddListener(() =>
            {
                ClassSelectionUI.AUsrc.Play();
                Hide();
            });
    }

    private static void Confirm()
    {
        ClassSelectionUI.AUsrc.Play();
        OnConfirm?.Invoke();
        OnConfirm = null;
        UI.SetActive(false);
    }

    public static void Hide()
    {
        UI.SetActive(false);
        OnConfirm = null;
    }

    public static void Show(string name, string description, Action onConfirm)
    {
        Name.text = name;
        Description.text = description;
        OnConfirm = onConfirm;
        UI.SetActive(true);
        UpdateCanvases();

        RectTransform rect = UI.transform.Find("Canvas/MainTab").GetComponent<RectTransform>();
        float RectY = rect.sizeDelta.y;
        rect.anchoredPosition = new Vector2(0, RectY / 2f);
    }

    private static void UpdateCanvases()
    {
        Canvas.ForceUpdateCanvases();
        var sizeFillers = UI.GetComponentsInChildren<ContentSizeFitter>().ToList();
        sizeFillers.ForEach(filter => filter.enabled = false);
        sizeFillers.ForEach(filter => filter.enabled = true);
    }

    [HarmonyPatch(typeof(TextInput), nameof(TextInput.IsVisible))]
    static class Menu_IsVisible_Patch
    {
        static void Postfix(ref bool __result)
        {
            __result |= IsVisible();
        }
    }
    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.IsVisible))]
    static class Menu_IsVisible_Patch2
    {
        static void Postfix(ref bool __result)
        {
            __result |= IsVisible();
        }
    }
}