namespace MagicHeim;

public static class CustomMapPins
{
    public class CustomPinhandler : MonoBehaviour
    {
        public Sprite icon;
        public string pinName;
        private Minimap.PinData pin;

        private void Awake()
        {
            pin = new Minimap.PinData();
            pin.m_type = Minimap.PinType.Icon0;
            pin.m_name = Localization.instance.Localize(pinName);
            pin.m_pos = transform.position;
            pin.m_icon = icon;
            pin.m_save = false;
            pin.m_checked = false;
            pin.m_ownerID = 0;
            Minimap.instance?.m_pins?.Add(pin);
        }

        private void LateUpdate()
        {
            pin.m_checked = false;
            pin.m_pos = transform.position;
        }

        private void OnDestroy()
        {
            if (pin.m_uiElement) Destroy(pin.m_uiElement.gameObject);
            Minimap.instance?.m_pins?.Remove(pin);
        }
    }

    public static void RegisterCustomPin(GameObject go, string name, Sprite icon)
    {
        var comp = go.AddComponent<CustomPinhandler>();
        comp.pinName = name;
        comp.icon = icon;
    }


    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Awake))]
    static class Minimap_Awake_Patch
    {
        static void Postfix(Minimap __instance)
        {
            __instance.m_pinPrefab.transform.Find("Name").GetComponent<Text>().supportRichText = true;
        }
    }
}