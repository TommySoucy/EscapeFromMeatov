using FistVR;
using H3MP;
using H3MP.Networking;
using H3MP.Tracking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EFM
{
    public class TrackedLootContainerData : TrackedObjectData
    {
        public TrackedLootContainer physicalLootContainer;

        public static bool firstInScene;
        public static List<LootContainer> sceneLootContainers = new List<LootContainer>();
        public static Dictionary<LootContainer, TrackedLootContainer> trackedLootContainerByLootContainer = new Dictionary<LootContainer, TrackedLootContainer>();

        public int index;

        public bool spawnedContents;
        public bool locked;

        public TrackedLootContainerData()
        {

        }

        public TrackedLootContainerData(Packet packet, string typeID, int trackedID) : base(packet, typeID, trackedID)
        {
            spawnedContents = packet.ReadBool();
            locked = packet.ReadBool();
        }

        public static bool IsOfType(Transform t)
        {
            return t.GetComponent<Door>() != null;
        }

        public static bool IsControlled(Transform root)
        {
            return false;
        }

        public static bool TrackSkipped(Transform t)
        {
            if (firstInScene)
            {
                sceneLootContainers.Clear();
                firstInScene = false;
            }
            sceneLootContainers.Add(t.GetComponent<LootContainer>());

            // Prevent destruction if tracking is skipped because we are not in control
            return false;
        }

        public static TrackedLootContainer MakeTracked(Transform root, TrackedObjectData parent)
        {
            TrackedLootContainer trackedLootContainer = root.gameObject.AddComponent<TrackedLootContainer>();
            TrackedLootContainerData data = new TrackedLootContainerData();
            trackedLootContainer.data = data;
            trackedLootContainer.lootContainerData = data;
            data.physicalLootContainer = trackedLootContainer;
            data.physical = trackedLootContainer;
            data.physicalLootContainer.physicalLootContainer = root.GetComponent<LootContainer>();
            data.physical.physical = data.physicalLootContainer.physicalLootContainer;

            data.typeIdentifier = "TrackedLootContainerData";
            data.active = trackedLootContainer.gameObject.activeInHierarchy;
            data.scene = GameManager.sceneLoading ? H3MP.Patches.LoadLevelBeginPatch.loadingLevel : GameManager.scene;
            data.instance = GameManager.instance;
            data.controller = GameManager.ID;
            data.initTracker = GameManager.ID;
            data.sceneInit = GameManager.InSceneInit();

            if (firstInScene)
            {
                sceneLootContainers.Clear();
                firstInScene = false;
            }
            data.index = sceneLootContainers.Count;
            sceneLootContainers.Add(data.physicalLootContainer.physicalLootContainer);

            trackedLootContainerByLootContainer.Add(data.physicalLootContainer.physicalLootContainer, trackedLootContainer);
            GameManager.trackedObjectByObject.Add(data.physicalLootContainer.physicalLootContainer, trackedLootContainer);

            // Add to local list
            data.localTrackedID = GameManager.objects.Count;
            GameManager.objects.Add(data);

            // Keep a reference in the LootContainer itself
            data.physicalLootContainer.physicalLootContainer.trackedLootContainerData = data;

            // Call an init update because the one in awake won't be called because data was not set yet
            if (trackedLootContainer.awoken)
            {
                trackedLootContainer.data.Update(true);
            }

            return trackedLootContainer;
        }

        public override IEnumerator Instantiate()
        {
            Mod.LogInfo("Instantiating loot container");
            // Get instance
            LootContainer physicalLootContainerScript = null;
            if (sceneLootContainers.Count > index)
            {
                physicalLootContainerScript = sceneLootContainers[index];
                if (physicalLootContainerScript == null)
                {
                    Mod.LogError("Attempted to instantiate LootContainer " + index + " sent from " + controller + " but list at index is null.");
                    yield break;
                }
            }
            else
            {
                Mod.LogError("Attempted to instantiate LootContainer " + index + " sent from " + controller + " but index does not fit in scene LootContainer list.");
                yield break;
            }
            GameObject lootContainerInstance = physicalLootContainerScript.gameObject;

            physicalLootContainer = lootContainerInstance.AddComponent<TrackedLootContainer>();
            physical = physicalLootContainer;
            physicalLootContainer.physicalLootContainer = physicalLootContainerScript;
            physical.physical = physicalLootContainerScript;
            awaitingInstantiation = false;
            physicalLootContainer.lootContainerData = this;
            physical.data = this;

            trackedLootContainerByLootContainer.Add(physicalLootContainer.physicalLootContainer, physicalLootContainer);
            GameManager.trackedObjectByObject.Add(physicalLootContainer.physicalLootContainer, physicalLootContainer);

            // Keep a reference in the LootContainer itself
            physicalLootContainer.physicalLootContainer.trackedLootContainerData = this;

            // Initially set itself
            UpdateFromData(this, true);
        }

        public override void UpdateFromData(TrackedObjectData updatedObject, bool full = false)
        {
            base.UpdateFromData(updatedObject, full);

            TrackedLootContainerData updatedLootContainer = updatedObject as TrackedLootContainerData;

            if (full)
            {
                spawnedContents = updatedLootContainer.spawnedContents;
                locked = updatedLootContainer.locked;
            }

            if (physicalLootContainer != null)
            {
                if (full)
                {
                    if (physicalLootContainer.physicalLootContainer.lockScript != null)
                    {
                        if (physicalLootContainer.physicalLootContainer.lockScript.locked && !locked)
                        {
                            physicalLootContainer.physicalLootContainer.lockScript.UnlockAction();
                        }
                        else if (!physicalLootContainer.physicalLootContainer.lockScript.locked && locked)
                        {
                            physicalLootContainer.physicalLootContainer.lockScript.LockAction();
                        }
                    }
                }
            }
        }

        public override void UpdateFromPacket(Packet packet, bool full = false)
        {
            base.UpdateFromPacket(packet, full);

            if (full)
            {
                spawnedContents = packet.ReadBool();
                locked = packet.ReadBool();
            }

            if (physicalLootContainer != null)
            {
                if (full)
                {
                    if (physicalLootContainer.physicalLootContainer.lockScript != null)
                    {
                        if (physicalLootContainer.physicalLootContainer.lockScript.locked && !locked)
                        {
                            physicalLootContainer.physicalLootContainer.lockScript.UnlockAction();
                        }
                        else if (!physicalLootContainer.physicalLootContainer.lockScript.locked && locked)
                        {
                            physicalLootContainer.physicalLootContainer.lockScript.LockAction();
                        }
                    }
                }
            }
        }

        public override bool Update(bool full = false)
        {
            base.Update(full);

            if (physical == null)
            {
                return false;
            }

            if (full)
            {
                spawnedContents = physicalLootContainer.physicalLootContainer.contentsSpawned;

                if (physicalLootContainer.physicalLootContainer.lockScript == null)
                {
                    locked = false;
                }
                else
                {
                    locked = physicalLootContainer.physicalLootContainer.lockScript.locked;
                }
            }

            return NeedsUpdate();
        }

        public override bool NeedsUpdate()
        {
            return base.NeedsUpdate();
        }

        public override void WriteToPacket(Packet packet, bool incrementOrder, bool full)
        {
            base.WriteToPacket(packet, incrementOrder, full);

            if (full)
            {
                packet.Write(spawnedContents);
                packet.Write(locked);
            }
        }

        public override void RemoveFromLocal()
        {
            base.RemoveFromLocal();

            if (trackedID == -1)
            {
                // If not tracked, make sure we remove from tracked lists in case object was unawoken
                if (physicalLootContainer != null && physicalLootContainer.physicalLootContainer != null)
                {
                    trackedLootContainerByLootContainer.Remove(physicalLootContainer.physicalLootContainer);
                }
            }
        }
    }
}
