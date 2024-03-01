using System;
using System.Collections.Generic;
using System.IO;
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

        public virtual void Awake()
        {
            FetchAvailableSaveFiles();

            InitUI();

            init = true;
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
            Mod.LogInfo("LoadHideout called");

            // Load necessary assets
            if (Mod.hideoutBundle == null)
            {
                Mod.LogInfo("Loading main asset bundles");
                Mod.playerBundle = AssetBundle.LoadFromFile(Mod.path + "/Assets/EFMPlayer.ab");
                Mod.itemsBundles = new AssetBundle[3];
                for (int i = 0; i < 3; ++i) 
                {
                    Mod.itemsBundles[i] = AssetBundle.LoadFromFile(Mod.path + "/Assets/EFMItems" + i + ".ab");
                }
                Mod.itemIconsBundle = AssetBundle.LoadFromFile(Mod.path + "/Assets/EFMItemIcons.ab");
                Mod.itemSoundsBundle = AssetBundle.LoadFromFile(Mod.path + "/Assets/EFMItemSounds.ab");
                Mod.LogInfo("Loading hideout bundle");
                Mod.hideoutBundle = AssetBundle.LoadFromFile(Mod.path + "/Assets/EFMHideout.ab");
                Mod.LogInfo("Loaded hideout bundles");

                Mod.playerStatusUIPrefab = Mod.playerBundle.LoadAsset<GameObject>("StatusUI");
                Mod.staminaBarPrefab = Mod.playerBundle.LoadAsset<GameObject>("StaminaBar");
                Mod.consumeUIPrefab = Mod.playerBundle.LoadAsset<GameObject>("ConsumeUI");
                Mod.stackSplitUIPrefab = Mod.playerBundle.LoadAsset<GameObject>("StackSplitUI");
                Mod.extractionUIPrefab = Mod.playerBundle.LoadAsset<GameObject>("ExtractionUI");
                Mod.extractionLimitUIPrefab = Mod.playerBundle.LoadAsset<GameObject>("ExtractionLimitUI");
                Mod.itemDescriptionUIPrefab = Mod.playerBundle.LoadAsset<GameObject>("ItemDescriptionUI");
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
                // else new game, loadedData = null
            }
            else
            {
                loadedData = JObject.Parse(File.ReadAllText(Mod.path + "/Saves/" + (slotIndex == 5 ? "AutoSave" : "Slot" + slotIndex) + ".sav"));
                Mod.saveSlotIndex = slotIndex;
            }

            SteamVR_LoadLevel.Begin("MeatovHideout", false, 0.5f, 0f, 0f, 0f, 1f);
        }

        public abstract void InitUI();
    }
}
