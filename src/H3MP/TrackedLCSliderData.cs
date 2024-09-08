using FistVR;
using H3MP;
using H3MP.Networking;
using H3MP.Tracking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EFM
{
    public class TrackedLCSliderData : TrackedObjectData
    {
        public TrackedLCSlider physicalLCSlider;

        public static bool firstInScene;
        public static List<LootContainerSlider> sceneLCSliders = new List<LootContainerSlider>();
        public static Dictionary<LootContainerSlider, TrackedLCSlider> trackedLCSliderByLCSlider = new Dictionary<LootContainerSlider, TrackedLCSlider>();

        public int index;

        public Vector3 previousPos;
        public Vector3 pos;

        public TrackedLCSliderData()
        {

        }

        public TrackedLCSliderData(Packet packet, string typeID, int trackedID) : base(packet, typeID, trackedID)
        {
            pos = packet.ReadVector3();
        }

        public static bool IsOfType(Transform t)
        {
            return t.GetComponent<LootContainerSlider>() != null;
        }

        public static bool IsControlled(Transform root)
        {
            FVRInteractiveObject lootContainerSlider = root.GetComponentInChildren<LootContainerSlider>();
            if (lootContainerSlider != null && lootContainerSlider.m_hand != null)
            {
                return true;
            }
            return false;
        }

        public static bool TrackSkipped(Transform t)
        {
            if (firstInScene)
            {
                sceneLCSliders.Clear();
                firstInScene = false;
            }
            sceneLCSliders.Add(t.GetComponent<LootContainerSlider>());

            // Prevent destruction if tracking is skipped because we are not in control
            return false;
        }

        public static TrackedLCSlider MakeTracked(Transform root, TrackedObjectData parent)
        {
            TrackedLCSlider trackedLCSlider = root.gameObject.AddComponent<TrackedLCSlider>();
            TrackedLCSliderData data = new TrackedLCSliderData();
            trackedLCSlider.data = data;
            trackedLCSlider.sliderData = data;
            data.physicalLCSlider = trackedLCSlider;
            data.physical = trackedLCSlider;
            data.physicalLCSlider.physicalLCSlider = root.GetComponent<LootContainerSlider>();
            data.physical.physical = data.physicalLCSlider.physicalLCSlider;

            data.typeIdentifier = "TrackedLCSliderData";
            data.active = trackedLCSlider.gameObject.activeInHierarchy;
            data.scene = GameManager.sceneLoading ? H3MP.Patches.LoadLevelBeginPatch.loadingLevel : GameManager.scene;
            data.instance = GameManager.instance;
            data.controller = GameManager.ID;
            data.initTracker = GameManager.ID;
            data.sceneInit = GameManager.InSceneInit();

            if (firstInScene)
            {
                sceneLCSliders.Clear();
                firstInScene = false;
            }
            data.index = sceneLCSliders.Count;
            sceneLCSliders.Add(data.physicalLCSlider.physicalLCSlider);

            trackedLCSliderByLCSlider.Add(data.physicalLCSlider.physicalLCSlider, trackedLCSlider);
            GameManager.trackedObjectByObject.Add(data.physicalLCSlider.physicalLCSlider, trackedLCSlider);
            GameManager.trackedObjectByInteractive.Add(data.physicalLCSlider.physicalLCSlider, trackedLCSlider);

            // Add to local list
            data.localTrackedID = GameManager.objects.Count;
            GameManager.objects.Add(data);

            // Call an init update because the one in awake won't be called because data was not set yet
            if (trackedLCSlider.awoken)
            {
                trackedLCSlider.data.Update(true);
            }

            // Keep a reference in the door itself
            data.physicalLCSlider.physicalLCSlider.trackedLCSliderData = data;

            return trackedLCSlider;
        }

        public override IEnumerator Instantiate()
        {
            Mod.LogInfo("Instantiating loot container cover");
            // Get instance
            LootContainerSlider physicalLCSliderScript = null;
            if (sceneLCSliders.Count > index)
            {
                physicalLCSliderScript = sceneLCSliders[index];
                if (physicalLCSliderScript == null)
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
            GameObject LCSliderInstance = physicalLCSliderScript.gameObject;

            physicalLCSlider = LCSliderInstance.AddComponent<TrackedLCSlider>();
            physical = physicalLCSlider;
            physicalLCSlider.physicalLCSlider = physicalLCSliderScript;
            physical.physical = physicalLCSliderScript;
            awaitingInstantiation = false;
            physicalLCSlider.sliderData = this;
            physical.data = this;

            trackedLCSliderByLCSlider.Add(physicalLCSlider.physicalLCSlider, physicalLCSlider);
            GameManager.trackedObjectByObject.Add(physicalLCSlider.physicalLCSlider, physicalLCSlider);
            GameManager.trackedObjectByInteractive.Add(physicalLCSlider.physicalLCSlider, physicalLCSlider);

            // Initially set itself
            UpdateFromData(this, true);
        }

        public override void UpdateFromData(TrackedObjectData updatedObject, bool full = false)
        {
            base.UpdateFromData(updatedObject, full);

            TrackedLCSliderData updatedSlider = updatedObject as TrackedLCSliderData;

            previousPos = pos;
            pos = updatedSlider.pos;

            if (physicalLCSlider != null)
            {
                physicalLCSlider.physicalLCSlider.transform.localPosition = pos;
            }
        }

        public override void UpdateFromPacket(Packet packet, bool full = false)
        {
            base.UpdateFromPacket(packet, full);

            previousPos = pos;
            pos = packet.ReadVector3();

            if (physicalLCSlider != null)
            {
                physicalLCSlider.physicalLCSlider.transform.localPosition = pos;
            }
        }

        public override bool Update(bool full = false)
        {
            base.Update(full);

            if (physical == null)
            {
                return false;
            }

            previousPos = pos;
            pos = physicalLCSlider.physicalLCSlider.transform.localPosition;

            return NeedsUpdate();
        }

        public override bool NeedsUpdate()
        {
            return base.NeedsUpdate() || !previousPos.Equals(pos);
        }

        public override void WriteToPacket(Packet packet, bool incrementOrder, bool full)
        {
            base.WriteToPacket(packet, incrementOrder, full);

            packet.Write(pos);
        }

        public override void RemoveFromLocal()
        {
            base.RemoveFromLocal();

            if (trackedID == -1)
            {
                // If not tracked, make sure we remove from tracked lists in case object was unawoken
                if (physicalLCSlider != null && physicalLCSlider.physicalLCSlider != null)
                {
                    trackedLCSliderByLCSlider.Remove(physicalLCSlider.physicalLCSlider);
                    GameManager.trackedObjectByInteractive.Remove(physicalLCSlider.physicalLCSlider);
                }
            }
        }
    }
}
