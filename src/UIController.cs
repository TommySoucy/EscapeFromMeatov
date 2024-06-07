using FistVR;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    public abstract class UIController : MonoBehaviour
    {
        [NonSerialized]
        public bool init;

        [NonSerialized]
        public static List<int> availableSaveFiles;

        public static int meatovTimeMultiplier = 7;
        public static JObject loadedData;

        public static float loadingHideoutAVGProgress;
        public static bool loadingHideoutAssets;
        public static bool loadingHideoutLatest;
        public static int loadingHideoutSlotIndex;

        public virtual void Awake()
        {
            FetchAvailableSaveFiles();

            InitUI();

            init = true;
        }

        public virtual void Update()
        {
            if (loadingHideoutAssets)
            {
                loadingHideoutAVGProgress = 0;
                loadingHideoutAVGProgress += Mod.playerBundleRequest.progress;
                for(int i=0; i < Mod.itemsBundlesRequests.Length; ++i)
                {
                    loadingHideoutAVGProgress += Mod.itemsBundlesRequests[i].progress;
                }
                for(int i=0; i < Mod.hideoutAreaBundleRequests.Length; ++i)
                {
                    loadingHideoutAVGProgress += Mod.hideoutAreaBundleRequests[i].progress;
                }
                loadingHideoutAVGProgress += Mod.itemIconsBundleRequest.progress;
                loadingHideoutAVGProgress += Mod.hideoutBundleRequest.progress;
                loadingHideoutAVGProgress += Mod.hideoutAssetsBundleRequest.progress;

                loadingHideoutAVGProgress /= 4 + Mod.itemsBundlesRequests.Length + Mod.hideoutAreaBundleRequests.Length;

                // Check if they are done loading
                bool doneLoadingItems = true;
                for (int i = 0; i < Mod.itemsBundlesRequests.Length; ++i)
                {
                    doneLoadingItems &= Mod.itemsBundlesRequests[i].isDone;
                }
                bool doneLoadingAreas = true;
                for (int i = 0; i < Mod.hideoutAreaBundleRequests.Length; ++i)
                {
                    doneLoadingAreas &= Mod.hideoutAreaBundleRequests[i].isDone;
                }
                if (doneLoadingItems 
                    && doneLoadingAreas
                    && Mod.playerBundleRequest.isDone
                    && Mod.itemIconsBundleRequest.isDone
                    && Mod.hideoutBundleRequest.isDone)
                {
                    Mod.playerBundle = Mod.playerBundleRequest.assetBundle;
                    for (int i = 0; i < Mod.itemsBundles.Length; ++i)
                    {
                        Mod.itemsBundles[i] = Mod.itemsBundlesRequests[i].assetBundle;
                    }
                    Mod.itemIconsBundle = Mod.itemIconsBundleRequest.assetBundle;
                    Mod.hideoutBundle = Mod.hideoutBundleRequest.assetBundle;
                    Mod.hideoutAssetsBundle = Mod.hideoutAssetsBundleRequest.assetBundle;
                    for (int i = 0; i < Mod.hideoutAreaBundles.Length; ++i)
                    {
                        Mod.hideoutAreaBundles[i] = Mod.hideoutAreaBundleRequests[i].assetBundle;
                    }

                    LoadHideout(loadingHideoutSlotIndex, loadingHideoutLatest);
                }
            }
        }

        // This should be called everytime we save because there may be a new save available
        public void FetchAvailableSaveFiles()
        {
            if(availableSaveFiles == null)
            {
                availableSaveFiles = new List<int>();
            }
            else
            {
                availableSaveFiles.Clear();
            }
            string[] allFiles = Directory.GetFiles(Mod.path + "/Saves");
            foreach (string path in allFiles)
            {
                if (path.EndsWith(".sav")) // If .sav is present as the last part of the path
                {
                    if(int.TryParse(path[path.Length - 5].ToString(), out int parseResult))
                    {
                        availableSaveFiles.Add(parseResult);
                    }
                    else // AutoSave.sav
                    {
                        availableSaveFiles.Add(5);
                    }
                }
            }
        }

        public static void LoadHideout(int slotIndex = -1, bool latest = false)
        {
            // Load necessary assets
            if (Mod.hideoutBundle == null) // Need to load asset bundles
            {
                loadingHideoutAssets = true;
                loadingHideoutSlotIndex = slotIndex;
                loadingHideoutLatest = latest;

                Mod.playerBundleRequest = AssetBundle.LoadFromFileAsync(Mod.path + "/Assets/EFMPlayer.ab");
                Mod.itemsBundlesRequests = new AssetBundleCreateRequest[3];
                Mod.itemsBundles = new AssetBundle[3];
                for (int i = 0; i < 3; ++i) 
                {
                    Mod.itemsBundlesRequests[i] = AssetBundle.LoadFromFileAsync(Mod.path + "/Assets/EFMItems" + i + ".ab");
                }
                Mod.itemIconsBundleRequest = AssetBundle.LoadFromFileAsync(Mod.path + "/Assets/EFMItemIcons.ab");
                Mod.hideoutBundleRequest = AssetBundle.LoadFromFileAsync(Mod.path + "/Assets/EFMHideout.ab");
                Mod.hideoutAssetsBundleRequest = AssetBundle.LoadFromFileAsync(Mod.path + "/Assets/EFMHideoutAssets.ab");
                Mod.hideoutAreaBundleRequests = new AssetBundleCreateRequest[4];
                Mod.hideoutAreaBundles = new AssetBundle[4];
                for(int i=0; i< Mod.hideoutAreaBundleRequests.Length; ++i)
                {
                    Mod.hideoutAreaBundleRequests[i] = AssetBundle.LoadFromFileAsync(Mod.path + "/Assets/EFMHideoutAreas"+i+".ab");
                }
                return;
            }
            else // Asset bundles loaded
            {
                if(Mod.playerBundleRequest != null) // Request still exists meaning we haven't loaded the assets
                {
                    loadingHideoutAssets = false;

                    Mod.playerStatusUIPrefab = Mod.playerBundle.LoadAsset<GameObject>("StatusUI");
                    Mod.staminaBarPrefab = Mod.playerBundle.LoadAsset<GameObject>("StaminaBar");
                    Mod.consumeUIPrefab = Mod.playerBundle.LoadAsset<GameObject>("ConsumeUI");
                    Mod.stackSplitUIPrefab = Mod.playerBundle.LoadAsset<GameObject>("StackSplitUI");
                    Mod.extractionUIPrefab = Mod.playerBundle.LoadAsset<GameObject>("ExtractionUI");
                    Mod.extractionLimitUIPrefab = Mod.playerBundle.LoadAsset<GameObject>("ExtractionLimitUI");
                    Mod.itemDescriptionUIPrefab = Mod.playerBundle.LoadAsset<GameObject>("ItemDescriptionUI");
                    Mod.devItemSpawnerPrefab = Mod.playerBundle.LoadAsset<GameObject>("DevItemSpawner");

                    Mod.quickbeltConfigurationIndices = new Dictionary<int, int>();
                    Mod.pocketsConfigIndex = GM.Instance.QuickbeltConfigurations.Length;
                    GM.Instance.QuickbeltConfigurations = GM.Instance.QuickbeltConfigurations.AddToArray(Mod.playerBundle.LoadAsset<GameObject>("PocketsConfiguration"));

                    Mod.questionMarkIcon = Mod.itemIconsBundle.LoadAsset<Sprite>("QuestionMarkIcon");
                    Mod.emptyCellIcon = Mod.itemIconsBundle.LoadAsset<Sprite>("cell_full_border");

                    HideoutController.areaCanvasPrefab = Mod.hideoutAssetsBundle.LoadAsset<GameObject>("AreaCanvas");

                    Mod.playerBundleRequest = null;
                    Mod.itemsBundlesRequests = null;
                    Mod.itemIconsBundleRequest = null;
                    Mod.hideoutBundleRequest = null;
                    Mod.hideoutAreaBundleRequests = null;
                }
                // else
                // Already have the bundles loaded and already cleared requests
                // So all assets are already loaded and we can continue with loading
                // This would be the case if we load a save from the hideout itself
            }

            if (availableSaveFiles == null)
            {
                availableSaveFiles = new List<int>();
            }

            // Get save data
            loadedData = null;
            if (slotIndex == -1)
            {
                if (latest)
                {
                    long currentLatestTime = 0;
                    for (int i = 0; i < availableSaveFiles.Count; ++i)
                    {
                        int fileIndex = availableSaveFiles[i];
                        JObject current = JObject.Parse(File.ReadAllText(Mod.path + "/Saves/" + (fileIndex == 5 ? "AutoSave" : "Slot" + fileIndex) + ".sav"));
                        long saveTime = (long)current["time"];
                        if (saveTime > currentLatestTime)
                        {
                            currentLatestTime = saveTime;
                            loadedData = current;
                            Mod.saveSlotIndex = i;
                        }
                    }
                }
                else // New game, use default save
                {
                    loadedData = JObject.Parse(File.ReadAllText(Mod.path + "/Saves/DefaultSave.sav"));
                }
            }
            else
            {
                loadedData = JObject.Parse(File.ReadAllText(Mod.path + "/Saves/" + (slotIndex == 5 ? "AutoSave" : "Slot" + slotIndex) + ".sav"));
                Mod.saveSlotIndex = slotIndex;
            }

            string[] bundledScenes = Mod.hideoutBundle.GetAllScenePaths();
            Mod.LogInfo("Got " + bundledScenes.Length + " bundled scenes in hideout ab");
            for (int i = 0; i < bundledScenes.Length; ++i)
            {
                Mod.LogInfo(i.ToString() + " : " + bundledScenes[i]);
            }
            SteamVR_LoadLevel.Begin("MeatovHideout", false, 0.5f, 0f, 0f, 0f, 1f);
        }

        public abstract void InitUI();
    }
}
