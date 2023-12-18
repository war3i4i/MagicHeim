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
            RectTransform root = (Minimap.instance.m_mode == Minimap.MapMode.Large) ? Minimap.instance.m_pinNameRootLarge : Minimap.instance.m_pinNameRootSmall;
            pin.m_NamePinData = new Minimap.PinNameData(pin);
            Minimap.instance.CreateMapNamePin(pin, root);
            pin.m_NamePinData.PinNameText.richText = true;
            pin.m_NamePinData.PinNameText.overrideColorTags = false;
            Minimap.instance?.m_pins?.Add(pin);
        }

        private void LateUpdate()
        {
            pin.m_checked = false; 
            pin.m_pos = transform.position;
        }

        private void OnDestroy()
        {
            if (pin.m_uiElement) Minimap.instance.DestroyPinMarker(pin);
            Minimap.instance?.m_pins?.Remove(pin);
        }
    }

    public static void RegisterCustomPin(GameObject go, string name, Sprite icon)
    {
        CustomPinhandler comp = go.AddComponent<CustomPinhandler>();
        comp.pinName = name;
        comp.icon = icon;
    }
    
}