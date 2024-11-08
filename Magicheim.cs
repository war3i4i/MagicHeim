﻿using JetBrains.Annotations;
using LocalizationManager;
using MagicHeim.AnimationHelpers;
using MagicHeim.UI_s;

namespace MagicHeim
{
    [BepInPlugin(GUID, PluginName, PluginVersion)]
    [BepInDependency("org.bepinex.plugins.groups", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInIncompatibility("org.bepinex.plugins.valheim_plus")]
    public class MagicHeim : BaseUnityPlugin
    {
        private const string GUID = "kg.magicheim";
        private const string PluginName = "MagicHeim";
        private const string PluginVersion = "1.3.3";
        public static MagicHeim _thistype;
        private readonly ConfigSync configSync = new(GUID) { DisplayName = PluginName, MinimumRequiredVersion = PluginVersion, CurrentVersion = PluginVersion, ModRequired = true, IsLocked = true};
        public static AssetBundle asset;
        public static AssetBundle asset_addition;
        public static GameObject MH_Altar;
        public static ConfigFile MH_SyncedConfig;
        private static FileSystemWatcher FSW;  
  
        private void Awake()  
        {
            Localizer.Load(); 
            _thistype = this; 
            MH_SyncedConfig = new ConfigFile(Path.Combine(Paths.ConfigPath, "kg.magicheim_synced.cfg"), true);
            Stream stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("MagicHeim.Asset.MagicHeimUnityCode.dll");
            byte[] buffer = new byte[stream!.Length];
            stream.Read(buffer, 0, buffer.Length);
            try
            {
                Assembly.Load(buffer);
            }
            catch
            {
                // ignored
            }

            asset = GetAssetBundle("magicheim");
            asset_addition = GetAssetBundle("mh_addition");
            SkillsDatabase.SkillsDatabase.Init();
            ClassesDatabase.ClassesDatabase.Init();
            ClassSelectionUI.Init();
            SkillPanelUI.Init();
            SkillBookUI.Init();
            MagicTomes.Init();
            ClassAnimationReplace.InitAnimations();
            MH_Altar = asset.LoadAsset<GameObject>("MH_Altar");
            CustomMapPins.RegisterCustomPin(MH_Altar, MH_Altar.GetComponent<Piece>().m_name, MH_Altar.GetComponent<Piece>().m_icon);
            MH_Altar.AddComponent<MH_Altar>();
            Exp_Configs.Init();

            FSW = new FileSystemWatcher(Paths.ConfigPath)
            {
                Filter = Path.GetFileName(MH_SyncedConfig.ConfigFilePath),
                EnableRaisingEvents = true,
                IncludeSubdirectories = false,
                SynchronizingObject = ThreadingHelper.SynchronizingObject
            };
            FSW.Changed += ConfigChanged;
 
            new Harmony(GUID).PatchAll();
        }


        private void ConfigChanged(object sender, FileSystemEventArgs e)
        {
            if (!Game.instance || !ZNet.instance || !ZNet.instance.IsServer()) return;
            if (Path.GetFileName(e.Name) != "kg.magicheim_synced.cfg") return;
            MagicHeim_Logger.Logger.Log("Reloading Config");
            DelayedReload(MH_SyncedConfig);
        }
        
        /*[HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Start))]
        private static class FejdStartup_Awake_Patch
        {
            [UsedImplicitly]
            private static void Postfix(FejdStartup __instance)
            {
                List<World> mWorlds = SaveSystem.GetWorldList();
                string name = PlayerPrefs.GetString("world");
                World world = mWorlds.FirstOrDefault(x => x.m_name == name);
                if (world == null) return;
                string playerProfile = PlayerPrefs.GetString("profile");
                if (string.IsNullOrWhiteSpace(playerProfile)) return;
                ZSteamMatchmaking.instance.StopServerListing();
                ZNet.m_onlineBackend = OnlineBackendType.Steamworks;
                Game.SetProfile(playerProfile, FileHelpers.FileSource.Auto);
                ZNet.SetServer(true, false, false, name, "", world);
                __instance.LoadMainScene();
            }
        }*/
        
        

        private static IEnumerator DelayedReloadRoutine(ConfigFile config)
        {
            yield return new WaitForSecondsRealtime(3f);
            config.Reload();
        }

        private static void DelayedReload(ConfigFile config)
        {
            if (config != null)
            {
                _thistype.StartCoroutine(DelayedReloadRoutine(config));
            }
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = MH_SyncedConfig.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        public static ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true) =>
            _thistype.config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        

        private static AssetBundle GetAssetBundle(string filename)
        {
            Assembly execAssembly = Assembly.GetExecutingAssembly(); 
            string resourceName = execAssembly.GetManifestResourceNames().Single(str => str.EndsWith(filename));
            using Stream stream = execAssembly.GetManifestResourceStream(resourceName);
            return AssetBundle.LoadFromStream(stream);
        }
        
        
        private void Update()
        {
            TurnOffUIs();
            if (OptionsUI.CurrentChoosenButton >= 0)
            {
                foreach (KeyCode key in (KeyCode[])Enum.GetValues(typeof(KeyCode)))
                { 
                    if (!Input.GetKeyDown(key)) continue;
                    OptionsUI.ButtonPressed(key);
                    break;
                }
            }

            if (OptionsUI.CurrentChoosenButtonAdditional >= 0) 
            {
                foreach (KeyCode key in (KeyCode[])Enum.GetValues(typeof(KeyCode)))
                {
                    if (!Input.GetKeyDown(key)) continue;
                    OptionsUI.ButtonPressedAdditional(key);
                    break;
                }
            } 

            if (!Player.m_localPlayer) return;
            if (Input.GetKeyDown(SkillPanelUI.MH_Hotkeys[20].Value))
            {
                if (!SkillBookUI.IsVisible())
                {
                    if (Player.m_localPlayer.TakeInput())
                        SkillBookUI.Show();
                }
                else
                {
                    SkillBookUI.Hide();
                }
            }
        }

        private void TurnOffUIs()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            if (ClassSelectionUI.IsVisible())
            {
                ClassSelectionUI.Hide();
                Menu.instance?.OnClose();
            }

            if (SkillBookUI.IsVisible())
            {
                SkillBookUI.Hide();
                Menu.instance?.OnClose();
            }

            if (ConfirmationUI.IsVisible())
            {
                ConfirmationUI.Hide(); 
                Menu.instance?.OnClose();
            }
        } 
        
        [HarmonyPatch(typeof(AudioMan), nameof(AudioMan.Awake))]
        private static class AudioMan_Awake_Patch
        { 
            private static void Postfix(AudioMan __instance)
            {
                var SFXgroup = __instance.m_masterMixer.FindMatchingGroups("SFX")[0];
                ClassSelectionUI.AUsrc = Chainloader.ManagerObject.AddComponent<AudioSource>();
                ClassSelectionUI.AUsrc.clip = asset.LoadAsset<AudioClip>("MH_Click");
                ClassSelectionUI.AUsrc.reverbZoneMix = 0;
                ClassSelectionUI.AUsrc.spatialBlend = 0;
                ClassSelectionUI.AUsrc.bypassListenerEffects = true; 
                ClassSelectionUI.AUsrc.bypassEffects = true; 
                ClassSelectionUI.AUsrc.volume = 0.8f;
                ClassSelectionUI.AUsrc.outputAudioMixerGroup = SFXgroup;
                foreach (GameObject allAsset in asset.LoadAllAssets<GameObject>())
                {
                    if(allAsset.GetComponentInChildren<ZSFX>()) MagicHeim_Logger.Logger.Log($"Found {allAsset.name} with zsfx");

                    foreach (AudioSource audioSource in allAsset.GetComponentsInChildren<AudioSource>(true))
                    {
                        audioSource.outputAudioMixerGroup = SFXgroup;
                    }
                }
            }
        }
        
    }
}