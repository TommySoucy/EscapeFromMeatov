using FistVR;
using H3MP;
using H3MP.Networking;
using H3MP.Tracking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EFM
{
    public class TrackedLCCoverData : TrackedObjectData
    {
        public TrackedLCCover physicalLCCover;

        public static bool firstInScene;
        public static List<LootContainerCover> sceneLCCovers = new List<LootContainerCover>();
        public static Dictionary<LootContainerCover, TrackedLCCover> trackedLCCoverByLCCover = new Dictionary<LootContainerCover, TrackedLCCover>();

        public int index;

        public float previousRotAngle;
        public float rotAngle;

        public TrackedLCCoverData()
        {

        }

        public TrackedLCCoverData(Packet packet, string typeID, int trackedID) : base(packet, typeID, trackedID)
        {
            rotAngle = packet.ReadFloat();
        }

        public static bool IsOfType(Transform t)
        {
            return t.GetComponent<LootContainerCover>() != null;
        }

        public static bool IsControlled(Transform root)
        {
            FVRInteractiveObject lootContainerCover = root.GetComponentInChildren<LootContainerCover>();
            if (lootContainerCover != null && lootContainerCover.m_hand != null)
            {
                return true;
            }
            return false;
        }

        public static bool TrackSkipped(Transform t)
        {
            if (firstInScene)
            {
                sceneLCCovers.Clear();
                firstInScene = false;
            }
            sceneLCCovers.Add(t.GetComponent<LootContainerCover>());

            // Prevent destruction if tracking is skipped because we are not in control
            return false;
        }

        public static TrackedLCCover MakeTracked(Transform root, TrackedObjectData parent)
        {
            TrackedLCCover trackedLCCover = root.gameObject.AddComponent<TrackedLCCover>();
            TrackedLCCoverData data = new TrackedLCCoverData();
            trackedLCCover.data = data;
            trackedLCCover.coverData = data;
            data.physicalLCCover = trackedLCCover;
            data.physical = trackedLCCover;
            data.physicalLCCover.physicalLCCover = root.GetComponent<LootContainerCover>();
            data.physical.physical = data.physicalLCCover.physicalLCCover;

            data.typeIdentifier = "TrackedLCCoverData";
            data.active = trackedLCCover.gameObject.activeInHierarchy;
            data.scene = GameManager.sceneLoading ? H3MP.Patches.LoadLevelBeginPatch.loadingLevel : GameManager.scene;
            data.instance = GameManager.instance;
            data.controller = GameManager.ID;
            data.initTracker = GameManager.ID;
            data.sceneInit = GameManager.InSceneInit();

            if (firstInScene)
            {
                sceneLCCovers.Clear();
                firstInScene = false;
            }
            data.index = sceneLCCovers.Count;
            sceneLCCovers.Add(data.physicalLCCover.physicalLCCover);

            trackedLCCoverByLCCover.Add(data.physicalLCCover.physicalLCCover, trackedLCCover);
            GameManager.trackedObjectByObject.Add(data.physicalLCCover.physicalLCCover, trackedLCCover);
            GameManager.trackedObjectByInteractive.Add(data.physicalLCCover.physicalLCCover, trackedLCCover);

            // Add to local list
            data.localTrackedID = GameManager.objects.Count;
            GameManager.objects.Add(data);

            // Keep a reference in the door itself
            data.physicalLCCover.physicalLCCover.trackedLCCoverData = data;

            // Call an init update because the one in awake won't be called because data was not set yet
            if (trackedLCCover.awoken)
            {
                trackedLCCover.data.Update(true);
            }

            return trackedLCCover;
        }

        public override IEnumerator Instantiate()
        {
            Mod.LogInfo("Instantiating loot container cover");
            // Get instance
            LootContainerCover physicalLCCoverScript = null;
            if (sceneLCCovers.Count > index)
            {
                physicalLCCoverScript = sceneLCCovers[index];
                if (physicalLCCoverScript == null)
                {
                    Mod.LogError("Attempted to instantiate loot container cover " + index + " sent from " + controller + " but list at index is null.");
                    yield break;
                }
            }
            else
            {
                Mod.LogError("Attempted to instantiate loot container cover " + index + " sent from " + controller + " but index does not fit in scene door list.");
                yield break;
            }
            GameObject LCCoverInstance = physicalLCCoverScript.gameObject;

            physicalLCCover = LCCoverInstance.AddComponent<TrackedLCCover>();
            physical = physicalLCCover;
            physicalLCCover.physicalLCCover = physicalLCCoverScript;
            physical.physical = physicalLCCoverScript;
            awaitingInstantiation = false;
            physicalLCCover.coverData = this;
            physical.data = this;

            trackedLCCoverByLCCover.Add(physicalLCCover.physicalLCCover, physicalLCCover);
            GameManager.trackedObjectByObject.Add(physicalLCCover.physicalLCCover, physicalLCCover);
            GameManager.trackedObjectByInteractive.Add(physicalLCCover.physicalLCCover, physicalLCCover);

            physicalLCCover.physicalLCCover.trackedLCCoverData = this;

            // Initially set itself
            UpdateFromData(this, true);
        }

        public override void UpdateFromData(TrackedObjectData updatedObject, bool full = false)
        {
            base.UpdateFromData(updatedObject, full);

            TrackedLCCoverData updatedCover = updatedObject as TrackedLCCoverData;

            previousRotAngle = rotAngle;
            rotAngle = updatedCover.rotAngle;

            if (physicalLCCover != null)
            {
                physicalLCCover.physicalLCCover.transform.localEulerAngles = new Vector3(rotAngle, 0f, 0f);
            }
        }

        public override void UpdateFromPacket(Packet packet, bool full = false)
        {
            base.UpdateFromPacket(packet, full);

            previousRotAngle = rotAngle;
            rotAngle = packet.ReadFloat();

            if (physicalLCCover != null)
            {
                physicalLCCover.physicalLCCover.transform.localEulerAngles = new Vector3(rotAngle, 0f, 0f);
            }
        }

        public override bool Update(bool full = false)
        {
            base.Update(full);

            if (physical == null)
            {
                return false;
            }

            previousRotAngle = rotAngle;
            rotAngle = physicalLCCover.physicalLCCover.transform.localEulerAngles.x;

            return NeedsUpdate();
        }

        public override bool NeedsUpdate()
        {
            return base.NeedsUpdate() || previousRotAngle != rotAngle;
        }

        public override void WriteToPacket(Packet packet, bool incrementOrder, bool full)
        {
            base.WriteToPacket(packet, incrementOrder, full);

            packet.Write(rotAngle);
        }

        public override void RemoveFromLocal()
        {
            base.RemoveFromLocal();

            if (trackedID == -1)
            {
                // If not tracked, make sure we remove from tracked lists in case object was unawoken
                if (physicalLCCover != null && physicalLCCover.physicalLCCover != null)
                {
                    trackedLCCoverByLCCover.Remove(physicalLCCover.physicalLCCover);
                    GameManager.trackedObjectByInteractive.Remove(physicalLCCover.physicalLCCover);
                }
            }
        }
    }
}
