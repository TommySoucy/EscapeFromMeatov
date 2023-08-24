﻿using FistVR;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EFM
{
    public class VanillaItemDescriptor : MonoBehaviour, Describable
    {
        public bool destroyed;

        public string H3ID; // ID of item in H3VR
        public string tarkovID; // ID of item in Tarkov
        public string description; // Item's description
        public int lootExperience; // Experience gained upon pickup
        public Mod.ItemRarity rarity; // Rarity of item
        public float spawnChance; // Spawn chance of item
        public int creditCost; // Value of item in rubles(?)
        public List<string> parents; // The parent IDs of this item, the categories this item is a part of
        public bool looted; // Whether this item has been looted before
        public bool foundInRaid;
        public bool hideoutSpawned;
        public string itemName;
        public FVRPhysicalObject physObj; // Reference to the physical object of this item
        public int compatibilityValue; // 0: Does not need mag or round, 1: Needs mag, 2: Needs round, 3: Needs both
        public bool usesMags; // Could be clip
        public bool usesAmmoContainers; // Could be internal mag or revolver
        public FireArmMagazineType magType;
        public FireArmClipType clipType;
        public FireArmRoundType roundType;
        public DescriptionPack descriptionPack;
        public bool takeCurrentLocation = true; // This dictates whether this item should take the current global location index or if it should wait to be set manually
        public int locationIndex; // 0: Player inventory, 1: Base, 2: Raid. This is to keep track of where an item is in general
        public DescriptionManager descriptionManager; // The current description manager displaying this item's description
        public List<MarketItemView> marketItemViews;
        public LeaveItemProcessor leaveItemProcessor;
        public int upgradeCheckBlockedIndex = -1;
        public int upgradeCheckWarnedIndex = -1;
        public bool inAll = true;
        public int weight; // The original weight of the item. We need to keep it ourselves because we would usually use the RB mass but the RB gets destroyed when a mag gets put in a firearm
        public int volume;
        public Mod.WeaponClass weaponClass;
        private long previousDescriptionTime;
        private bool validLeaveItemPress;
        private float leavingTimer;
        private float leaveItemTime = -1;
        private bool _insured;
        public bool insured
        {
            get { return _insured; }
            set
            {
                _insured = value;
                if (descriptionManager != null)
                {
                    descriptionManager.SetDescriptionPack();
                }
            }
        }
        private int _currentWeight; // Includes attachments and ammo containers attached to this item
        public int currentWeight
        {
            get { return _currentWeight; }
            set
            {
                _currentWeight = value;
                if (descriptionManager != null)
                {
                    descriptionManager.SetDescriptionPack();
                }
            }
        }

        public void TakeInput(FVRViveHand hand, Hand EFMHand)
        {
            bool usageButtonPressed = false;
            if (hand.CMode == ControlMode.Index || hand.CMode == ControlMode.Oculus)
            {
                usageButtonPressed = hand.Input.AXButtonPressed;
            }
            else if (hand.CMode == ControlMode.Vive || hand.CMode == ControlMode.WMR)
            {
                Vector2 touchpadAxes = hand.Input.TouchpadAxes;
                if (touchpadAxes.magnitude > 0.3f && Vector2.Angle(touchpadAxes, Vector2.down) <= 45f)
                {
                    usageButtonPressed = hand.Input.TouchpadPressed;
                }
            }

            // If A is being pressed this frame
            if (usageButtonPressed)
            {
                if (leaveItemProcessor != null)
                {
                    bool otherHandLeaving = false;
                    if (!validLeaveItemPress)
                    {
                        otherHandLeaving = EFMHand.otherHand.leaving;
                    }
                    if (!otherHandLeaving)
                    {
                        EFMHand.leaving = true;

                        // Increment timer
                        leavingTimer += Time.deltaTime;

                        if (leaveItemTime == -1)
                        {
                            List<TraderTaskCondition> conditions = leaveItemProcessor.conditionsByItemID[H3ID];
                            leaveItemTime = conditions[conditions.Count - 1].plantTime;
                        }

                        Mod.consumeUIText.text = string.Format("{0:0.#}/{1:0.#}", leavingTimer, leaveItemTime);
                        Mod.consumeUIText.color = Color.white;
                        Mod.consumeUI.transform.parent = hand.transform;
                        Mod.consumeUI.transform.localPosition = new Vector3(hand.IsThisTheRightHand ? -0.15f : 0.15f, 0, 0);
                        Mod.consumeUI.transform.localRotation = Quaternion.Euler(25, 0, 0);
                        Mod.consumeUI.SetActive(true);
                        validLeaveItemPress = true;

                        if (leavingTimer >= leaveItemTime)
                        {
                            EFMHand.leaving = false;
                            validLeaveItemPress = false;
                            leavingTimer = 0;
                            leaveItemTime = -1;

                            List<TraderTaskCondition> conditions = leaveItemProcessor.conditionsByItemID[H3ID];
                            if (conditions.Count > 0)
                            {
                                TraderTaskCondition condition = conditions[conditions.Count - 1];
                                ++condition.itemCount;
                                TraderStatus.UpdateConditionFulfillment(condition);
                                if (condition.fulfilled)
                                {
                                    conditions.RemoveAt(conditions.Count - 1);
                                }
                            }
                            if (conditions.Count == 0)
                            {
                                leaveItemProcessor.conditionsByItemID.Remove(H3ID);
                                leaveItemProcessor.itemIDs.Remove(H3ID);
                            }

                            // Update player inventory and weight
                            Mod.RemoveFromPlayerInventory(transform, true);
                            Mod.weight -= currentWeight;
                            destroyed = true;
                            physObj.ForceBreakInteraction();
                            Destroy(gameObject);
                        }
                    }
                }
            }
        }

        public void Awake()
        {
            // Set the reference to the physical object
            physObj = gameObject.GetComponent<FVRPhysicalObject>();

            if (physObj.RootRigidbody == null)
            {
                // This has to be done because attachments and ammo container, when attached to a firearm, will have their rigidbodies detroyed
                weight = (int)(((FVRPhysicalObject.RigidbodyStoredParams)typeof(FVRPhysicalObject).GetField("StoredRBParams").GetValue(physObj)).Mass * 1000);
            }
            else
            {
                weight = (int)(physObj.RootRigidbody.mass * 1000);
            }

            if (physObj is FVRFireArm)
            {
                FVRFireArm asFireArm = physObj as FVRFireArm;

                // Set chamber firearm vars TODO: Check if this is necessary, shouldnt already be done by h3?
                if (asFireArm.GetChambers() != null && asFireArm.GetChambers().Count > 0)
                {
                    foreach (FVRFireArmChamber chamber in asFireArm.GetChambers())
                    {
                        chamber.Firearm = asFireArm;
                    }
                }
            }


            if (takeCurrentLocation)
            {
                locationIndex = Mod.currentLocationIndex;
            }

            descriptionPack = new DescriptionPack();
            descriptionPack.ID = H3ID;
            descriptionPack.isCustom = false;
            descriptionPack.isPhysical = true;
            descriptionPack.vanillaItem = this;
            descriptionPack.name = itemName;
            descriptionPack.description = description;

            try
            {
                // Some vanilla items have custom icons, look there first
                if (Mod.itemIcons.ContainsKey(H3ID))
                {
                    descriptionPack.icon = Mod.itemIcons[H3ID];
                }
                else
                {
                    descriptionPack.icon = physObj is FVRFireArmRound ? Mod.cartridgeIcon : IM.GetSpawnerID(physObj.ObjectWrapper.SpawnedFromId).Sprite;
                }
            }
            catch (Exception)
            {
                Mod.LogError("Could not get spawner ID for icon for vanilla item: " + H3ID);
            }
            descriptionPack.amountRequiredPerArea = new int[22];

            // Set init weight
            SetCurrentWeight(this);
        }

        public static int SetCurrentWeight(VanillaItemDescriptor item)
        {
            if(item == null)
            {
                return 0;
            }

            item.currentWeight = item.weight;

            if (item.physObj is FVRFireArm)
            {
                FVRFireArm asFireArm = (FVRFireArm)item.physObj;

                // Considering 0.015g per round
                // TODO: Ammo container weight management will have to be reviewed. If we want to manage it, we will need to also keep track of the round and container's
                // locationIndex so we know when to add/remove weight from player also
                //item.currentWeight += 15 * (asFireArm.GetChamberRoundList() == null ? 0 : asFireArm.GetChamberRoundList().Count);

                // Ammo container
                if (asFireArm.UsesMagazines && asFireArm.Magazine != null)
                {
                    VanillaItemDescriptor magVID = asFireArm.Magazine.GetComponent<VanillaItemDescriptor>();
                    if (magVID != null)
                    {
                        item.currentWeight += SetCurrentWeight(magVID);
                    }
                }
                else if (asFireArm.UsesClips && asFireArm.Clip != null)
                {
                    VanillaItemDescriptor clipVID = asFireArm.Clip.GetComponent<VanillaItemDescriptor>();
                    if (clipVID != null)
                    {
                        item.currentWeight += SetCurrentWeight(clipVID);
                    }
                }

                // Attachments
                if (asFireArm.Attachments != null && asFireArm.Attachments.Count > 0)
                {
                    foreach (FVRFireArmAttachment attachment in asFireArm.Attachments)
                    {
                        item.currentWeight += SetCurrentWeight(attachment.GetComponent<VanillaItemDescriptor>());
                    }
                }
            }
            else if (item.physObj is FVRFireArmAttachment)
            {
                FVRFireArmAttachment asFireArmAttachment = (FVRFireArmAttachment)item.physObj;

                if (asFireArmAttachment.Attachments != null && asFireArmAttachment.Attachments.Count > 0)
                {
                    foreach (FVRFireArmAttachment attachment in asFireArmAttachment.Attachments)
                    {
                        item.currentWeight += SetCurrentWeight(attachment.GetComponent<VanillaItemDescriptor>());
                    }
                }
            }
            else if (item.physObj is FVRFireArmMagazine)
            {
                FVRFireArmMagazine asFireArmMagazine = (FVRFireArmMagazine)item.physObj;

                // TODO: Ammo container weight management will have to be reviewed. If we want to manage it, we will need to also keep track of the round and container's
                // locationIndex so we know when to add/remove weight from player also
                //item.currentWeight += 15 * asFireArmMagazine.m_numRounds;
            }
            else if (item.physObj is FVRFireArmClip)
            {
                FVRFireArmClip asFireArmClip = (FVRFireArmClip)item.physObj;

                // TODO: Ammo container weight management will have to be reviewed. If we want to manage it, we will need to also keep track of the round and container's
                // locationIndex so we know when to add/remove weight from player also
                //item.currentWeight += 15 * asFireArmClip.m_numRounds;
            }
            else if(item.GetComponentInChildren<M203>() != null)
            {
                M203 m203 = item.GetComponentInChildren<M203>();
                item.currentWeight += m203.Chamber.IsFull ? 100 : 0;
            }

            return item.currentWeight;
        }

        public DescriptionPack GetDescriptionPack()
        {
            if (physObj == null)
            {
                physObj = gameObject.GetComponent<FVRPhysicalObject>();
            }
            if (physObj is FVRFireArmMagazine) 
            { 
                // Checking amount of ammo in mag only counts once per minute for a unique mag
                long currentTime = Mod.currentLocationIndex == 1 ? Mod.currentBaseManager.GetTimeSeconds() : Mod.currentRaidManager.GetTimeSeconds();
                if (currentTime - previousDescriptionTime > 60)
                {
                    Mod.AddSkillExp(Skill.magazineCheckAction, 31);
                }
                previousDescriptionTime = currentTime;
            }

            descriptionPack.amount = (Mod.baseInventory.ContainsKey(H3ID) ? Mod.baseInventory[H3ID] : 0) + (Mod.playerInventory.ContainsKey(H3ID) ? Mod.playerInventory[H3ID] : 0);
            for (int i = 0; i < 22; ++i)
            {
                if (Mod.requiredPerArea[i] != null && Mod.requiredPerArea[i].ContainsKey(H3ID))
                {
                    descriptionPack.amountRequired += Mod.requiredPerArea[i][H3ID];
                    descriptionPack.amountRequiredPerArea[i] = Mod.requiredPerArea[i][H3ID];
                }
                else
                {
                    descriptionPack.amountRequiredPerArea[i] = 0;
                }
            }
            descriptionPack.onWishlist = Mod.wishList.Contains(H3ID);
            descriptionPack.insured = insured;

            if (compatibilityValue == 3 || compatibilityValue == 1)
            {
                if (usesAmmoContainers)
                {
                    if (usesMags)
                    {
                        if (Mod.magazinesByType.ContainsKey(magType))
                        {
                            descriptionPack.compatibleAmmoContainers = Mod.magazinesByType[magType];
                        }
                        else
                        {
                            descriptionPack.compatibleAmmoContainers = new Dictionary<string, int>();
                        }
                    }
                    else
                    {
                        if (Mod.clipsByType.ContainsKey(clipType))
                        {
                            descriptionPack.compatibleAmmoContainers = Mod.clipsByType[clipType];
                        }
                        else
                        {
                            descriptionPack.compatibleAmmoContainers = new Dictionary<string, int>();
                        }
                    }
                }
            }
            if (compatibilityValue == 3 || compatibilityValue == 2)
            {
                if (Mod.roundsByType.ContainsKey(roundType))
                {
                    descriptionPack.compatibleAmmo = Mod.roundsByType[roundType];
                }
                else
                {
                    descriptionPack.compatibleAmmo = new Dictionary<string, int>();
                }
            }
            descriptionPack.weight = weight;
            descriptionPack.volume = volume;
            descriptionPack.amountRequiredQuest = Mod.requiredForQuest.ContainsKey(H3ID) ? Mod.requiredForQuest[H3ID] : 0;
            FVRFireArmMagazine asMagazine = gameObject.GetComponent<FVRFireArmMagazine>();
            FVRFireArmClip asClip = gameObject.GetComponent<FVRFireArmClip>();
            descriptionPack.containedAmmoClassesByType = new Dictionary<FireArmRoundType, Dictionary<FireArmRoundClass, int>>();
            if (asMagazine != null)
            {
                descriptionPack.stack = asMagazine.m_numRounds;
                descriptionPack.maxStack = asMagazine.m_capacity;
                foreach (FVRLoadedRound loadedRound in asMagazine.LoadedRounds)
                {
                    if (loadedRound != null)
                    {
                        if (descriptionPack.containedAmmoClassesByType.ContainsKey(asMagazine.RoundType))
                        {
                            if (descriptionPack.containedAmmoClassesByType[asMagazine.RoundType].ContainsKey(loadedRound.LR_Class))
                            {
                                descriptionPack.containedAmmoClassesByType[asMagazine.RoundType][loadedRound.LR_Class] += 1;
                            }
                            else
                            {
                                descriptionPack.containedAmmoClassesByType[asMagazine.RoundType].Add(loadedRound.LR_Class, 1);
                            }
                        }
                        else
                        {
                            Dictionary<FireArmRoundClass, int> newDict = new Dictionary<FireArmRoundClass, int>();
                            newDict.Add(loadedRound.LR_Class, 1);
                            descriptionPack.containedAmmoClassesByType.Add(asMagazine.RoundType, newDict);
                        }
                    }
                }
            }
            else if(asClip != null)
            {
                descriptionPack.stack = asClip.m_numRounds;
                descriptionPack.maxStack = asClip.m_capacity;
                foreach (FVRFireArmClip.FVRLoadedRound loadedRound in asClip.LoadedRounds)
                {
                    if (loadedRound != null)
                    {
                        if (descriptionPack.containedAmmoClassesByType.ContainsKey(asClip.RoundType))
                        {
                            if (descriptionPack.containedAmmoClassesByType[asClip.RoundType].ContainsKey(loadedRound.LR_Class))
                            {
                                descriptionPack.containedAmmoClassesByType[asClip.RoundType][loadedRound.LR_Class] += 1;
                            }
                            else
                            {
                                descriptionPack.containedAmmoClassesByType[asClip.RoundType].Add(loadedRound.LR_Class, 1);
                            }
                        }
                        else
                        {
                            Dictionary<FireArmRoundClass, int> newDict = new Dictionary<FireArmRoundClass, int>();
                            newDict.Add(loadedRound.LR_Class, 1);
                            descriptionPack.containedAmmoClassesByType.Add(asClip.RoundType, newDict);
                        }
                    }
                }
            }
            descriptionPack.foundInRaid = foundInRaid;

            return descriptionPack;
        }

        public int GetValue()
        {
            return creditCost;

            // TODO: Maybe add all values of sub items attached to this one too but will have to adapt  market for it, for example, when we insure, we only ensure the root item not sub items
        }

        public int GetInsuranceValue()
        {
            // Thsi inureability of the current item will be checked by the calling method
            return creditCost;

            // TODO: WE DONT CHECK FOR INSUREABILITY HERE FOR THIS OBJECT, BUT WE MUST FOR SUB OBJECTS WHEN WE IMPLEMENT THAT
            // TODO: Maybe add all values of sub items attached to this one too but will have to adapt  market for it, for example, when we insure, we only ensure the root item not sub items
        }

        public void SetDescriptionManager(DescriptionManager descriptionManager)
        {
            this.descriptionManager = descriptionManager;
        }

        public void Highlight(Color color)
        {
            MeshRenderer[] mrs = gameObject.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer mr in mrs)
            {
                mr.material.EnableKeyword("_RIM_ON");
                mr.material.SetColor("_RimColor", color);
            }
        }

        public void RemoveHighlight()
        {
            MeshRenderer[] mrs = gameObject.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer mr in mrs)
            {
                mr.material.DisableKeyword("_RIM_ON");
            }
        }
    }
}