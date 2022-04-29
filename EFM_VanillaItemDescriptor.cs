using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class EFM_VanillaItemDescriptor : MonoBehaviour, EFM_Describable
    {
        public string H3ID; // ID of item in H3VR
        public string tarkovID; // ID of item in Tarkov
        public string description; // Item's description
        public int lootExperience; // Experience gained upon pickup
        public Mod.ItemRarity rarity; // Rarity of item
        public float spawnChance; // Spawn chance of item
        public int creditCost; // Value of item in rubles(?)
        public Sprite sprite; // Icon for this item, only assigned if it has no custom icon
        public string parent; // The parent ID of this item, the category this item is really
        public bool looted; // Whether this item has been looted before
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
        public EFM_DescriptionManager descriptionManager; // The current description manager displaying this item's description
        public float weight; // The original weight of the item. We need to keep it ourselves because we would usually use the RB mass but the RB gets destroyed when a mag gets put in a firearm
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
        private float _currentWeight; // Includes attachments and ammo containers attached to this item
        public float currentWeight
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

        public void Start()
        {
            // Set the reference to the physical object
            physObj = gameObject.GetComponent<FVRPhysicalObject>();
            weight = physObj.RootRigidbody.mass;

            if(physObj is FVRFireArm)
            {
                FVRFireArm asFireArm = physObj as FVRFireArm;

                // Set chamber firearm vars
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
            descriptionPack.isCustom = false;
            descriptionPack.vanillaItem = this;
            descriptionPack.name = itemName;
            descriptionPack.description = description;
            descriptionPack.icon = physObj is FVRFireArmRound ? null /*TODO have a default icon for all rounds?*/ : IM.GetSpawnerID(physObj.ObjectWrapper.SpawnedFromId).Sprite;
            descriptionPack.amountRequiredPerArea = new int[22];

            // Set init weight
            SetCurrentWeight(this);
        }

        public static float SetCurrentWeight(EFM_VanillaItemDescriptor item)
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
                item.currentWeight += 0.015f * (asFireArm.GetChamberRoundList() == null ? 0 : asFireArm.GetChamberRoundList().Count);

                // Ammo container
                if (asFireArm.UsesMagazines && asFireArm.Magazine != null)
                {
                    item.currentWeight += SetCurrentWeight(asFireArm.Magazine.GetComponent<EFM_VanillaItemDescriptor>());
                }
                else if (asFireArm.UsesClips && asFireArm.Clip != null)
                {
                    item.currentWeight += SetCurrentWeight(asFireArm.Clip.GetComponent<EFM_VanillaItemDescriptor>());
                }

                // Attachments
                if (asFireArm.Attachments != null && asFireArm.Attachments.Count > 0)
                {
                    foreach (FVRFireArmAttachment attachment in asFireArm.Attachments)
                    {
                        item.currentWeight += SetCurrentWeight(attachment.GetComponent<EFM_VanillaItemDescriptor>());
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
                        item.currentWeight += SetCurrentWeight(attachment.GetComponent<EFM_VanillaItemDescriptor>());
                    }
                }
            }
            else if (item.physObj is FVRFireArmMagazine)
            {
                FVRFireArmMagazine asFireArmMagazine = (FVRFireArmMagazine)item.physObj;

                item.currentWeight += 0.015f * asFireArmMagazine.m_numRounds;
            }
            else if (item.physObj is FVRFireArmClip)
            {
                FVRFireArmClip asFireArmClip = (FVRFireArmClip)item.physObj;

                item.currentWeight += 0.015f * asFireArmClip.m_numRounds;
            }
            else if(item.GetComponentInChildren<M203>() != null)
            {
                M203 m203 = item.GetComponentInChildren<M203>();
                item.currentWeight += m203.Chamber.IsFull ? 0.1f : 0;
            }

            return item.currentWeight;
        }

        public DescriptionPack GetDescriptionPack()
        {
            Mod.instance.LogInfo("Vanilla getdesc pack called on "+gameObject.name);
            descriptionPack.amount = (Mod.baseInventory.ContainsKey(H3ID) ? Mod.baseInventory[H3ID] : 0) + (Mod.playerInventory.ContainsKey(H3ID) ? Mod.playerInventory[H3ID] : 0);
            Mod.instance.LogInfo("0");
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
            Mod.instance.LogInfo("0");
            descriptionPack.onWishlist = Mod.wishList.Contains(H3ID);
            Mod.instance.LogInfo("0");
            descriptionPack.insured = insured;
            Mod.instance.LogInfo("0");

            if (compatibilityValue == 3 || compatibilityValue == 1)
            {
                Mod.instance.LogInfo("\t0");
                if (usesAmmoContainers)
                {
                    Mod.instance.LogInfo("\t\t0");
                    if (usesMags)
                    {
                        Mod.instance.LogInfo("\t\t\t0");
                        if (Mod.magazinesByType.ContainsKey(magType))
                        {
                            Mod.instance.LogInfo("\t\t\t\t0");
                            descriptionPack.compatibleAmmoContainers = Mod.magazinesByType[magType];
                        }
                        else
                        {
                            Mod.instance.LogInfo("\t\t\t\t0");
                            descriptionPack.compatibleAmmoContainers = new Dictionary<string, int>();
                        }
                    }
                    else
                    {
                        Mod.instance.LogInfo("\t\t\t0");
                        if (Mod.clipsByType.ContainsKey(clipType))
                        {
                            Mod.instance.LogInfo("\t\t\t\t0");
                            descriptionPack.compatibleAmmoContainers = Mod.clipsByType[clipType];
                        }
                        else
                        {
                            Mod.instance.LogInfo("\t\t\t\t0");
                            descriptionPack.compatibleAmmoContainers = new Dictionary<string, int>();
                        }
                    }
                }
            }
            Mod.instance.LogInfo("0");
            if (compatibilityValue == 3 || compatibilityValue == 2)
            {
                Mod.instance.LogInfo("\t0");
                if (Mod.roundsByType.ContainsKey(roundType))
                {
                    Mod.instance.LogInfo("\t\t0");
                    descriptionPack.compatibleAmmo = Mod.roundsByType[roundType];
                }
                else
                {
                    Mod.instance.LogInfo("\t\t0");
                    descriptionPack.compatibleAmmo = new Dictionary<string, int>();
                }
            }
            Mod.instance.LogInfo("0");
            descriptionPack.weight = weight;
            Mod.instance.LogInfo("0");
            descriptionPack.volume = Mod.sizeVolumes[(int)physObj.Size];
            Mod.instance.LogInfo("0");
            descriptionPack.amountRequiredQuest = Mod.requiredForQuest.ContainsKey(H3ID) ? Mod.requiredForQuest[H3ID] : 0;
            Mod.instance.LogInfo("0");
            FVRFireArmMagazine asMagazine = gameObject.GetComponent<FVRFireArmMagazine>();
            Mod.instance.LogInfo("0");
            FVRFireArmClip asClip = gameObject.GetComponent<FVRFireArmClip>();
            Mod.instance.LogInfo("0");
            descriptionPack.containedAmmoClasses = new Dictionary<string, int>();
            if (asMagazine != null)
            {
                foreach (FVRLoadedRound loadedRound in asMagazine.LoadedRounds)
                {
                    if (loadedRound != null)
                    {
                        if (descriptionPack.containedAmmoClasses.ContainsKey(loadedRound.LR_Class.ToString()))
                        {
                            descriptionPack.containedAmmoClasses[loadedRound.LR_Class.ToString()] += 1;
                        }
                        else
                        {
                            descriptionPack.containedAmmoClasses.Add(loadedRound.LR_Class.ToString(), 1);
                        }
                    }
                }
            }
            else if(asClip != null)
            {
                foreach (FVRFireArmClip.FVRLoadedRound loadedRound in asClip.LoadedRounds)
                {
                    if (loadedRound != null)
                    {
                        if (descriptionPack.containedAmmoClasses.ContainsKey(loadedRound.LR_Class.ToString()))
                        {
                            descriptionPack.containedAmmoClasses[loadedRound.LR_Class.ToString()] += 1;
                        }
                        else
                        {
                            descriptionPack.containedAmmoClasses.Add(loadedRound.LR_Class.ToString(), 1);
                        }
                    }
                }
            }

            Mod.instance.LogInfo("returning desc");
            return descriptionPack;
        }
    }
}
