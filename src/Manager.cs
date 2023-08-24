using FistVR;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    public abstract class Manager : MonoBehaviour
    {
        public bool init;

        public static bool hideoutLoaded;
        public static int meatovTimeMultiplier = 7;
        public static List<int> availableSaveFiles;

        public static JObject loadedData;

        public virtual void Init()
        {
            FetchAvailableSaveFiles();

            InitUI();

            init = true;
        }

        // This should be called everytime we save because there may be a new save available
        protected void FetchAvailableSaveFiles()
        {
            if(availableSaveFiles == null)
            {
                availableSaveFiles = new List<int>();
            }
            else
            {
                availableSaveFiles.Clear();
            }
            string[] allFiles = Directory.GetFiles(Mod.path + "/EscapeFromMeatov");
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

        public static void LoadBase(int slotIndex = -1, bool latest = false)
        {
            Mod.LogInfo("Loadbase called");

            Mod.currentLocationIndex = 1;

            // Load base asset bundle
            if (!hideoutLoaded)
            {
                Mod.LogInfo("base null, loading bundle from file for first time");
                Mod.baseAssetsBundle = AssetBundle.LoadFromFile(Mod.path + "/EscapeFromMeatovHideoutAssets.ab");
                Mod.LogInfo("Loaded hideout bunble from file, loading hideout prefab");
                Mod.baseBundle = AssetBundle.LoadFromFile(Mod.path + "/EscapeFromMeatovHideout.ab");
                Mod.LogInfo("Loaded hideout prefab");
                hideoutLoaded = true;
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
                        JObject current = JObject.Parse(File.ReadAllText(Mod.path + "/EscapeFromMeatov/" + (fileIndex == 5 ? "AutoSave" : "Slot" + fileIndex) + ".sav"));
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
                loadedData = JObject.Parse(File.ReadAllText(Mod.path + "/EscapeFromMeatov/" + (slotIndex == 5 ? "AutoSave" : "Slot" + slotIndex) + ".sav"));
                Mod.saveSlotIndex = slotIndex;
            }

            SteamVR_LoadLevel.Begin("MeatovHideoutScene", false, 0.5f, 0f, 0f, 0f, 1f);
        }

        public abstract void InitUI();
    }
}
