using FistVR;
using H3MP;
using H3MP.Networking;
using H3MP.Tracking;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;

namespace EFM
{
    public class TrackedMeatovItemData : TrackedItemData
    {
        public TrackedMeatovItem physicalMeatovItem;

        public static Dictionary<MeatovItem, TrackedMeatovItem> trackedMeatovItemByMeatovItem = new Dictionary<MeatovItem, TrackedMeatovItem>();

        // Data
        public string tarkovID;
        public int dogtagLevel;
        public string dogtagName;

        // State
        public int previousParentSlotIndex = -1;
        public int parentSlotIndex = -1;
        public bool previousInsured;
        public bool insured;
        public bool previousFoundInRaid;
        public bool foundInRaid;
        public int previousMode;
        public int mode;
        public bool previousOpen;
        public bool open;
        public bool previousBroken;
        public bool broken;
        public int previousStack;
        public int stack;
        public int previousAmount;
        public int amount;

        public TrackedMeatovItemData()
        {

        }

        public TrackedMeatovItemData(Packet packet, string typeID, int trackedID) : base(packet, typeID, trackedID)
        {
            // Update
            parentSlotIndex = packet.ReadInt();
            insured = packet.ReadBool();
            foundInRaid = packet.ReadBool();
            mode = packet.ReadInt();
            open = packet.ReadBool();
            broken = packet.ReadBool();
            stack = packet.ReadInt();
            amount = packet.ReadInt();

            // Full
            tarkovID = packet.ReadString();
            dogtagLevel = packet.ReadInt();
            dogtagName = packet.ReadString();
        }

        public static new bool IsOfType(Transform t)
        {
            return t.GetComponent<MeatovItem>() != null && TrackedItemData.IsOfType(t);
        }

        public override bool IsIdentifiable()
        {
            return Mod.defaultItemData.ContainsKey(tarkovID) && base.IsIdentifiable();
        }

        public static new TrackedMeatovItem MakeTracked(Transform root, TrackedObjectData parent)
        {
            Mod.LogInfo("Making meatov item tracked", false);
            TrackedMeatovItem trackedMeatovItem = root.gameObject.AddComponent<TrackedMeatovItem>();
            TrackedMeatovItemData data = new TrackedMeatovItemData();
            trackedMeatovItem.data = data;
            trackedMeatovItem.itemData = data;
            trackedMeatovItem.meatovItemData = data;
            data.physicalItem = trackedMeatovItem;
            data.physicalMeatovItem = trackedMeatovItem;
            data.physical = trackedMeatovItem;
            data.physicalItem.physicalItem = root.GetComponent<FVRPhysicalObject>();
            data.physicalMeatovItem.physicalMeatovItem = root.GetComponent<MeatovItem>();
            data.physical.physical = data.physicalItem.physicalItem;

            data.typeIdentifier = "TrackedMeatovItemData";
            data.active = trackedMeatovItem.gameObject.activeInHierarchy;
            data.scene = GameManager.sceneLoading ? H3MP.Patches.LoadLevelBeginPatch.loadingLevel : GameManager.scene;
            data.instance = GameManager.instance;
            data.controller = GameManager.ID;
            data.initTracker = GameManager.ID;
            data.sceneInit = GameManager.InSceneInit();

            trackedMeatovItemByMeatovItem.Add(data.physicalMeatovItem.physicalMeatovItem, trackedMeatovItem);
            GameManager.trackedItemByItem.Add(data.physicalItem.physicalItem, trackedMeatovItem);
            if (data.physicalItem.physicalItem is SosigWeaponPlayerInterface)
            {
                GameManager.trackedItemBySosigWeapon.Add((data.physicalItem.physicalItem as SosigWeaponPlayerInterface).W, trackedMeatovItem);
            }
            GameManager.trackedObjectByObject.Add(data.physicalItem.physicalItem, trackedMeatovItem);
            GameManager.trackedObjectByObject.Add(data.physicalMeatovItem.physicalMeatovItem, trackedMeatovItem);
            GameManager.trackedObjectByInteractive.Add(data.physicalItem.physicalItem, trackedMeatovItem);

            if (parent != null)
            {
                data.parent = parent.trackedID;
                if (parent.children == null)
                {
                    parent.children = new List<TrackedObjectData>();
                }
                data.childIndex = parent.children.Count;
                parent.children.Add(data);

                data.position = trackedMeatovItem.transform.localPosition;
                data.rotation = trackedMeatovItem.transform.localRotation;
            }
            else
            {
                data.position = trackedMeatovItem.transform.position;
                data.rotation = trackedMeatovItem.transform.rotation;
            }
            data.SetItemIdentifyingInfo();
            Mod.LogInfo("\tItemID: " + data.itemID, false);
            data.underActiveControl = data.IsControlled(out int interactionID);

            data.CollectExternalData();

            // Add to local list
            data.localTrackedID = GameManager.objects.Count;
            GameManager.objects.Add(data);

            // Call an init update because the one in awake won't be called because data was not set yet
            if (trackedMeatovItem.updateFunc != null)
            {
                trackedMeatovItem.updateFunc();
            }

            return trackedMeatovItem;
        }

        public override IEnumerator Instantiate()
        {
            GameObject itemPrefab = GetItemPrefab();
            if (itemPrefab == null)
            {
                if (IM.OD.TryGetValue(itemID, out FVRObject obj))
                {
                    yield return obj.GetGameObjectAsync();
                    itemPrefab = obj.GetGameObject();
                }
            }
            if (itemPrefab == null)
            {
                Mod.LogError($"Attempted to instantiate {itemID} sent from {controller} but failed to get item prefab.");
                awaitingInstantiation = false;
                yield break;
            }

            if (!awaitingInstantiation)
            {
                // Could have cancelled an item instantiation if received destruction order while we were waiting to get the prefab
                yield break;
            }

            // Here, we can't simply skip the next instantiate
            // Since Awake() will be called on the object upon instantiation, if the object instantiates something in Awake(), like firearm,
            // it will consume the skipNextInstantiate, so we instead skipAllInstantiates during the current instantiation
            //++Mod.skipNextInstantiates;

            try
            {
                Mod.LogInfo("Instantiating meatov item at " + trackedID + ": " + tarkovID + ":" + itemID, false);
                ++H3MP.Mod.skipAllInstantiates;
                if (H3MP.Mod.skipAllInstantiates <= 0) { Mod.LogError("SkipAllInstantiates negative or 0 at item instantiation, setting to 1"); H3MP.Mod.skipAllInstantiates = 1; }
                GameObject itemObject = GameObject.Instantiate(itemPrefab, position, rotation);
                --H3MP.Mod.skipAllInstantiates;
                physicalMeatovItem = itemObject.AddComponent<TrackedMeatovItem>();
                physicalItem = physicalMeatovItem;
                physical = physicalItem;
                awaitingInstantiation = false;
                physicalItem.itemData = this;
                physicalMeatovItem.meatovItemData = this;
                physical.data = this;
                physicalMeatovItem.physicalMeatovItem = itemObject.GetComponent<MeatovItem>();
                physicalItem.physicalItem = itemObject.GetComponent<FVRPhysicalObject>();
                physical.physical = physicalItem.physicalItem;

                trackedMeatovItemByMeatovItem.Add(physicalMeatovItem.physicalMeatovItem, physicalMeatovItem);
                if (GameManager.trackedItemByItem.TryGetValue(physicalItem.physicalItem, out TrackedItem t))
                {
                    Mod.LogError("Error at instantiation of: " + itemID + ": Item's physical item object already exists in trackedItemByItem\n\tTrackedID: " + t.data.trackedID);
                }
                else
                {
                    GameManager.trackedItemByItem.Add(physicalItem.physicalItem, physicalItem);
                }
                if (physicalItem.physicalItem is SosigWeaponPlayerInterface)
                {
                    GameManager.trackedItemBySosigWeapon.Add((physicalItem.physicalItem as SosigWeaponPlayerInterface).W, physicalItem);
                }

                if (GameManager.trackedObjectByObject.TryGetValue(physicalItem.physicalItem, out TrackedObject to))
                {
                    Mod.LogError("Error at instantiation of: " + itemID + ": Item's physical object already exists in trackedObjectByObject\n\tTrackedID: " + to.data.trackedID);
                }
                else
                {
                    GameManager.trackedObjectByObject.Add(physicalItem.physicalItem, physicalItem);
                }
                GameManager.trackedObjectByObject.Add(physicalMeatovItem.physicalMeatovItem, physicalMeatovItem);
                GameManager.trackedObjectByInteractive.Add(physicalItem.physicalItem, physicalItem);

                // See Note in GameManager.SyncTrackedObjects
                // Unfortunately this doesn't necessarily help us in this case considering we need the parent to have been instantiated
                // by now, but since the instantiation is a coroutine, we are not guaranteed to have the parent's physObj yet
                if (parent != -1)
                {
                    // Add ourselves to the parent's children
                    TrackedObjectData parentObject = (ThreadManager.host ? Server.objects : Client.objects)[parent];

                    if (parentObject.physical == null)
                    {
                        parentObject.childrenToParent.Add(trackedID);
                    }
                    else
                    {
                        // Physically parent
                        ++ignoreParentChanged;
                        itemObject.transform.parent = parentObject.physical.transform;
                        --ignoreParentChanged;

                        // Handle meatov item parent
                        if(parentObject is TrackedMeatovItemData)
                        {
                            TODO: // Note that this assumes that if we our parent has a volume and no slot, we are in the volume.
                            // This means we assume a meatov item cannot be attached to a parent that has both a container volume AND some other attachment system
                            // like vanilla mount. We should probably check for stuff like that, but currently, no such meatov item exists
                            TrackedMeatovItemData parentMeatovItemData = parentObject as TrackedMeatovItemData;
                            if (parentSlotIndex = -1 && parentMeatovItemData.physicalMeatovItem.physicalMeatovItem.containerVolume != null)
                            {
                                parentMeatovItemData.physicalMeatovItem.physicalMeatovItem.containerVolume.AddItem(physicalMeatovItem.physicalMeatovItem, true);
                            }
                            else if(parentSlotIndex != -1 && parentMeatovItemData.physicalMeatovItem.physicalMeatovItem.rigSlots != null 
                                && parentMeatovItemData.physicalMeatovItem.physicalMeatovItem.rigslots.Count > parentSlotIndex)
                            {
                                physicalMeatovItem.physicalMeatovItem.physObj.SetQuickBeltSlot(parentMeatovItemData.physicalMeatovItem.physicalMeatovItem.rigslots[parentSlotIndex]);
                            }
                        }
                    }
                }

                // Set as kinematic if not in control
                if (controller != GameManager.ID)
                {
                    H3MP.Mod.SetKinematicRecursive(physical.transform, true);
                }

                // Initially set itself
                UpdateFromData(this);

                // Process the initialdata. This must be done after the update so it can override it
                ProcessAdditionalData();

                // Process childrenToParent
                for (int i = 0; i < childrenToParent.Count; ++i)
                {
                    TrackedObjectData childObject = (ThreadManager.host ? Server.objects : Client.objects)[childrenToParent[i]];
                    if (childObject != null && childObject.parent == trackedID && childObject.physical != null)
                    {
                        // Physically parent
                        ++childObject.ignoreParentChanged;
                        childObject.physical.transform.parent = physical.transform;
                        --childObject.ignoreParentChanged;

                        // Handle meatov item parent
                        if (childObject is TrackedMeatovItemData)
                        {
                            TrackedMeatovItemData childMeatovItemData = childObject as TrackedMeatovItemData;
                            if (childMeatovItemData.parentSlotIndex = -1 && physicalMeatovItem.physicalMeatovItem.containerVolume != null)
                            {
                                physicalMeatovItem.physicalMeatovItem.containerVolume.AddItem(childMeatovItemData.physicalMeatovItem.physicalMeatovItem, true);
                            }
                            else if (childMeatovItemData.parentSlotIndex != -1 && physicalMeatovItem.physicalMeatovItem.rigSlots != null
                                && physicalMeatovItem.physicalMeatovItem.rigslots.Count > childMeatovItemData.parentSlotIndex)
                            {
                                childMeatovItemData.physicalMeatovItem.physicalMeatovItem.physObj.SetQuickBeltSlot(physicalMeatovItem.physicalMeatovItem.rigslots[childMeatovItemData.parentSlotIndex]);
                            }
                        }

                        // Call update on child in case it needs to process its new parent somehow
                        // This is needed for attachments that did their latest update before we got their parent's phys
                        // Calling this update will let them mount themselves to their mount properly
                        childObject.UpdateFromData(childObject);
                    }
                }
                childrenToParent.Clear();
            }
            catch (Exception e)
            {
                Mod.LogError("Error while trying to instantiate meatov item: " + itemID + ":\n" + e.Message + "\n" + e.StackTrace);
            }
        }

        public override void UpdateFromData(TrackedObjectData updatedObject, bool full = false)
        {
            base.UpdateFromData(updatedObject, full);

            TrackedMeatovItemData updatedMeatovItem = updatedObject as TrackedMeatovItemData;

            if (full)
            {
                tarkovID = updatedMeatovItem.tarkovID;
                dogtagLevel = updatedMeatovItem.dogtagLevel;
                dogtagName = updatedMeatovItem.dogtagName;
            }

            previousParentSlotIndex = parentSlotIndex;
            parentSlotIndex = updatedMeatovItem.parentSlotIndex;
            previousInsured = insured;
            insured = updatedMeatovItem.insured;
            previousFoundInRaid = foundInRaid;
            foundInRaid = updatedMeatovItem.foundInRaid;
            previousMode = mode;
            mode = updatedMeatovItem.mode;
            previousOpen = open;
            open = updatedMeatovItem.open;
            previousBroken = broken;
            broken = updatedMeatovItem.broken;
            previousStack = stack;
            stack = updatedMeatovItem.stack;
            previousAmount = amount;
            amount = updatedMeatovItem.amount;

            if(physical != null)
            {
                if(previousParentSlotIndex != parentSlotIndex)
                {
                    if(parent != -1)
                    {
                        TrackedMeatovItemData parentMeatovItemData = (ThreadManager.host ? Server.objects : Client.objects)[parent] as TrackedMeatovItemData;
                        if (parentSlotIndex == -1)
                        {
                            if(physicalMeatovItem.physicalMeatovItem.physObj.QuickbeltSlot != null)
                            {
                                // Remove from any parent QBS it might be in
                                List<RigSlot> rigSlots = parentMeatovItemData.physicalMeatovItem.physicalMeatovItem.rigSlots;
                                for (int i = 0; i < rigSlots.Count; ++i)
                                {
                                    if(rigSlots[i] == physicalItem.physicalItem.QuickbeltSlot)
                                    {
                                        physicalMeatovItem.physicalMeatovItem.physObj.SetQuickBeltSlot(null);
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Remove from current QBS if in QBS
                            if (physicalMeatovItem.physicalMeatovItem.physObj.QuickbeltSlot != null)
                            {
                                physicalMeatovItem.physicalMeatovItem.physObj.SetQuickBeltSlot(null);
                            }

                            // Add to correct parent QBS
                            physicalMeatovItem.physicalMeatovItem.physObj.SetQuickBeltSlot(parentMeatovItemData.physicalMeatovItem.physicalMeatovItem.rigSlots[parentSlotIndex]);
                        }
                    }
                }
                physicalMeatovItem.physicalMeatovItem.insured = insured;
                physicalMeatovItem.physicalMeatovItem.foundInRaid = foundInRaid;
                if(previousMode != mode || previousOpen != open)
                {
                    physicalMeatovItem.physicalMeatovItem.UpdateMode(mode, open);
                }
                physicalMeatovItem.physicalMeatovItem.broken = broken;
                physicalMeatovItem.physicalMeatovItem.stack = stack;
                physicalMeatovItem.physicalMeatovItem.amount = amount;
            }
        }

        public override void UpdateFromPacket(Packet packet, bool full = false)
        {
            base.UpdateFromPacket(packet, full);

            if (full)
            {
                tarkovID = packet.ReadString();
                dogtagLevel = packet.ReadInt();
                dogtagName = packet.ReadString();
            }

            previousParentSlotIndex = parentSlotIndex;
            parentSlotIndex = packet.ReadInt();
            previousInsured = insured;
            insured = packet.ReadBool();
            previousFoundInRaid = foundInRaid;
            foundInRaid = packet.ReadBool();
            previousMode = mode;
            mode = packet.ReadInt();
            previousOpen = open;
            open = packet.ReadBool();
            previousBroken = broken;
            broken = packet.ReadBool();
            previousStack = stack;
            stack = packet.ReadInt();
            previousAmount = amount;
            amount = packet.ReadInt();

            if (physical != null)
            {
                if (previousParentSlotIndex != parentSlotIndex)
                {
                    if (parent != -1)
                    {
                        TrackedMeatovItemData parentMeatovItemData = (ThreadManager.host ? Server.objects : Client.objects)[parent] as TrackedMeatovItemData;
                        if (parentSlotIndex == -1)
                        {
                            if (physicalMeatovItem.physicalMeatovItem.physObj.QuickbeltSlot != null)
                            {
                                // Remove from any parent QBS it might be in
                                List<RigSlot> rigSlots = parentMeatovItemData.physicalMeatovItem.physicalMeatovItem.rigSlots;
                                for (int i = 0; i < rigSlots.Count; ++i)
                                {
                                    if (rigSlots[i] == physicalItem.physicalItem.QuickbeltSlot)
                                    {
                                        physicalMeatovItem.physicalMeatovItem.physObj.SetQuickBeltSlot(null);
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Remove from current QBS if in QBS
                            if (physicalMeatovItem.physicalMeatovItem.physObj.QuickbeltSlot != null)
                            {
                                physicalMeatovItem.physicalMeatovItem.physObj.SetQuickBeltSlot(null);
                            }

                            // Add to correct parent QBS
                            physicalMeatovItem.physicalMeatovItem.physObj.SetQuickBeltSlot(parentMeatovItemData.physicalMeatovItem.physicalMeatovItem.rigSlots[parentSlotIndex]);
                        }
                    }
                }
                physicalMeatovItem.physicalMeatovItem.insured = insured;
                physicalMeatovItem.physicalMeatovItem.foundInRaid = foundInRaid;
                if (previousMode != mode || previousOpen != open)
                {
                    physicalMeatovItem.physicalMeatovItem.UpdateMode(mode, open);
                }
                physicalMeatovItem.physicalMeatovItem.broken = broken;
                physicalMeatovItem.physicalMeatovItem.stack = stack;
                physicalMeatovItem.physicalMeatovItem.amount = amount;
            }
        }

        public override bool Update(bool full = false)
        {
            bool updated = base.Update(full);

            // Phys could be null if we were given control of the item while we were loading and we haven't instantiated it on our side yet
            if (physical == null)
            {
                return false;
            }

            if (full)
            {
                tarkovID = physicalMeatovItem.physicalMeatovItem.tarkovID;
                dogtagLevel = physicalMeatovItem.physicalMeatovItem.dogtagLevel;
                dogtagName = physicalMeatovItem.physicalMeatovItem.dogtagName;
            }

            cont from here // Update data with physical  object state
            previousParentSlotIndex = parentSlotIndex;
            parentSlotIndex = packet.ReadInt();
            previousInsured = insured;
            insured = packet.ReadBool();
            previousFoundInRaid = foundInRaid;
            foundInRaid = packet.ReadBool();
            previousMode = mode;
            mode = packet.ReadInt();
            previousOpen = open;
            open = packet.ReadBool();
            previousBroken = broken;
            broken = packet.ReadBool();
            previousStack = stack;
            stack = packet.ReadInt();
            previousAmount = amount;
            amount = packet.ReadInt();

            return updated || previousActiveControl != underActiveControl || !previousPos.Equals(position) || !previousRot.Equals(rotation);
        }
    }
}
