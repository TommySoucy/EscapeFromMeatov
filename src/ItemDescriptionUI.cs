using FistVR;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class ItemDescriptionUI : MonoBehaviour
    {
        public DescriptionPack descriptionPack;
        [NonSerialized]
        public Hand hand;

        public GameObject summary;
        public ItemView summaryItemView;
        public Text summaryName;
        public Text summaryNeededText;
        public Text summaryWishlist;
        public Text summaryWeight;
        public Text summaryVolume;

        public GameObject full;
        public HoverScrollProcessor fullHoverScrollProcessor;
        public ItemView fullItemView;
        public Text fullName;
        public Text propertiesText;
        public GameObject containsParent;
        public Text contentText;
        public GameObject neededForTitle;
        public GameObject neededForNone;
        public Text neededForWishlist;
        public GameObject neededForAreas;
        public Text neededForAreasTitle;
        public GameObject neededForAreasOpenIcon;
        public GameObject neededForAreasCloseIcon;
        public GameObject neededForAreasParent;
        public GameObject neededForAreasEntryPrefab;
        public Text neededForAreasTotal;
        public GameObject neededForQuests;
        public Text neededForQuestsTitle;
        public GameObject neededForQuestsOpenIcon;
        public GameObject neededForQuestsCloseIcon;
        public GameObject neededForQuestsParent;
        public GameObject neededForQuestsEntryPrefab;
        public Text neededForQuestsTotal;
        public GameObject neededForBarters;
        public Text neededForBartersTitle;
        public GameObject neededForBartersOpenIcon;
        public GameObject neededForBartersCloseIcon;
        public GameObject neededForBartersParent;
        public GameObject neededForBartersEntryPrefab;
        public Text neededForBartersTotal;
        public GameObject neededForProductions;
        public Text neededForProductionsTitle;
        public GameObject neededForProductionsOpenIcon;
        public GameObject neededForProductionsCloseIcon;
        public GameObject neededForProductionsParent;
        public GameObject neededForProductionsEntryPrefab;
        public Text neededForProductionsTotal;
        public GameObject compatibleAmmoContainers;
        public GameObject compatibleAmmoContainersOpenIcon;
        public GameObject compatibleAmmoContainersCloseIcon;
        public GameObject compatibleAmmoContainersParent;
        public GameObject compatibleAmmoContainersEntryPrefab;
        public GameObject compatibleAmmo;
        public GameObject compatibleAmmoOpenIcon;
        public GameObject compatibleAmmoCloseIcon;
        public GameObject compatibleAmmoParent;
        public GameObject compatibleAmmoEntryPrefab;
        public Text description;
        public Image wishlistStar;

        public AudioSource clickAudio;

        public void Start()
        {
            // Inactive by default
            gameObject.SetActive(false);
        }

        public void Update()
        {
            if (Vector3.Distance(transform.position, GM.CurrentPlayerRoot.position) > 10)
            {
                Destroy(gameObject);
            }
        }

        public void SetDescriptionPack(DescriptionPack pack)
        {
            if(descriptionPack != null)
            {
                descriptionPack.itemData.OnNeededForChanged -= OnNeededForChanged;
                descriptionPack.itemData.OnNeededForAreaTotalChanged -= OnNeededForAreaTotalChanged;
                descriptionPack.itemData.OnNeededForTaskTotalChanged -= OnNeededForTaskTotalChanged;
                if(descriptionPack.item != null)
                {
                    descriptionPack.item.OnCurrentWeightChanged -= OnPropertiesChanged;
                    descriptionPack.item.OnModeChanged -= OnPropertiesChanged;
                    if (descriptionPack.item.containerVolume != null)
                    {
                        descriptionPack.item.containerVolume.OnItemAdded -= OnItemAdded;
                        descriptionPack.item.containerVolume.OnItemRemoved -= OnItemRemoved;
                        descriptionPack.item.containerVolume.OnItemStackChanged -= OnItemStackChanged;
                    }
                }
            }

            descriptionPack = pack;

            descriptionPack.itemData.OnNeededForChanged += OnNeededForChanged;
            descriptionPack.itemData.OnNeededForAreaTotalChanged += OnNeededForAreaTotalChanged;
            descriptionPack.itemData.OnNeededForTaskTotalChanged += OnNeededForTaskTotalChanged;
            if (descriptionPack.item != null)
            {
                descriptionPack.item.OnCurrentWeightChanged += OnPropertiesChanged;
                descriptionPack.item.OnModeChanged += OnPropertiesChanged;
                if(descriptionPack.item.containerVolume != null)
                {
                    descriptionPack.item.containerVolume.OnItemAdded += OnItemAdded;
                    descriptionPack.item.containerVolume.OnItemRemoved += OnItemRemoved;
                    descriptionPack.item.containerVolume.OnItemStackChanged += OnItemStackChanged;
                }
            }

            // Summary
            if(descriptionPack.item != null)
            {
                summaryItemView.SetItem(descriptionPack.item);
            }
            else
            {
                summaryItemView.SetItemData(descriptionPack.itemData, descriptionPack.hasInsuredOverride, descriptionPack.insuredOverride,
                                            descriptionPack.hasCountOverride, descriptionPack.countOverride, descriptionPack.hasValueOverride,
                                            descriptionPack.currencyIconIndexOverride, descriptionPack.valueOverride, descriptionPack.hasToolOveride, descriptionPack.isToolOverride);
            }
            summaryName.text = descriptionPack.itemData.name;
            summaryNeededText.text = Mod.GetItemCountInInventories(descriptionPack.itemData.H3ID).ToString()+"/"+descriptionPack.itemData.GetCurrentNeededForTotal();
            summaryWishlist.gameObject.SetActive(descriptionPack.itemData.onWishlist);
            summaryWishlist.color = Mod.neededForColors[2];

            // Full
            if(descriptionPack.item != null)
            {
                fullItemView.SetItem(descriptionPack.item);
            }
            else
            {
                fullItemView.SetItemData(descriptionPack.itemData, descriptionPack.hasInsuredOverride, descriptionPack.insuredOverride,
                                         descriptionPack.hasCountOverride, descriptionPack.countOverride, descriptionPack.hasValueOverride,
                                         descriptionPack.currencyIconIndexOverride, descriptionPack.valueOverride, descriptionPack.hasToolOveride, descriptionPack.isToolOverride);
            }
            fullName.text = descriptionPack.itemData.name;
            containsParent.SetActive(descriptionPack.item != null && descriptionPack.item.containerVolume != null);
            if (containsParent.activeSelf)
            {
                OnContentChanged();
            }
            bool needed = false;
            needed |= UpdateNeededForTasks();
            needed |= UpdateNeededForAreas();
            needed |= UpdateNeededForBarter();
            needed |= UpdateNeededForProduction();
            neededForQuestsTitle.color = Mod.neededForColors[0];
            neededForQuestsTotal.color = Mod.neededForColors[0];
            neededForWishlist.color = Mod.neededForColors[2];
            neededForBartersTitle.color = Mod.neededForColors[3];
            neededForBartersTotal.color = Mod.neededForColors[3];
            neededForProductionsTitle.color = Mod.neededForColors[4];
            neededForProductionsTotal.color = Mod.neededForColors[4];
            UpdateCompatibilityLists();

            // Common
            OnPropertiesChanged();
            needed |= UpdateNeededForWishlist();

            neededForNone.SetActive(!needed);
        }

        public void OnToggleAreasClicked()
        {
            // open is the new state
            bool open = !neededForAreasParent.activeSelf;
            neededForAreasParent.SetActive(open);
            neededForAreasOpenIcon.SetActive(!open);
            neededForAreasCloseIcon.SetActive(open);
            fullHoverScrollProcessor.mustUpdateMiddleHeight = 1;
            clickAudio.Play();
        }

        public void OnToggleQuestsClicked()
        {
            // open is the new state
            bool open = !neededForQuestsParent.activeSelf;
            neededForQuestsParent.SetActive(open);
            neededForQuestsOpenIcon.SetActive(!open);
            neededForQuestsCloseIcon.SetActive(open);
            fullHoverScrollProcessor.mustUpdateMiddleHeight = 1;
            clickAudio.Play();
        }

        public void OnToggleBartersClicked()
        {
            // open is the new state
            bool open = !neededForBartersParent.activeSelf;
            neededForBartersParent.SetActive(open);
            neededForBartersOpenIcon.SetActive(!open);
            neededForBartersCloseIcon.SetActive(open);
            fullHoverScrollProcessor.mustUpdateMiddleHeight = 1;
            clickAudio.Play();
        }

        public void OnToggleProductionsClicked()
        {
            // open is the new state
            bool open = !neededForProductionsParent.activeSelf;
            neededForProductionsParent.SetActive(open);
            neededForProductionsOpenIcon.SetActive(!open);
            neededForProductionsCloseIcon.SetActive(open);
            fullHoverScrollProcessor.mustUpdateMiddleHeight = 1;
            clickAudio.Play();
        }

        public void OnToggleAmmoContainersClicked()
        {
            // open is the new state
            bool open = !compatibleAmmoContainersParent.activeSelf;
            compatibleAmmoContainersParent.SetActive(open);
            compatibleAmmoContainersOpenIcon.SetActive(!open);
            compatibleAmmoContainersCloseIcon.SetActive(open);
            fullHoverScrollProcessor.mustUpdateMiddleHeight = 1;
            clickAudio.Play();
        }

        public void OnToggleAmmoClicked()
        {
            // open is the new state
            bool open = !compatibleAmmoParent.activeSelf;
            compatibleAmmoParent.SetActive(open);
            compatibleAmmoOpenIcon.SetActive(!open);
            compatibleAmmoCloseIcon.SetActive(open);
            fullHoverScrollProcessor.mustUpdateMiddleHeight = 1;
            clickAudio.Play();
        }

        public void OnOpenFullClicked()
        {
            clickAudio.Play();
            summary.SetActive(false);
            full.SetActive(true);
            transform.parent = null;
            hand.description = null;
            hand.currentDescribable = null;
            hand = null;
        }

        public void OnWishlistClicked()
        {
            if(descriptionPack == null || descriptionPack.itemData == null)
            {
                Destroy(gameObject);
                return;
            }

            clickAudio.Play();

            // onWishlist is a property and will call an event that relevant UI should be subscribed to 
            // to update themselves accordingly
            descriptionPack.itemData.onWishlist = !descriptionPack.itemData.onWishlist;
        }

        public void OnExitClicked()
        {
            clickAudio.Play();
            Destroy(gameObject);
        }

        public void OnContentChanged()
        {
            string contentString = "";
            bool firstIt = true;
            foreach(KeyValuePair<string, int> contentEntry in descriptionPack.item.containerVolume.inventory)
            {
                if (firstIt)
                {
                    firstIt = false;
                }
                else
                {
                    contentString += "\n";
                }
                if(Mod.GetItemData(contentEntry.Key, out MeatovItemData itemData))
                {
                    contentString += ("- " + itemData.name + " (" + contentEntry.Value + ")");
                }
                else
                {
                    contentString += "NO DATA "+ contentEntry.Key;
                }
            }
            contentText.text = contentString;
        }

        public void OnItemAdded(MeatovItem item)
        {
            OnContentChanged();
        }

        public void OnItemRemoved(MeatovItem item)
        {
            OnContentChanged();
        }

        public void OnItemStackChanged(MeatovItem item, int difference)
        {
            OnContentChanged();
        }

        public void OnNeededForChanged(int index)
        {
            switch (index)
            {
                case 0:
                    UpdateNeededForTasks();
                    break;
                case 1:
                    UpdateNeededForAreas();
                    break;
                case 2:
                    UpdateNeededForWishlist();
                    break;
                case 3:
                    UpdateNeededForBarter();
                    break;
                case 4:
                    UpdateNeededForProduction();
                    break;
                default:
                    Mod.LogError("OnNeededForChanged called with wrong index: " + index);
                    break;
            }
        }

        public bool UpdateNeededForTasks()
        {
            // Clear current list
            while (neededForQuestsParent.transform.childCount > 1)
            {
                Transform currentFirstChild = neededForQuestsParent.transform.GetChild(1);
                currentFirstChild.SetParent(null);
                Destroy(currentFirstChild.gameObject);
            }

            // Fill new list if necessary
            if (descriptionPack.itemData.neededForTasksCurrent.Count > 0)
            {
                neededForQuests.SetActive(true);
                int total = 0;
                long currentCount = Mod.GetItemCountInInventories(descriptionPack.itemData.H3ID);
                long currentFIRCount = Mod.GetFIRItemCountInInventories(descriptionPack.itemData.H3ID);
                foreach (KeyValuePair<Task, KeyValuePair<int, bool>> entry in descriptionPack.itemData.neededForTasksCurrent) 
                {
                    GameObject newEntry = Instantiate(neededForQuestsEntryPrefab, neededForQuestsParent.transform);
                    newEntry.SetActive(true);
                    ItemDescriptionListEntryUI entryUI = newEntry.GetComponent<ItemDescriptionListEntryUI>();
                    entryUI.SetTask(this, entry.Key, entry.Value.Value ? currentFIRCount : currentCount, entry.Value.Key);
                    total += entry.Value.Key;
                }
                neededForQuestsTotal.text = "Total: " + currentCount + "/" + total;
                return true;
            }
            else
            {
                neededForQuests.SetActive(false);
                return false;
            }
        }

        public bool UpdateNeededForAreas()
        {
            // Clear current list
            while (neededForAreasParent.transform.childCount > 1)
            {
                Transform currentFirstChild = neededForAreasParent.transform.GetChild(1);
                currentFirstChild.SetParent(null);
                Destroy(currentFirstChild.gameObject);
            }

            // Fill new list if necessary
            if (descriptionPack.itemData.neededForLevelByAreaCurrent.Count > 0)
            {
                neededForAreas.SetActive(true);
                int total = 0;
                long currentCount = Mod.GetItemCountInInventories(descriptionPack.itemData.H3ID);
                foreach (KeyValuePair<int, Dictionary<int,int>> areaEntry in descriptionPack.itemData.neededForLevelByAreaCurrent)
                {
                    foreach(KeyValuePair<int, int> entry in areaEntry.Value)
                    {
                        GameObject newEntry = Instantiate(neededForAreasEntryPrefab, neededForAreasParent.transform);
                        newEntry.SetActive(true);
                        ItemDescriptionListEntryUI entryUI = newEntry.GetComponent<ItemDescriptionListEntryUI>();
                        entryUI.SetAreaLevel(this, areaEntry.Key, entry.Key, currentCount, entry.Value);
                        total += entry.Value;
                    }
                }
                neededForAreasTotal.text = "Total: " + currentCount + "/" + total;
                Color areasColor = Mod.GetItemCountInInventories(descriptionPack.itemData.H3ID) >= descriptionPack.itemData.minimumUpgradeAmount ? Mod.neededForAreaFulfilledColor : Mod.neededForColors[1];
                neededForAreasTitle.color = areasColor;
                neededForAreasTotal.color = areasColor;
                return true;
            }
            else
            {
                neededForAreas.SetActive(false);
                return false;
            }
        }

        public bool UpdateNeededForWishlist()
        {
            if (descriptionPack.itemData.onWishlist)
            {
                summaryWishlist.gameObject.SetActive(true);
                neededForWishlist.gameObject.SetActive(true);
                wishlistStar.color = Color.yellow;
                return true;
            }
            else
            {
                summaryWishlist.gameObject.SetActive(false);
                neededForWishlist.gameObject.SetActive(false);
                wishlistStar.color = Color.black;
                return false;
            }
        }

        public bool UpdateNeededForBarter()
        {
            // Clear current list
            while (neededForBartersParent.transform.childCount > 1)
            {
                Transform currentFirstChild = neededForBartersParent.transform.GetChild(1);
                currentFirstChild.SetParent(null);
                Destroy(currentFirstChild.gameObject);
            }

            // Fill new list if necessary
            if (descriptionPack.itemData.neededForBarterByLevelByTraderCurrent.Count > 0)
            {
                neededForBarters.SetActive(true);
                int total = 0;
                long currentCount = Mod.GetItemCountInInventories(descriptionPack.itemData.H3ID);
                foreach (KeyValuePair<int, Dictionary<int, Dictionary<Barter, int>>> traderEntry in descriptionPack.itemData.neededForBarterByLevelByTraderCurrent)
                {
                    foreach (KeyValuePair<int, Dictionary<Barter, int>> traderLevelEntry in traderEntry.Value)
                    {
                        foreach (KeyValuePair<Barter, int> entry in traderLevelEntry.Value)
                        {
                            GameObject newEntry = Instantiate(neededForBartersEntryPrefab, neededForBartersParent.transform);
                            newEntry.SetActive(true);
                            ItemDescriptionListEntryUI entryUI = newEntry.GetComponent<ItemDescriptionListEntryUI>();
                            entryUI.SetBarter(this, entry.Key, traderEntry.Key, traderLevelEntry.Key, currentCount, entry.Value);
                            total += entry.Value;
                        }
                    }
                }
                neededForBartersTotal.text = "Total: " + currentCount + "/" + total;
                return true;
            }
            else
            {
                neededForBarters.SetActive(false);
                return false;
            }
        }

        public bool UpdateNeededForProduction()
        {
            // Clear current list
            while (neededForProductionsParent.transform.childCount > 1)
            {
                Transform currentFirstChild = neededForProductionsParent.transform.GetChild(1);
                currentFirstChild.SetParent(null);
                Destroy(currentFirstChild.gameObject);
            }

            // Fill new list if necessary
            if (descriptionPack.itemData.neededForProductionByLevelByAreaCurrent.Count > 0)
            {
                neededForBarters.SetActive(true);
                int total = 0;
                long currentCount = Mod.GetItemCountInInventories(descriptionPack.itemData.H3ID);
                foreach (KeyValuePair<int, Dictionary<int, Dictionary<Production, int>>> areaEntry in descriptionPack.itemData.neededForProductionByLevelByAreaCurrent)
                {
                    foreach (KeyValuePair<int, Dictionary<Production, int>> areaLevelEntry in areaEntry.Value)
                    {
                        foreach (KeyValuePair<Production, int> entry in areaLevelEntry.Value)
                        {
                            GameObject newEntry = Instantiate(neededForProductionsEntryPrefab, neededForProductionsParent.transform);
                            newEntry.SetActive(true);
                            ItemDescriptionListEntryUI entryUI = newEntry.GetComponent<ItemDescriptionListEntryUI>();
                            entryUI.SetProduction(this, entry.Key, areaEntry.Key, areaLevelEntry.Key, currentCount, entry.Value);
                            total += entry.Value;
                        }
                    }
                }
                neededForProductionsTotal.text = "Total: " + currentCount + "/" + total;
                return true;
            }
            else
            {
                neededForBarters.SetActive(false);
                return false;
            }
        }

        public void UpdateCompatibilityLists()
        {
            switch (descriptionPack.itemData.compatibilityValue)
            {
                case 0:
                    compatibleAmmoContainers.SetActive(false);
                    compatibleAmmo.SetActive(false);
                    break;
                case 1:
                    compatibleAmmo.SetActive(false);
                    compatibleAmmoContainers.SetActive(true);
                    UpdateCompatibleAmmoContainersList();
                    break;
                case 2:
                    compatibleAmmoContainers.SetActive(false);
                    compatibleAmmo.SetActive(true);
                    UpdateCompatibleAmmoList();
                    break;
                case 3:
                    compatibleAmmoContainers.SetActive(true);
                    compatibleAmmo.SetActive(true);
                    UpdateCompatibleAmmoContainersList();
                    UpdateCompatibleAmmoList();
                    break;
                default:
                    Mod.LogError("Item data for "+descriptionPack.itemData.name + " has invalid compatibility value: " + descriptionPack.itemData.compatibilityValue);
                    compatibleAmmoContainers.SetActive(false);
                    compatibleAmmo.SetActive(false);
                    break;
            }
        }

        public void UpdateCompatibleAmmoContainersList()
        {
            // Clear current list
            while (compatibleAmmoContainersParent.transform.childCount > 1)
            {
                Transform currentFirstChild = compatibleAmmoContainersParent.transform.GetChild(1);
                currentFirstChild.SetParent(null);
                Destroy(currentFirstChild.gameObject);
            }

            // Fill new list if necessary
            if (IM.OD.TryGetValue(descriptionPack.itemData.H3ID, out FVRObject wrapper))
            {
                bool gotContainer = false;
                if(wrapper.CompatibleMagazines != null)
                {
                    for (int i = 0; i < wrapper.CompatibleMagazines.Count; ++i)
                    {
                        if(Mod.GetItemData(wrapper.CompatibleMagazines[i].ItemID, out MeatovItemData itemData))
                        {
                            // Only consider mags not already loaded into a firearm
                            // Note that we only consider mags in hideout if we are currently in hideout
                            int hideoutCount = 0;
                            if (HideoutController.instance != null && HideoutController.instance.inventoryItems.TryGetValue(wrapper.CompatibleMagazines[i].ItemID, out List<MeatovItem> mags))
                            {
                                hideoutCount = mags.Count;
                                for(int j=0; j < mags.Count; ++j)
                                {
                                    FVRFireArmMagazine asMag = mags[j].physObj as FVRFireArmMagazine;
                                    if (asMag == null || asMag.FireArm != null || asMag.AttachableFireArm != null)
                                    {
                                        --hideoutCount;
                                    }
                                }
                            }
                            int playerCount = 0;
                            if (Mod.playerInventoryItems.TryGetValue(wrapper.CompatibleMagazines[i].ItemID, out List<MeatovItem> playerMags))
                            {
                                playerCount = playerMags.Count;
                                for (int j = 0; j < playerMags.Count; ++j)
                                {
                                    FVRFireArmMagazine asMag = playerMags[j].physObj as FVRFireArmMagazine;
                                    if (asMag == null || asMag.FireArm != null || asMag.AttachableFireArm != null)
                                    {
                                        --playerCount;
                                    }
                                }
                            }
                            if(hideoutCount > 0 || playerCount > 0)
                            {
                                GameObject newEntry = Instantiate(compatibleAmmoContainersEntryPrefab, compatibleAmmoContainersParent.transform);
                                newEntry.SetActive(true);
                                ItemDescriptionListEntryUI entryUI = newEntry.GetComponent<ItemDescriptionListEntryUI>();
                                entryUI.SetAmmoContainer(this, itemData, hideoutCount, playerCount, true);
                                gotContainer = true;
                            }
                        }
                    }
                }
                if(wrapper.CompatibleClips != null)
                {
                    for (int i = 0; i < wrapper.CompatibleClips.Count; ++i)
                    {
                        if (Mod.GetItemData(wrapper.CompatibleClips[i].ItemID, out MeatovItemData itemData))
                        {
                            // Only consider clips not already loaded into a firearm
                            // Note that we only consider clips in hideout if we are currently in hideout
                            int hideoutCount = 0;
                            if (HideoutController.instance != null && HideoutController.instance.inventoryItems.TryGetValue(wrapper.CompatibleClips[i].ItemID, out List<MeatovItem> clips))
                            {
                                hideoutCount = clips.Count;
                                for (int j = 0; j < clips.Count; ++j)
                                {
                                    FVRFireArmClip asClip = clips[j].physObj as FVRFireArmClip;
                                    if (asClip == null || asClip.FireArm != null)
                                    {
                                        --hideoutCount;
                                    }
                                }
                            }
                            int playerCount = 0;
                            if (Mod.playerInventoryItems.TryGetValue(wrapper.CompatibleClips[i].ItemID, out List<MeatovItem> playerClips))
                            {
                                playerCount = playerClips.Count;
                                for (int j = 0; j < playerClips.Count; ++j)
                                {
                                    FVRFireArmClip asClip = playerClips[j].physObj as FVRFireArmClip;
                                    if (asClip == null || asClip.FireArm != null)
                                    {
                                        --playerCount;
                                    }
                                }
                            }
                            if (hideoutCount > 0 || playerCount > 0)
                            {
                                GameObject newEntry = Instantiate(compatibleAmmoContainersEntryPrefab, compatibleAmmoContainersParent.transform);
                                newEntry.SetActive(true);
                                ItemDescriptionListEntryUI entryUI = newEntry.GetComponent<ItemDescriptionListEntryUI>();
                                entryUI.SetAmmoContainer(this, itemData, hideoutCount, playerCount, false);
                                gotContainer = true;
                            }
                        }
                    }
                }
                compatibleAmmoContainers.SetActive(gotContainer);
            }
            else
            {
                compatibleAmmoContainers.SetActive(false);
            }
        }

        public void UpdateCompatibleAmmoList()
        {
            // Clear current list
            while (compatibleAmmoParent.transform.childCount > 1)
            {
                Transform currentFirstChild = compatibleAmmoParent.transform.GetChild(1);
                currentFirstChild.SetParent(null);
                Destroy(currentFirstChild.gameObject);
            }

            // Fill new list if necessary
            if (IM.OD.TryGetValue(descriptionPack.itemData.H3ID, out FVRObject wrapper) && wrapper.CompatibleSingleRounds != null)
            {
                bool gotAmmo = false;
                for (int i = 0; i < wrapper.CompatibleSingleRounds.Count; ++i)
                {
                    if (Mod.GetItemData(wrapper.CompatibleSingleRounds[i].ItemID, out MeatovItemData itemData))
                    {
                        int hideoutCount = 0;
                        if (HideoutController.instance != null && HideoutController.instance.inventoryItems.TryGetValue(wrapper.CompatibleSingleRounds[i].ItemID, out List<MeatovItem> rounds))
                        {
                            hideoutCount = rounds.Count;
                            for (int j = 0; j < rounds.Count; ++j)
                            {
                                FVRFireArmRound asRound = rounds[j].physObj as FVRFireArmRound;
                                if (asRound == null)
                                {
                                    --hideoutCount;
                                }
                            }
                        }
                        int playerCount = 0;
                        if (Mod.playerInventoryItems.TryGetValue(wrapper.CompatibleSingleRounds[i].ItemID, out List<MeatovItem> playerRounds))
                        {
                            playerCount = playerRounds.Count;
                            for (int j = 0; j < playerRounds.Count; ++j)
                            {
                                FVRFireArmRound asRound = playerRounds[j].physObj as FVRFireArmRound;
                                if (asRound == null)
                                {
                                    --playerCount;
                                }
                            }
                        }
                        int ammoBoxCount = 0;
                        if (Mod.ammoBoxesByRoundClassByRoundType.TryGetValue(wrapper.CompatibleSingleRounds[i].RoundType, out Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>> roundClasses))
                        {
                            if(roundClasses.TryGetValue(itemData.roundClass, out Dictionary<MeatovItem, int> ammoBoxes))
                            {
                                foreach(KeyValuePair<MeatovItem, int> ammoBoxEntry in ammoBoxes)
                                {
                                    ammoBoxCount += ammoBoxEntry.Value;
                                }
                            }
                        }
                        GameObject newEntry = Instantiate(compatibleAmmoEntryPrefab, compatibleAmmoParent.transform);
                        newEntry.SetActive(true);
                        ItemDescriptionListEntryUI entryUI = newEntry.GetComponent<ItemDescriptionListEntryUI>();
                        entryUI.SetAmmo(this, itemData, hideoutCount, playerCount, ammoBoxCount);
                        gotAmmo = true;
                    }
                }
                compatibleAmmo.SetActive(gotAmmo);
            }
            else
            {
                compatibleAmmo.SetActive(false);
            }
        }

        public void OnNeededForAreaTotalChanged()
        {
            summaryNeededText.text = Mod.GetItemCountInInventories(descriptionPack.itemData.H3ID).ToString() + "/" + descriptionPack.itemData.GetCurrentNeededForTotal();
        }

        public void OnNeededForTaskTotalChanged()
        {
            summaryNeededText.text = Mod.GetItemCountInInventories(descriptionPack.itemData.H3ID).ToString() + "/" + descriptionPack.itemData.GetCurrentNeededForTotal();
        }

        public void OnPropertiesChanged()
        {
            if(descriptionPack.item == null)
            {
                summaryWeight.text = "Weight: " + (descriptionPack.itemData.weight / 1000f).ToString("0.0") + "kg";
                summaryVolume.text = "Volume: " + (descriptionPack.itemData.volumes[0] / 1000f).ToString("0.0") + "L";
                propertiesText.text = "Weight: " + descriptionPack.itemData.weight.ToString("0.0") + "kg, Volume: " + (descriptionPack.itemData.volumes[0] / 1000f).ToString("0.0") + "L";
            }
            else
            {
                summaryWeight.text = "Weight: " + (descriptionPack.item.currentWeight / 1000f).ToString("0.0") + "kg";
                summaryVolume.text = "Volume: " + (descriptionPack.item.volumes[descriptionPack.item.mode] / 1000f).ToString("0.0") + "L";
                propertiesText.text = "Weight: " + descriptionPack.item.currentWeight.ToString("0.0") + "kg, Volume: " + (descriptionPack.item.volumes[descriptionPack.item.mode] / 1000f).ToString("0.0") + "L";
            }
        }

        public void OnDestroy()
        {
            if (descriptionPack != null)
            {
                descriptionPack.itemData.OnNeededForChanged -= OnNeededForChanged;
                descriptionPack.itemData.OnNeededForAreaTotalChanged -= OnNeededForAreaTotalChanged;
                descriptionPack.itemData.OnNeededForTaskTotalChanged -= OnNeededForTaskTotalChanged;
                if (descriptionPack.item != null)
                {
                    descriptionPack.item.OnCurrentWeightChanged -= OnPropertiesChanged;
                    descriptionPack.item.OnModeChanged -= OnPropertiesChanged;
                    if (descriptionPack.item.containerVolume != null)
                    {
                        descriptionPack.item.containerVolume.OnItemAdded -= OnItemAdded;
                        descriptionPack.item.containerVolume.OnItemRemoved -= OnItemRemoved;
                        descriptionPack.item.containerVolume.OnItemStackChanged -= OnItemStackChanged;
                    }
                }
            }
        }
    }
}
