using FistVR;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class ItemDescriptionListEntryUI : MonoBehaviour
    {
        public ItemDescriptionUI owner;

        public GameObject fulfilledIcon;
        public GameObject unfulfilledIcon;
        public Text amount;
        public Text entryName;
        public Text entryInfo;
        public Button button;

        private int entryCount;

        public void SetTask(ItemDescriptionUI owner, Task task, long currentCount, int neededCount)
        {
            // Note that an entry like this one does not subscribe to events to update its amount in realtime
            // We consider item descriptions to be shortlived and as such, don't need to be updated
            // Up to date numbers will simply be displayed when we make a new description of the item
            this.owner = owner;
            if(currentCount >= neededCount)
            {
                fulfilledIcon.SetActive(true);
                unfulfilledIcon.SetActive(false);
            }
            else
            {
                fulfilledIcon.SetActive(false);
                unfulfilledIcon.SetActive(true);
            }
            amount.text = currentCount.ToString() + "/" + neededCount;
            entryName.text = task.name;
            entryName.color = Mod.neededForColors[0];
            entryInfo.text = task.trader.name;
        }

        public void SetAreaLevel(ItemDescriptionUI owner, int areaIndex, int level, long currentCount, int neededCount)
        {
            this.owner = owner;
            if(currentCount >= neededCount)
            {
                fulfilledIcon.SetActive(true);
                unfulfilledIcon.SetActive(false);
                entryName.color = Mod.neededForAreaFulfilledColor;
            }
            else
            {
                fulfilledIcon.SetActive(false);
                unfulfilledIcon.SetActive(true);
                entryName.color = Mod.neededForColors[1];
            }
            amount.text = currentCount.ToString() + "/" + neededCount;
            entryName.text = Area.IndexToName(areaIndex) + " lvl " + level;
        }

        public void SetBarter(ItemDescriptionUI owner, Barter barter, int traderIndex, int level, long currentCount, int neededCount)
        {
            this.owner = owner;
            if(currentCount >= neededCount)
            {
                fulfilledIcon.SetActive(true);
                unfulfilledIcon.SetActive(false);
            }
            else
            {
                fulfilledIcon.SetActive(false);
                unfulfilledIcon.SetActive(true);
            }
            amount.text = currentCount.ToString() + "/" + neededCount;
            entryName.text = barter.itemData.name;
            entryName.color = Mod.neededForColors[3];
            entryInfo.text = Mod.traders[traderIndex].name + " lvl " + level;
        }

        public void SetProduction(ItemDescriptionUI owner, Production production, int areaIndex, int level, long currentCount, int neededCount)
        {
            this.owner = owner;
            if(currentCount >= neededCount)
            {
                fulfilledIcon.SetActive(true);
                unfulfilledIcon.SetActive(false);
            }
            else
            {
                fulfilledIcon.SetActive(false);
                unfulfilledIcon.SetActive(true);
            }
            amount.text = currentCount.ToString() + "/" + neededCount;
            entryName.text = production.endProduct.name;
            entryName.color = Mod.neededForColors[4];
            entryInfo.text = Area.IndexToName(areaIndex) + " lvl " + level;
        }

        public void SetAmmoContainer(ItemDescriptionUI owner, MeatovItemData containerData, int hideoutCount, int playerCount, bool isMag)
        {
            this.owner = owner;
            fulfilledIcon.SetActive(false);
            unfulfilledIcon.SetActive(false);
            entryName.text = containerData.name;
            if(owner.descriptionPack.item == null)
            {
                button.gameObject.SetActive(false);
            }
            else
            {
                button.gameObject.SetActive(true);
                string containerID = containerData.H3ID;
                bool mag = isMag;
                button.onClick.AddListener(() => OnLoadAmmoContainerClicked(containerID, mag));
            }
            entryCount = hideoutCount + playerCount;
            amount.text = "(" + entryCount + ")";
        }

        public void SetAmmo(ItemDescriptionUI owner, MeatovItemData roundData, int hideoutCount, int playerCount, int ammoBoxCount)
        {
            this.owner = owner;
            fulfilledIcon.SetActive(false);
            unfulfilledIcon.SetActive(false);
            entryName.text = roundData.name;
            if(owner.descriptionPack.item == null)
            {
                button.gameObject.SetActive(false);
            }
            else
            {
                button.gameObject.SetActive(true);
                MeatovItemData roundDataToUse = roundData;
                button.onClick.AddListener(() => OnFillRoundsClicked(roundDataToUse));
            }
            entryCount = hideoutCount + playerCount + ammoBoxCount;
            amount.text = "(" + entryCount + ")";
        }

        public void OnFillRoundsClicked(MeatovItemData roundData)
        {
            int countLeft = 0;
            FVRFireArmMagazine asMag = null;
            FVRFireArmMagazine asClip = null;
            Speedloader asSL = null;
            int typeIndex = -1;
            if (owner.descriptionPack.item != null)
            {
                if(owner.descriptionPack.item.physObj is FVRFireArmMagazine)
                {
                    typeIndex = 0;
                    asMag = owner.descriptionPack.item.physObj as FVRFireArmMagazine;
                    if(asMag.m_numRounds >= asMag.m_capacity)
                    {
                        return;
                    }
                    else
                    {
                        countLeft = asMag.m_capacity - asMag.m_numRounds;
                    }
                }
                else if(owner.descriptionPack.item.physObj is FVRFireArmClip)
                {
                    typeIndex = 1;
                    asClip = owner.descriptionPack.item.physObj as FVRFireArmMagazine;
                    if (asClip.m_numRounds >= asClip.m_capacity)
                    {
                        return;
                    }
                    else
                    {
                        countLeft = asClip.m_capacity - asClip.m_numRounds;
                    }
                }
                else if(owner.descriptionPack.item.physObj is Speedloader)
                {
                    typeIndex = 2;
                    asSL = owner.descriptionPack.item.physObj as Speedloader;
                    int filledCount = 0;
                    for(int i=0; i < asSL.Chambers.Count; ++i)
                    {
                        if (asSL.Chambers[i].IsLoaded)
                        {
                            ++filledCount;
                        }
                    }
                    if (filledCount == asSL.Chambers.Count)
                    {
                        return;
                    }
                    else
                    {
                        countLeft = asSL.Chambers.Count - filledCount;
                    }
                }
                else // Not an item we can load ammo container in anyway
                {
                    Mod.LogError("Attempted to description ammo fill " + roundData.H3ID + " into " + owner.descriptionPack.item.name + " which is not magazine, clip, or speedloader");
                    return;
                }
            }
            else // Should not have had load button, but item could have been destroyed since description creation
            {
                return;
            }

            if(countLeft <= 0)
            {
                return;
            }

            // Loose rounds on player
            if(Mod.playerInventoryItems.TryGetValue(roundData.H3ID, out List<MeatovItem> roundList))
            {
                if(typeIndex == 0)
                {
                    // Note inverse order, because rounds will be removed from roundlist as they get loaded because they get destroyed as they do
                    for (int i = roundList.Count-1; i >= 0 && countLeft > 0; --i)
                    {
                        asMag.AddRound(roundList[i].physObj as FVRFireArmRound, false, true, false);
                        --entryCount;
                        --countLeft;
                    }
                }
                else if(typeIndex == 1)
                {
                    for (int i = roundList.Count - 1; i >= 0 && countLeft > 0; --i)
                    {
                        asClip.AddRound(roundList[i].physObj as FVRFireArmRound, false, true, false);
                        --entryCount;
                        --countLeft;
                    }
                }
                else if(typeIndex == 2)
                {
                    for (int i = roundList.Count - 1; i >= 0 && countLeft > 0; --i)
                    {
                        bool found = false;
                        for(int j=0; j < asSL.Chambers.Count; ++j)
                        {
                            if (!asSL.Chambers[j].IsLoaded)
                            {
                                asSL.Chambers[j].Load(roundList[i].roundClass);
                                roundList[i].Destroy();
                                --entryCount;
                                --countLeft;
                                found = true;
                                break;
                            }
                        }
                        if (!found) // SL full
                        {
                            break;
                        }
                    }
                }

                if (countLeft == 0)
                {
                    if(entryCount <= 0)
                    {
                        Destroy(gameObject);
                    }
                    else
                    {
                        amount.text = "(" + entryCount + ")";
                    }
                    return;
                }
            }

            // Loose rounds in hideout
            if(HideoutController.instance != null && HideoutController.instance.inventoryItems.TryGetValue(roundData.H3ID, out List<MeatovItem> hideoutRoundList))
            {
                if(typeIndex == 0)
                {
                    // Note inverse order, because rounds will be removed from roundlist as they get loaded because they get destroyed as they do
                    for (int i = hideoutRoundList.Count-1; i >= 0 && countLeft > 0; --i)
                    {
                        asMag.AddRound(hideoutRoundList[i].physObj as FVRFireArmRound, false, true, false);
                        --entryCount;
                        --countLeft;
                    }
                }
                else if(typeIndex == 1)
                {
                    for (int i = hideoutRoundList.Count - 1; i >= 0 && countLeft > 0; --i)
                    {
                        asClip.AddRound(hideoutRoundList[i].physObj as FVRFireArmRound, false, true, false);
                        --entryCount;
                        --countLeft;
                    }
                }
                else if(typeIndex == 2)
                {
                    for (int i = hideoutRoundList.Count - 1; i >= 0 && countLeft > 0; --i)
                    {
                        bool found = false;
                        for(int j=0; j < asSL.Chambers.Count; ++j)
                        {
                            if (!asSL.Chambers[j].IsLoaded)
                            {
                                asSL.Chambers[j].Load(hideoutRoundList[i].roundClass);
                                hideoutRoundList[i].Destroy();
                                --entryCount;
                                --countLeft;
                                found = true;
                                break;
                            }
                        }
                        if (!found) // SL full
                        {
                            break;
                        }
                    }
                }

                if (countLeft == 0)
                {
                    if (entryCount <= 0)
                    {
                        Destroy(gameObject);
                    }
                    else
                    {
                        amount.text = "(" + entryCount + ")";
                    }
                    return;
                }
            }

            // Player ammoboxes
            if (Mod.ammoBoxesByRoundClassByRoundType.TryGetValue(roundData.roundType, out Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>> playerRoundClasses)
                && playerRoundClasses.TryGetValue(roundData.roundClass, out Dictionary<MeatovItem, int> playerAmmoBoxes))
            {
                List<MeatovItem> toRemove = new List<MeatovItem>();
                foreach(KeyValuePair<MeatovItem, int> ammobox in playerAmmoBoxes)
                {
                    // Note that we can't remove a round from the box mag using mag.RemoveRound because we only want to remove rounds of a specific class
                    // Because of this, we need to do the ammo box ammo tracking here like we do in the RemoveRound patches
                    FVRFireArmMagazine boxMag = ammobox.Key.physObj as FVRFireArmMagazine;
                    if (typeIndex == 0)
                    {
                        for (int i = boxMag.LoadedRounds.Length - 1; i >= 0 && countLeft > 0; --i)
                        {
                            FVRLoadedRound lr = boxMag.LoadedRounds[i];
                            if (lr.LR_Class == roundData.roundClass)
                            {
                                asMag.AddRound(roundData.roundClass, false, true);
                                --entryCount;
                                --countLeft;

                                // Remove the correct round
                                boxMag.LoadedRounds[i] = boxMag.LoadedRounds[boxMag.m_numRounds - 1];
                                boxMag.LoadedRounds[boxMag.m_numRounds - 1] = null;
                                --boxMag.m_numRounds;

                                // Manage ammo tracking
                                // Note that since we are in a foreach loop on the innermost dict, we can't just remove the entry if now empty
                                // We instead store it to remove it once we are done
                                --Mod.ammoBoxesByRoundClassByRoundType[roundData.roundType][lr.LR_Class][ammobox.Key];
                                if (Mod.ammoBoxesByRoundClassByRoundType[roundData.roundType][lr.LR_Class][ammobox.Key] == 0)
                                {
                                    toRemove.Add(ammobox.Key);
                                }
                            }
                        }
                    }
                    else if (typeIndex == 1)
                    {
                        for (int i = boxMag.LoadedRounds.Length - 1; i >= 0 && countLeft > 0; --i)
                        {
                            FVRLoadedRound lr = boxMag.LoadedRounds[i];
                            if (lr.LR_Class == roundData.roundClass)
                            {
                                asClip.AddRound(roundData.roundClass, false, true);
                                --entryCount;
                                --countLeft;

                                boxMag.LoadedRounds[i] = boxMag.LoadedRounds[boxMag.m_numRounds - 1];
                                boxMag.LoadedRounds[boxMag.m_numRounds - 1] = null;
                                --boxMag.m_numRounds;

                                --Mod.ammoBoxesByRoundClassByRoundType[roundData.roundType][lr.LR_Class][ammobox.Key];
                                if (Mod.ammoBoxesByRoundClassByRoundType[roundData.roundType][lr.LR_Class][ammobox.Key] == 0)
                                {
                                    toRemove.Add(ammobox.Key);
                                }
                            }
                        }
                    }
                    else if (typeIndex == 2)
                    {
                        for (int i = boxMag.LoadedRounds.Length - 1; i >= 0 && countLeft > 0; --i)
                        {
                            FVRLoadedRound lr = boxMag.LoadedRounds[i];
                            if (lr.LR_Class == roundData.roundClass)
                            {
                                bool found = false;
                                for (int j = 0; j < asSL.Chambers.Count; ++j)
                                {
                                    if (!asSL.Chambers[j].IsLoaded)
                                    {
                                        asSL.Chambers[j].Load(roundData.roundClass);
                                        --entryCount;
                                        --countLeft;
                                        found = true;
                                        break;
                                    }
                                }
                                if (!found) // SL full
                                {
                                    break;
                                }

                                boxMag.LoadedRounds[i] = boxMag.LoadedRounds[boxMag.m_numRounds - 1];
                                boxMag.LoadedRounds[boxMag.m_numRounds - 1] = null;
                                --boxMag.m_numRounds;

                                --Mod.ammoBoxesByRoundClassByRoundType[roundData.roundType][lr.LR_Class][ammobox.Key];
                                if (Mod.ammoBoxesByRoundClassByRoundType[roundData.roundType][lr.LR_Class][ammobox.Key] == 0)
                                {
                                    toRemove.Add(ammobox.Key);
                                }
                            }
                        }
                    }

                    if(countLeft == 0)
                    {
                        for(int i=0; i < toRemove.Count; ++i)
                        {
                            Mod.ammoBoxesByRoundClassByRoundType[roundData.roundType][roundData.roundClass].Remove(toRemove[i]);
                            if (Mod.ammoBoxesByRoundClassByRoundType[roundData.roundType][roundData.roundClass].Count == 0)
                            {
                                Mod.ammoBoxesByRoundClassByRoundType[roundData.roundType].Remove(roundData.roundClass);
                                if (Mod.ammoBoxesByRoundClassByRoundType[roundData.roundType].Count == 0)
                                {
                                    Mod.ammoBoxesByRoundClassByRoundType.Remove(roundData.roundType);
                                }
                            }
                        }

                        if (entryCount <= 0)
                        {
                            Destroy(gameObject);
                        }
                        else
                        {
                            amount.text = "(" + entryCount + ")";
                        }

                        return;
                    }
                }

                for (int i = 0; i < toRemove.Count; ++i)
                {
                    Mod.ammoBoxesByRoundClassByRoundType[roundData.roundType][roundData.roundClass].Remove(toRemove[i]);
                    if (Mod.ammoBoxesByRoundClassByRoundType[roundData.roundType][roundData.roundClass].Count == 0)
                    {
                        Mod.ammoBoxesByRoundClassByRoundType[roundData.roundType].Remove(roundData.roundClass);
                        if (Mod.ammoBoxesByRoundClassByRoundType[roundData.roundType].Count == 0)
                        {
                            Mod.ammoBoxesByRoundClassByRoundType.Remove(roundData.roundType);
                        }
                    }
                }
            }

            // Hideout ammoboxes
            if (HideoutController.instance != null 
                && HideoutController.instance.ammoBoxesByRoundClassByRoundType.TryGetValue(roundData.roundType, out Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>> hideoutRoundClasses)
                && hideoutRoundClasses.TryGetValue(roundData.roundClass, out Dictionary<MeatovItem, int> hideoutAmmoBoxes))
            {
                List<MeatovItem> toRemove = new List<MeatovItem>();
                foreach(KeyValuePair<MeatovItem, int> ammobox in hideoutAmmoBoxes)
                {
                    FVRFireArmMagazine boxMag = ammobox.Key.physObj as FVRFireArmMagazine;
                    if (typeIndex == 0)
                    {
                        for (int i = boxMag.LoadedRounds.Length - 1; i >= 0 && countLeft > 0; --i)
                        {
                            FVRLoadedRound lr = boxMag.LoadedRounds[i];
                            if (lr.LR_Class == roundData.roundClass)
                            {
                                asMag.AddRound(roundData.roundClass, false, true);
                                --entryCount;
                                --countLeft;

                                boxMag.LoadedRounds[i] = boxMag.LoadedRounds[boxMag.m_numRounds - 1];
                                boxMag.LoadedRounds[boxMag.m_numRounds - 1] = null;
                                --boxMag.m_numRounds;

                                --HideoutController.instance.ammoBoxesByRoundClassByRoundType[roundData.roundType][lr.LR_Class][ammobox.Key];
                                if (HideoutController.instance.ammoBoxesByRoundClassByRoundType[roundData.roundType][lr.LR_Class][ammobox.Key] == 0)
                                {
                                    toRemove.Add(ammobox.Key);
                                }
                            }
                        }
                    }
                    else if (typeIndex == 1)
                    {
                        for (int i = boxMag.LoadedRounds.Length - 1; i >= 0 && countLeft > 0; --i)
                        {
                            FVRLoadedRound lr = boxMag.LoadedRounds[i];
                            if (lr.LR_Class == roundData.roundClass)
                            {
                                asClip.AddRound(roundData.roundClass, false, true);
                                --entryCount;
                                --countLeft;

                                boxMag.LoadedRounds[i] = boxMag.LoadedRounds[boxMag.m_numRounds - 1];
                                boxMag.LoadedRounds[boxMag.m_numRounds - 1] = null;
                                --boxMag.m_numRounds;

                                --HideoutController.instance.ammoBoxesByRoundClassByRoundType[roundData.roundType][lr.LR_Class][ammobox.Key];
                                if (HideoutController.instance.ammoBoxesByRoundClassByRoundType[roundData.roundType][lr.LR_Class][ammobox.Key] == 0)
                                {
                                    toRemove.Add(ammobox.Key);
                                }
                            }
                        }
                    }
                    else if (typeIndex == 2)
                    {
                        for (int i = boxMag.LoadedRounds.Length - 1; i >= 0 && countLeft > 0; --i)
                        {
                            FVRLoadedRound lr = boxMag.LoadedRounds[i];
                            if (lr.LR_Class == roundData.roundClass)
                            {
                                bool found = false;
                                for (int j = 0; j < asSL.Chambers.Count; ++j)
                                {
                                    if (!asSL.Chambers[j].IsLoaded)
                                    {
                                        asSL.Chambers[j].Load(roundData.roundClass);
                                        --entryCount;
                                        --countLeft;
                                        found = true;
                                        break;
                                    }
                                }
                                if (!found) // SL full
                                {
                                    break;
                                }

                                boxMag.LoadedRounds[i] = boxMag.LoadedRounds[boxMag.m_numRounds - 1];
                                boxMag.LoadedRounds[boxMag.m_numRounds - 1] = null;
                                --boxMag.m_numRounds;

                                --HideoutController.instance.ammoBoxesByRoundClassByRoundType[roundData.roundType][lr.LR_Class][ammobox.Key];
                                if (HideoutController.instance.ammoBoxesByRoundClassByRoundType[roundData.roundType][lr.LR_Class][ammobox.Key] == 0)
                                {
                                    toRemove.Add(ammobox.Key);
                                }
                            }
                        }
                    }

                    if(countLeft == 0)
                    {
                        for(int i=0; i < toRemove.Count; ++i)
                        {
                            HideoutController.instance.ammoBoxesByRoundClassByRoundType[roundData.roundType][roundData.roundClass].Remove(toRemove[i]);
                            if (HideoutController.instance.ammoBoxesByRoundClassByRoundType[roundData.roundType][roundData.roundClass].Count == 0)
                            {
                                HideoutController.instance.ammoBoxesByRoundClassByRoundType[roundData.roundType].Remove(roundData.roundClass);
                                if (HideoutController.instance.ammoBoxesByRoundClassByRoundType[roundData.roundType].Count == 0)
                                {
                                    HideoutController.instance.ammoBoxesByRoundClassByRoundType.Remove(roundData.roundType);
                                }
                            }
                        }

                        if (entryCount <= 0)
                        {
                            Destroy(gameObject);
                        }
                        else
                        {
                            amount.text = "(" + entryCount + ")";
                        }

                        return;
                    }
                }

                for (int i = 0; i < toRemove.Count; ++i)
                {
                    HideoutController.instance.ammoBoxesByRoundClassByRoundType[roundData.roundType][roundData.roundClass].Remove(toRemove[i]);
                    if (HideoutController.instance.ammoBoxesByRoundClassByRoundType[roundData.roundType][roundData.roundClass].Count == 0)
                    {
                        HideoutController.instance.ammoBoxesByRoundClassByRoundType[roundData.roundType].Remove(roundData.roundClass);
                        if (HideoutController.instance.ammoBoxesByRoundClassByRoundType[roundData.roundType].Count == 0)
                        {
                            HideoutController.instance.ammoBoxesByRoundClassByRoundType.Remove(roundData.roundType);
                        }
                    }
                }
            }
        }

        public void OnLoadAmmoContainerClicked(string containerID, bool mag)
        {
            if (owner.descriptionPack.item != null)
            {
                if(owner.descriptionPack.item.physObj is FVRFireArm)
                {
                    FVRFireArm asFA = owner.descriptionPack.item.physObj as FVRFireArm;
                    if(asFA.Magazine != null || asFA.Clip != null)
                    {
                        return;
                    }
                }
                else if(owner.descriptionPack.item.physObj is AttachableFirearmPhysicalObject)
                {
                    AttachableFirearmPhysicalObject asAFA = owner.descriptionPack.item.physObj as AttachableFirearmPhysicalObject;
                    if (asAFA.FA.Magazine != null || asAFA.FA.Clip != null)
                    {
                        return;
                    }
                }
                else // Not an item we can load ammo container in anyway
                {
                    Mod.LogError("Attempted to description ammo container load " + containerID + " into " + owner.descriptionPack.item.name + " which is not FVRFirearm nor AttachableFirearmPhysicalObject");
                    return;
                }
            }
            else // Should not have had load button, but item could have been destroyed since description creation
            {
                return;
            }

            bool loaded = false;
            if(Mod.playerInventoryItems.TryGetValue(containerID, out List<MeatovItem> containerList))
            {
                if (mag)
                {
                    for (int i = 0; i < containerList.Count; ++i)
                    {
                        FVRFireArmMagazine asMag = containerList[i].physObj as FVRFireArmMagazine;
                        if (asMag != null && asMag.FireArm == null && asMag.AttachableFireArm == null)
                        {
                            if (owner.descriptionPack.item.physObj is FVRFireArm)
                            {
                                FVRFireArm asFA = owner.descriptionPack.item.physObj as FVRFireArm;
                                asFA.LoadMag(asMag);
                                --entryCount;
                                loaded = true;
                                break;
                            }
                            else if (owner.descriptionPack.item.physObj is AttachableFirearmPhysicalObject)
                            {
                                AttachableFirearmPhysicalObject asAFA = owner.descriptionPack.item.physObj as AttachableFirearmPhysicalObject;
                                asAFA.FA.LoadMag(asMag);
                                --entryCount;
                                loaded = true;
                                break;
                            }
                            else
                            {
                                return;
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < containerList.Count; ++i)
                    {
                        FVRFireArmClip asClip = containerList[i].physObj as FVRFireArmClip;
                        if (asClip != null && asClip.FireArm == null)
                        {
                            if (owner.descriptionPack.item.physObj is FVRFireArm)
                            {
                                FVRFireArm asFA = owner.descriptionPack.item.physObj as FVRFireArm;
                                asFA.LoadClip(asClip);
                                --entryCount;
                                loaded = true;
                                break;
                            }
                            else // Can't load clip into attachable firearm
                            {
                                return;
                            }
                        }
                    }
                }
            }

            if(!loaded && HideoutController.instance != null && HideoutController.instance.inventoryItems.TryGetValue(containerID, out List<MeatovItem> hideoutContainerList))
            {
                if (mag)
                {
                    for (int i = 0; i < hideoutContainerList.Count; ++i)
                    {
                        FVRFireArmMagazine asMag = hideoutContainerList[i].physObj as FVRFireArmMagazine;
                        if (asMag != null && asMag.FireArm == null && asMag.AttachableFireArm == null)
                        {
                            if (owner.descriptionPack.item.physObj is FVRFireArm)
                            {
                                FVRFireArm asFA = owner.descriptionPack.item.physObj as FVRFireArm;
                                asFA.LoadMag(asMag);
                                --entryCount;
                                loaded = true;
                                break;
                            }
                            else if (owner.descriptionPack.item.physObj is AttachableFirearmPhysicalObject)
                            {
                                AttachableFirearmPhysicalObject asAFA = owner.descriptionPack.item.physObj as AttachableFirearmPhysicalObject;
                                asAFA.FA.LoadMag(asMag);
                                --entryCount;
                                loaded = true;
                                break;
                            }
                            else
                            {
                                return;
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < hideoutContainerList.Count; ++i)
                    {
                        FVRFireArmClip asClip = hideoutContainerList[i].physObj as FVRFireArmClip;
                        if (asClip != null && asClip.FireArm == null)
                        {
                            if (owner.descriptionPack.item.physObj is FVRFireArm)
                            {
                                FVRFireArm asFA = owner.descriptionPack.item.physObj as FVRFireArm;
                                asFA.LoadClip(asClip);
                                --entryCount;
                                loaded = true;
                                break;
                            }
                            else // Can't load clip into attachable firearm
                            {
                                return;
                            }
                        }
                    }
                }
            }

            if (entryCount <= 0)
            {
                Destroy(gameObject);
            }
            else
            {
                amount.text = "(" + entryCount + ")";
            }
        }
    }
}
