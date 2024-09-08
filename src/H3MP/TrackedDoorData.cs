using FistVR;
using H3MP;
using H3MP.Networking;
using H3MP.Tracking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EFM
{
    public class TrackedDoorData : TrackedObjectData
    {
        public TrackedDoor physicalDoor;

        public static bool firstInScene;
        public static List<Door> sceneDoors = new List<Door>();
        public static Dictionary<Door, TrackedDoor> trackedDoorByDoor = new Dictionary<Door, TrackedDoor>();

        public int index;

        public float previousRotAngle;
        public float rotAngle;
        public bool locked;

        public TrackedDoorData()
        {

        }

        public TrackedDoorData(Packet packet, string typeID, int trackedID) : base(packet, typeID, trackedID)
        {
            rotAngle = packet.ReadFloat();
            locked = packet.ReadBool();
        }

        public static bool IsOfType(Transform t)
        {
            return t.GetComponent<Door>() != null;
        }

        public static bool IsControlled(Transform root)
        {
            FVRInteractiveObject door = root.GetComponentInChildren<Door>();
            if (door != null && door.m_hand != null)
            {
                return true;
            }
            return false;
        }

        public static bool TrackSkipped(Transform t)
        {
            if (firstInScene)
            {
                sceneDoors.Clear();
                firstInScene = false;
            }
            sceneDoors.Add(t.GetComponent<Door>());

            // Prevent destruction if tracking is skipped because we are not in control
            return false;
        }

        public static TrackedDoor MakeTracked(Transform root, TrackedObjectData parent)
        {
            TrackedDoor trackedDoor = root.gameObject.AddComponent<TrackedDoor>();
            TrackedDoorData data = new TrackedDoorData();
            trackedDoor.data = data;
            trackedDoor.doorData = data;
            data.physicalDoor = trackedDoor;
            data.physical = trackedDoor;
            data.physicalDoor.physicalDoor = root.GetComponent<Door>();
            data.physical.physical = data.physicalDoor.physicalDoor;

            data.typeIdentifier = "TrackedDoorData";
            data.active = trackedDoor.gameObject.activeInHierarchy;
            data.scene = GameManager.sceneLoading ? H3MP.Patches.LoadLevelBeginPatch.loadingLevel : GameManager.scene;
            data.instance = GameManager.instance;
            data.controller = GameManager.ID;
            data.initTracker = GameManager.ID;
            data.sceneInit = GameManager.InSceneInit();

            if (firstInScene)
            {
                sceneDoors.Clear();
                firstInScene = false;
            }
            data.index = sceneDoors.Count;
            sceneDoors.Add(data.physicalDoor.physicalDoor);

            trackedDoorByDoor.Add(data.physicalDoor.physicalDoor, trackedDoor);
            GameManager.trackedObjectByObject.Add(data.physicalDoor.physicalDoor, trackedDoor);
            GameManager.trackedObjectByInteractive.Add(data.physicalDoor.physicalDoor, trackedDoor);

            // Add to local list
            data.localTrackedID = GameManager.objects.Count;
            GameManager.objects.Add(data);

            // Call an init update because the one in awake won't be called because data was not set yet
            if (trackedDoor.awoken)
            {
                trackedDoor.data.Update(true);
            }

            // Keep a reference in the door itself
            data.physicalDoor.physicalDoor.trackedDoorData = data;

            return trackedDoor;
        }

        public override IEnumerator Instantiate()
        {
            Mod.LogInfo("Instantiating door");
            // Get instance
            Door physicalDoorScript = null;
            if (sceneDoors.Count > index)
            {
                physicalDoorScript = sceneDoors[index];
                if (physicalDoorScript == null)
                {
                    Mod.LogError("Attempted to instantiate Door " + index + " sent from " + controller + " but list at index is null.");
                    yield break;
                }
            }
            else
            {
                Mod.LogError("Attempted to instantiate Door " + index + " sent from " + controller + " but index does not fit in scene door list.");
                yield break;
            }
            GameObject doorInstance = physicalDoorScript.gameObject;

            physicalDoor = doorInstance.AddComponent<TrackedDoor>();
            physical = physicalDoor;
            physicalDoor.physicalDoor = physicalDoorScript;
            physical.physical = physicalDoorScript;
            awaitingInstantiation = false;
            physicalDoor.doorData = this;
            physical.data = this;

            trackedDoorByDoor.Add(physicalDoor.physicalDoor, physicalDoor);
            GameManager.trackedObjectByObject.Add(physicalDoor.physicalDoor, physicalDoor);
            GameManager.trackedObjectByInteractive.Add(physicalDoor.physicalDoor, physicalDoor);

            // Initially set itself
            UpdateFromData(this, true);
        }

        public override void UpdateFromData(TrackedObjectData updatedObject, bool full = false)
        {
            base.UpdateFromData(updatedObject, full);

            TrackedDoorData updatedDoor = updatedObject as TrackedDoorData;

            previousRotAngle = rotAngle;
            rotAngle = updatedDoor.rotAngle;

            if (full)
            {
                locked = updatedDoor.locked;
            }

            if (physicalDoor != null)
            {
                physicalDoor.physicalDoor.transform.localEulerAngles = new Vector3(rotAngle, 0f, 0f);

                if (full)
                {
                    if(physicalDoor.physicalDoor.lockScript != null)
                    {
                        if(physicalDoor.physicalDoor.lockScript.locked && !locked)
                        {
                            physicalDoor.physicalDoor.lockScript.UnlockAction();
                        }
                        else if(!physicalDoor.physicalDoor.lockScript.locked && locked)
                        {
                            physicalDoor.physicalDoor.lockScript.LockAction();
                        }
                    }
                }
            }
        }

        public override void UpdateFromPacket(Packet packet, bool full = false)
        {
            base.UpdateFromPacket(packet, full);

            previousRotAngle = rotAngle;
            rotAngle = packet.ReadFloat();

            if (full)
            {
                locked = packet.ReadBool();
            }

            if (physicalDoor != null)
            {
                physicalDoor.physicalDoor.transform.localEulerAngles = new Vector3(rotAngle, 0f, 0f);

                if (full)
                {
                    if (physicalDoor.physicalDoor.lockScript != null)
                    {
                        if (physicalDoor.physicalDoor.lockScript.locked && !locked)
                        {
                            physicalDoor.physicalDoor.lockScript.UnlockAction();
                        }
                        else if (!physicalDoor.physicalDoor.lockScript.locked && locked)
                        {
                            physicalDoor.physicalDoor.lockScript.LockAction();
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

            previousRotAngle = rotAngle;
            rotAngle = physicalDoor.physicalDoor.transform.localEulerAngles.x;

            if (full)
            {
                if (physicalDoor.physicalDoor.lockScript == null)
                {
                    locked = false;
                }
                else
                {
                    locked = physicalDoor.physicalDoor.lockScript.locked;
                }
            }

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

            if (full)
            {
                packet.Write(locked);
            }
        }

        public override void RemoveFromLocal()
        {
            base.RemoveFromLocal();

            if (trackedID == -1)
            {
                // If not tracked, make sure we remove from tracked lists in case object was unawoken
                if (physicalDoor != null && physicalDoor.physicalDoor != null)
                {
                    trackedDoorByDoor.Remove(physicalDoor.physicalDoor);
                    GameManager.trackedObjectByInteractive.Remove(physicalDoor.physicalDoor);
                }
            }
        }
    }
}
