using ItemManager;
using JetBrains.Annotations;
using MagicHeim.MH_Enums;
using Random = UnityEngine.Random;

namespace MagicHeim;

public static class MagicTomes
{
    private static ConfigEntry<float> DropChance;
    private static ConfigEntry<float> DropChance_Bosses;

    private static readonly Dictionary<GameObject, ConfigEntry<int>> MagicTomeDictionary =
        new Dictionary<GameObject, ConfigEntry<int>>(6);

    private static readonly Dictionary<Heightmap.Biome, GameObject> TomesByBiomes = new(6);

    public static void Init()
    {
        SaveLoadTome.Init();
        Item meadowsTome = new(MagicHeim.asset.LoadAsset<GameObject>("MH_Tome_Meadows")) { Configurable = Configurability.Disabled };
        Item blackForestTome = new(MagicHeim.asset.LoadAsset<GameObject>("MH_Tome_BlackForest")) { Configurable = Configurability.Disabled };
        Item swampsTome = new(MagicHeim.asset.LoadAsset<GameObject>("MH_Tome_Swamps")) { Configurable = Configurability.Disabled };
        Item mountainsTome = new(MagicHeim.asset.LoadAsset<GameObject>("MH_Tome_Mountains")) { Configurable = Configurability.Disabled };
        Item plainsTome = new(MagicHeim.asset.LoadAsset<GameObject>("MH_Tome_Plains")) { Configurable = Configurability.Disabled };
        Item mistlandsTome = new(MagicHeim.asset.LoadAsset<GameObject>("MH_Tome_Mistlands")) { Configurable = Configurability.Disabled };
        
        MagicTomeDictionary.Add(meadowsTome.Prefab, MagicHeim.config("Magic Tomes", "Meadows", 100, "EXP Gain for the Meadows Tome"));
        MagicTomeDictionary.Add(blackForestTome.Prefab, MagicHeim.config("Magic Tomes", "Black Forest", 200, "EXP Gain for the Black Forest Tome"));
        MagicTomeDictionary.Add(swampsTome.Prefab, MagicHeim.config("Magic Tomes", "Swamps", 400, "EXP Gain for the Swamp Tome"));
        MagicTomeDictionary.Add(mountainsTome.Prefab, MagicHeim.config("Magic Tomes", "Mountains", 800, "EXP Gain for the Mountains Tome"));
        MagicTomeDictionary.Add(plainsTome.Prefab, MagicHeim.config("Magic Tomes", "Plains", 1500, "EXP Gain for the Plains Tome")); 
        MagicTomeDictionary.Add(mistlandsTome.Prefab, MagicHeim.config("Magic Tomes", "Mistlands", 3000, "EXP Gain for the Mistlands Tome"));

        DropChance = MagicHeim.config("Magic Tomes", "Drop Chance", 0.5f, "Chance for a Magic Tome to drop from a monster");
        DropChance_Bosses = MagicHeim.config("Magic Tomes", "Drop Chance Bosses", 100f, "Chance for a Magic Tome to drop from a boss");

        TomesByBiomes.Add(Heightmap.Biome.Meadows, meadowsTome.Prefab);
        TomesByBiomes.Add(Heightmap.Biome.BlackForest, blackForestTome.Prefab);
        TomesByBiomes.Add(Heightmap.Biome.Swamp, swampsTome.Prefab);
        TomesByBiomes.Add(Heightmap.Biome.Mountain, mountainsTome.Prefab);
        TomesByBiomes.Add(Heightmap.Biome.Plains, plainsTome.Prefab);
        TomesByBiomes.Add(Heightmap.Biome.Mistlands, mistlandsTome.Prefab);
    }
    
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData),
        typeof(int),
        typeof(bool),
        typeof(float), typeof(int))]
    public class GetTooltipPatch
    {
        public static void Postfix(ItemDrop.ItemData item, bool crafting, ref string __result)
        {
            if (crafting || item == null || !item.m_dropPrefab) return;
            if (MagicTomeDictionary.TryGetValue(item.m_dropPrefab, out ConfigEntry<int> expGain))
            {
                __result = $"Right Mouse Button click to get <color=yellow>{expGain.Value}</color> EXP";
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnRightClickItem))]
    private static class Player_ConsumeItem_PatchDItem
    {
        private static readonly DateTime LastActivation = default;

        [UsedImplicitly]
        private static bool Prefix(InventoryGrid grid, ItemDrop.ItemData item)
        {
            if (ClassManager.CurrentClass == Class.None ||
                grid.m_inventory != Player.m_localPlayer.m_inventory) return true;
            if (item != null && Player.m_localPlayer)
            {
                foreach (KeyValuePair<GameObject, ConfigEntry<int>> chestItem in MagicTomeDictionary)
                {
                    if (item.m_dropPrefab == chestItem.Key)
                    {
                        if ((DateTime.Now - LastActivation).TotalSeconds >= 1f)
                        {
                            grid.m_inventory.RemoveOneItem(item);
                            ClassManager.AddExp(chestItem.Value.Value);
                        }

                        return false;
                    }
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
    static class Tome_SpawnLoot_Patch
    {
        static void DropItem(GameObject prefab, Vector3 centerPos, float dropArea)
        {
            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0, 360), 0f);
            Vector3 b = Random.insideUnitSphere * dropArea;
            GameObject gameObject = UnityEngine.Object.Instantiate(prefab, centerPos + b, rotation);
            Rigidbody component = gameObject.GetComponent<Rigidbody>();
            if (component)
            {
                Vector3 insideUnitSphere = Random.insideUnitSphere;
                if (insideUnitSphere.y < 0f)
                {
                    insideUnitSphere.y = -insideUnitSphere.y;
                }

                component.AddForce(insideUnitSphere * 5f, ForceMode.VelocityChange);
            }
        }

        [UsedImplicitly]
        static void Prefix(Character __instance)
        {
            if (__instance.IsPlayer() || !__instance.m_nview.IsOwner() || __instance.IsTamed()) return;
            Heightmap.Biome biome = EnvMan.instance.m_currentBiome;
            float rand = Random.value;
            float dropChance = __instance.IsBoss() ? DropChance_Bosses.Value : DropChance.Value;
            if (TomesByBiomes.TryGetValue(biome, out GameObject book) && rand <= dropChance / 100f)
            {
                if (__instance.IsBoss())
                {
                    for (int i = 0; i < Random.Range(1, 4); i++)
                    {
                        DropItem(book, __instance.transform.position + Vector3.up * 0.75f, 0.5f);
                    }
                }
                else
                {
                    DropItem(book, __instance.transform.position + Vector3.up * 0.75f, 0.5f);
                }
            }
        }
    }
}