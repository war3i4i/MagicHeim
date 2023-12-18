using ItemDataManager;
using ItemManager;
using JetBrains.Annotations;
using MagicHeim.MH_Enums;
using MagicHeim.UI_s;

namespace MagicHeim;

public static class SaveLoadTome
{
    public static Item MH_SaveLoad_Item;

    public static void Init()
    {
        MH_SaveLoad_Item = new(MagicHeim.asset.LoadAsset<GameObject>("MH_Sphere_SaveLoad")) { Configurable = Configurability.Disabled };
        MH_SaveLoad_Item.Prefab.GetComponent<ItemDrop>().m_itemData.Data().GetOrCreate<MH_SaveLoad>().Save();
    }

    private class MH_SaveLoad : ItemData
    {
        [SerializeField] public string OwnerName = "";
        [SerializeField] public long OwnerID = 0;
        [SerializeField] public bool Initialized;
        [SerializeField] public ClassManager.SaveLoad Data = new();

        public void AssignData(string ownerName, long ownerID, ClassManager.SaveLoad data)
        {
            Data = data;
            OwnerName = ownerName;
            OwnerID = ownerID;
            Save();
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float))]
    public class TooltipPatch
    {
        [UsedImplicitly]
        public static void Postfix(ItemDrop.ItemData item, bool crafting, int qualityLevel, ref string __result)
        {
            if (item.Data().Get<MH_SaveLoad>() is {} sl)
            {
                if (!sl.Initialized)
                {
                    __result = "<color=#00FFFF>Right click to save your memories inside sphere</color>\n" +
                                "<color=yellow>It will reset your class level / exp / skills and save that into sphere so you can return it later</color>";
                }
                else
                {
                    var classDef = ClassesDatabase.ClassesDatabase.GetClassDefinition(sl.Data.Class);
                    var color = classDef.GetColor;
                    string colorStr = ColorUtility.ToHtmlStringRGB(color);
                    __result = $"<color=#00FFFF>This sphere contains memories of <color=#{colorStr}>{sl.Data.Class}</color> class that belongs to <color=green>{sl.OwnerName}</color></color>\n" 
                               + $"<color=yellow>Right click to earn memories from sphere</color>";

                    __result += $"\n\nSkills:"; 
                    
                    var skills = sl.Data.SkillsData.Split(';');
                    foreach (var s in skills)
                    {
                        string[] split2 = s.Split(':');
                        if (split2.Length < 2) continue;  
                        int skillID = int.Parse(split2[0]);
                        int skillLevel = int.Parse(split2[1]);
                        var skill = SkillsDatabase.SkillsDatabase.TryGetSkillDefinition(skillID, out var def) ? def : null;
                        if (skill == null) continue;
                        var skillColor = ColorUtility.ToHtmlStringRGB(skill.SkillColor);
                        __result += $"\n<color=#{skillColor}>{skill.Name}</color>: {skillLevel}";
                    }
                }
            } 
        } 
    }


    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnRightClickItem))]
    private static class Player_ConsumeItem_PatchDItem
    {
        [UsedImplicitly]
        private static bool Prefix(InventoryGrid grid, ItemDrop.ItemData item)
        {
            if (ClassManager.CurrentClass == Class.None || grid.m_inventory != Player.m_localPlayer?.m_inventory) return true;
            if (item != null && Player.m_localPlayer)
            {
                if (item.Data().Get<MH_SaveLoad>() is { } saveload)
                {
                    if (ClassManager.CurrentClassDef != null && !ClassManager.CurrentClassDef.CanChangeClass())
                    {
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "You can't use memories sphere right now");
                        return false;
                    }

                    var currentClass = ClassManager.CurrentClass;
                    if (!saveload.Initialized)
                    {
                        ConfirmationUI.Show("Use sphere of memories", "Are you sure wanna write your memories inside sphere? It will drop your class level / exp / skills and save that into tome so you can activate it later", () =>
                        {
                            if (!Player.m_localPlayer.m_inventory.ContainsItem(saveload.Item)) return;
                            if (ClassManager.CurrentClass != Class.None && ClassManager.CurrentClassDef != null && !ClassManager.CurrentClassDef.CanChangeClass()) return;
                            saveload.Initialized = true;
                            saveload.AssignData(Game.instance.m_playerProfile.m_playerName, Game.instance.m_playerProfile.m_playerID, ClassManager.GetSaveData());
                            ClassManager.SetClass(currentClass);
                            UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_Potion_frostresist"), Player.m_localPlayer.transform.position, Quaternion.identity);
                        });
                    }
                    else
                    {
                        if (saveload.OwnerID != Game.instance.m_playerProfile.m_playerID)
                        {
                            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "You can't use memories sphere that belongs to other player");
                            return false;
                        }
                        ConfirmationUI.Show("Use sphere of memories", "Are you sure wanna earn memories from sphere? It will change your current class and you will lose all progress", () =>
                        {
                            if (!Player.m_localPlayer.m_inventory.ContainsItem(saveload.Item)) return;
                            if (ClassManager.CurrentClassDef != null && !ClassManager.CurrentClassDef.CanChangeClass()) return;
                            ClassManager.Load(saveload.Data);
                            UnityEngine.Object.Instantiate(ClassManager.CurrentClassDef!.OnSelect_VFX, Player.m_localPlayer.transform.position, Quaternion.identity);
                            Player.m_localPlayer.m_inventory.RemoveItem(saveload.Item);
                        });
                    }

                    return false;
                }
            }

            return true;
        }
    }
}