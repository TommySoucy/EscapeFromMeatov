﻿using FistVR;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    public abstract class EFM_Manager : MonoBehaviour
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
        private void FetchAvailableSaveFiles()
        {
            if(availableSaveFiles == null)
            {
                availableSaveFiles = new List<int>();
            }
            else
            {
                availableSaveFiles.Clear();
            }
            string[] allFiles = Directory.GetFiles("BepInEx/Plugins/EscapeFromMeatov");
            foreach (string path in allFiles)
            {
                if (path.IndexOf(".sav") == path.Length - 4) // If .sav is present as the last part of the path
                {
                    availableSaveFiles.Add(int.Parse("" + path[path.Length - 5]));
                }
            }
        }

        public static void LoadBase(int slotIndex = -1, bool latest = false)
        {
            Mod.instance.LogInfo("Loadbase called");
            // Load base asset bundle
            if (!hideoutLoaded)
            {
                Mod.instance.LogInfo("base null, loading bundle from file for first time");
                Mod.baseAssetsBundle = AssetBundle.LoadFromFile("BepinEx/Plugins/EscapeFromMeatov/EscapeFromMeatovHideoutAssets.ab");
                Mod.instance.LogInfo("Loaded hideout bunble from file, loading hideout prefab");
                Mod.baseBundle = AssetBundle.LoadFromFile("BepinEx/Plugins/EscapeFromMeatov/EscapeFromMeatovHideout.ab");
                Mod.instance.LogInfo("Loaded hideout prefab");
                hideoutLoaded = true;
            }

            if (availableSaveFiles == null)
            {
                availableSaveFiles = new List<int>();
            }

            FVRViveHand rh = GM.CurrentPlayerBody.RightHand.GetComponentInChildren<FVRViveHand>();
            if (rh.CurrentInteractable != null)
            {
                FVRInteractiveObject currentInteractable = rh.CurrentInteractable;
                currentInteractable.EndInteraction(rh);
                Destroy(currentInteractable.gameObject);
            }
            if (rh.OtherHand.CurrentInteractable != null)
            {
                FVRInteractiveObject currentInteractable = rh.OtherHand.CurrentInteractable;
                currentInteractable.EndInteraction(rh.OtherHand);
                Destroy(currentInteractable.gameObject);
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
                        JObject current = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/" + (i == 5 ? "AutoSave" : "Slot" + availableSaveFiles[i]) + ".sav"));
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
                loadedData = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/" + (slotIndex == 5 ? "AutoSave" : "Slot" + slotIndex) + ".sav"));
                Mod.saveSlotIndex = slotIndex;
            }

            SteamVR_LoadLevel.Begin("MeatovHideoutScene", false, 0.5f, 0f, 0f, 0f, 1f);
        }

        public abstract void InitUI();
    }
}
