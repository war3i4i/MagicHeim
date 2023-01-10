using MagicHeim.UI_s;

namespace MagicHeim;

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
static class ZNetScene_Awake_Patch
{
    static void Postfix(ZNetScene __instance)
    { 
        __instance.m_namedPrefabs[MagicHeim.MH_Altar.name.GetStableHashCode()] = MagicHeim.MH_Altar;
        var hammer = __instance.GetPrefab("Hammer").GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces;
        if (!hammer.Contains(MagicHeim.MH_Altar)) hammer.Add(MagicHeim.MH_Altar);
    }
} 

public class MH_Altar : MonoBehaviour, Interactable, Hoverable 
{ 
    public static List<MH_Altar> m_allAltars = new List<MH_Altar>();

    private ZNetView _znv; 

    private void Awake() 
    {
        _znv = GetComponent<ZNetView>();
        if (!_znv.IsValid()) return; 
        transform.Find("model").GetComponent<BoxCollider>().enabled = false;
        transform.Find("model").GetComponent<MeshCollider>().enabled = true;
        m_allAltars.Add(this);
    } 
 
    private void OnDestroy()
    { 
        m_allAltars.Remove(this);
    }

    public static bool IsClose(Vector3 point)
    {
        return m_allAltars.Any(altar => Vector3.Distance(point, altar.transform.position) <= 4f);
    }


    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (Player.m_localPlayer?.GetHoverObject() is { } go && go.name == "BallAB")
        {
            ClassSelectionUI.Show();
            return true;
        }
        return false;
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return false;
    }

    public string GetHoverText()
    {
        if (Player.m_localPlayer?.GetHoverObject() is { } go && go.name == "BallAB")
            return Localization.instance.Localize($"[<color=yellow><b>$KEY_Use</b></color>] Use Altar");
        return "";
    }

    public string GetHoverName()
    {
        return "MH_Altar";
    }
}

[HarmonyPatch(typeof(Player),nameof(Player.PlacePiece))]
static class Player_PlacePiece_Patch
{
    static bool Prefix(Player __instance, Piece piece) 
    {
        if (piece.name == "MH_Altar" && !Player.m_debugMode)
        {
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,"Debug Mode Please");
            return false;
        }

        return true;
    }
}