using FistVR;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Valve.Newtonsoft.Json;

namespace EFM
{
    abstract class EFM_Manager : MonoBehaviour
    {
        public bool init;

        public static int meatovTimeMultiplier = 7;
        public static List<int> availableSaveFiles;

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

        public static void LoadBase(GameObject caller, int slotIndex = -1, bool latest = false)
        {
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
            SaveData loadedData = null;
            if (slotIndex == -1)
            {
                if (latest)
                {
                    long currentLatestTime = 0;
                    for (int i = 0; i < availableSaveFiles.Count; ++i)
                    {
                        SaveData current = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/" + (i == 5 ? "AutoSave" : "Slot" + availableSaveFiles[i]) + ".sav"));
                        long saveTime = current.time;
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
                loadedData = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/" + (slotIndex == 5 ? "AutoSave" : "Slot" + slotIndex) + ".sav"));
                Mod.saveSlotIndex = slotIndex;
            }

            GameObject baseObject = Instantiate<GameObject>(Mod.scenePrefab_Base);
            baseObject.name = Mod.scenePrefab_Base.name;
            baseObject.transform.position = Vector3.down * 50; // Move base 50 meters down because it will not be destroyed while in raid and we dont want it in the way

            EFM_Base_Manager baseManager = baseObject.AddComponent<EFM_Base_Manager>();
            baseManager.data = loadedData;
            baseManager.Init();

            Transform spawnPoint = baseObject.transform.GetChild(baseObject.transform.childCount - 1).GetChild(0);
            GM.CurrentMovementManager.TeleportToPoint(spawnPoint.position, true, spawnPoint.rotation.eulerAngles);

            Destroy(caller);
        }

        public abstract void InitUI();
    }

    // Items data
    public class DefaultObjectWrapper
    {
        public string DisplayName { get; set; }
        public int Category { get; set; }
        public float Mass { get; set; }
        public int MagazineCapacity { get; set; }
        public bool RequiresPicatinnySight { get; set; }
        public int TagEra { get; set; }
        public int TagSet { get; set; }
        public int TagFirearmSize { get; set; }
        public int TagFirearmAction { get; set; }
        public int TagFirearmRoundPower { get; set; }
        public int TagFirearmCountryOfOrigin { get; set; }
        public int TagFirearmFirstYear { get; set; }
        public List<int> TagFirearmFiringModes { get; set; }
        public List<int> TagFirearmFeedOption { get; set; }
        public List<int> TagFirearmMounts { get; set; }
        public int TagAttachmentMount { get; set; }
        public int TagAttachmentFeature { get; set; }
        public int TagMeleeStyle { get; set; }
        public int TagMeleeHandedness { get; set; }
        public int TagPowerupType { get; set; }
        public int TagThrownType { get; set; }
        public int TagThrownDamageType { get; set; }
        public int MagazineType { get; set; }
        public List<ObjectWrapper> CompatibleMagazines { get; set; } // TODO
        public List<ObjectWrapper> CompatibleClips { get; set; } // TODO
        public List<ObjectWrapper> CompatibleSpeedLoaders { get; set; } // TODO
        public List<ObjectWrapper> CompatibleSingleRounds { get; set; } // TODO
        public List<ObjectWrapper> BespokeAttachments { get; set; } // TODO
        public List<ObjectWrapper> RequiredSecondaryPieces { get; set; } // TODO
        public int MinCapacityRelated { get; set; } // TODO -1
        public int MaxCapacityRelated { get; set; } // TODO -1
        public int CreditCost { get; set; }
        public bool OSple { get; set; }
    }

    public class DefaultPhysicalObject
    {
        public DefaultObjectWrapper DefaultObjectWrapper { get; set; }
        public bool SpawnLockable { get; set; }
        public bool Harnessable { get; set; }
        public int HandlingReleaseIntoSlotSound { get; set; }
        public int Size { get; set; }
        public int QBSlotType { get; set; }
        public bool DoesReleaseOverrideVelocity { get; set; }
        public bool DoesReleaseAddVelocity { get; set; }
        public float ThrowVelMultiplier { get; set; }
        public float ThrowAngMultiplier { get; set; }
        public float MoveIntensity { get; set; }
        public float RotIntensity { get; set; }
        public bool UsesGravity { get; set; }
        public bool DistantGrabbable { get; set; }
        public bool IsDebug { get; set; }
        public bool IsAltHeld { get; set; }
        public bool IsKinematicLocked { get; set; }
        public bool DoesQuickbeltSlotFollowHead { get; set; }
        public bool IsPickUpLocked { get; set; }
        public int OverridesObjectToHand { get; set; }
    }

    public class ArmorAndRigProperties
    {
        public float Coverage { get; set; }
        public float DamageResist { get; set; }
        public float Armor { get; set; }
    }

    public class BackpackProperties
    {
        public float MaxVolume { get; set; }
    }

    public class ItemDefaults
    {
        public int ItemType { get; set; }
        public List<float> Volumes { get; set; }
        public DefaultPhysicalObject DefaultPhysicalObject { get; set; }

        // Specific to armor and rigs
        public ArmorAndRigProperties ArmorAndRigProperties { get; set; }

        // Specific to Backpacks
        public BackpackProperties BackpackProperties { get; set; }
    }

    public class DefaultItemData
    {
        public List<ItemDefaults> ItemDefaults { get; set; }
    }
}
