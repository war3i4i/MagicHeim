using MagicHeim.MH_Interfaces;

namespace MagicHeim.UI_s;

public class ChargeData
{
    public float _maxTime;
    public GameObject _go;
    public Image _toFill;
    public float _time;
}

public static class SkillChargeUI
{
    static SkillChargeUI()
    {
        Init();
    }

    private static GameObject UI;
    private static GameObject Element;
    private static Transform Content;
    private static readonly Dictionary<int, ChargeData> Data = new();
    private static Coroutine _routine;
    public static bool IsCharging => Data.Count > 0;

    private static void SetupElement(GameObject go, MH_Skill skill, float maxTime)
    { 
        go.transform.Find("Icon").GetComponent<Image>().sprite = skill.Icon;
        go.transform.Find("Text").GetComponent<Text>().text = Localization.instance.Localize(skill.Name);
        ChargeData _cData = new()
        {
            _go = go,
            _toFill = go.transform.Find("Fill").GetComponent<Image>(),
            _maxTime = maxTime,
            _time = 0
        };
        _cData._toFill.color = skill.SkillColor;
        _cData._toFill.fillAmount = 0;
        Data[skill.Key] = _cData;
        if (_routine != null) MagicHeim._thistype.StopCoroutine(_routine);
        _routine = MagicHeim._thistype.StartCoroutine(UpdateCharges());
    }

    private static IEnumerator UpdateCharges()
    {
        List<int> toRemove = new();
        while (Data.Count > 0)
        {
            float dt = Time.deltaTime;
            foreach (KeyValuePair<int, ChargeData> data in Data)
            {
                data.Value._time += dt;
                data.Value._toFill.fillAmount = data.Value._time / data.Value._maxTime;
                if (Player.m_localPlayer && data.Value._time < data.Value._maxTime) continue;
                UnityEngine.Object.Destroy(data.Value._go);
                toRemove.Add(data.Key);
            }

            toRemove.ForEach(x => Data.Remove(x));
            toRemove.Clear();
            yield return null;
        }
    }

    private static void Init()
    {
        UI = UnityEngine.Object.Instantiate(MagicHeim.asset.LoadAsset<GameObject>("SkillChargeUI"));
        UnityEngine.Object.DontDestroyOnLoad(UI);
        UI.SetActive(true);
        Element = MagicHeim.asset.LoadAsset<GameObject>("SkillChargeElement");
        Content = UI.transform.Find("Canvas/Content");
        UI.name = "SkillChargeUI";
    }

    public static void ShowCharge(MH_Skill skill, float maxTime = 99999999f)
    {
        if (maxTime <= 0 || Data.ContainsKey(skill.Key)) return;
        GameObject go = UnityEngine.Object.Instantiate(Element, Content);
        SetupElement(go, skill, maxTime);
    }

    public static void RemoveCharge(MH_Skill skill)
    {
        if (!Data.ContainsKey(skill.Key)) return;
        UnityEngine.Object.Destroy(Data[skill.Key]._go);
        Data.Remove(skill.Key);
    }

    private static void RemoveAllCharges()
    {
        foreach (KeyValuePair<int, ChargeData> data in Data)
        {
            UnityEngine.Object.Destroy(data.Value._go);
        }

        Data.Clear();
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
    static class Player_OnDeath_Patch
    {
        static void Postfix()
        {
            RemoveAllCharges();
        }
    }
}