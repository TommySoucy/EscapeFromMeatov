using FistVR;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class ItemDescriptionUI : MonoBehaviour
    {
        public DescriptionPack descriptionPack;

        public GameObject summary;
        public ItemView summaryItemView;
        public Text summaryName;
        public Text summaryNeededText;
        public GameObject summaryWishlist;
        public Text summaryWeight;
        public Text summaryVolume;

        public GameObject full;
        public ItemView fullItemView;
        public Text fullName;
        public Text propertiesText;
        public GameObject containsParent;
        public Text contentText;
        public GameObject neededForTitle;
        public GameObject neededForNone;
        public GameObject neededForWishlist;
        public GameObject neededForAreas;
        public GameObject neededForAreasOpenIcon;
        public GameObject neededForAreasCloseIcon;
        public GameObject neededForAreasParent;
        public GameObject neededForAreasEntryPrefab;
        public Text neededForAreasTotal;
        public GameObject neededForQuests;
        public GameObject neededForQuestsOpenIcon;
        public GameObject neededForQuestsCloseIcon;
        public GameObject neededForQuestsParent;
        public GameObject neededForQuestsEntryPrefab;
        public Text neededForQuestsTotal;
        public GameObject neededForBarters;
        public GameObject neededForBartersOpenIcon;
        public GameObject neededForBartersCloseIcon;
        public GameObject neededForBartersParent;
        public GameObject neededForBartersEntryPrefab;
        public Text neededForBartersTotal;
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
            summaryWishlist.SetActive(descriptionPack.itemData.onWishlist);

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

            // Common
            OnPropertiesChanged();
            needed |= UpdateNeededForWishlist();

            neededForNone.SetActive(!needed);
        }

        public void OnToggleAreasClicked()
        {

        }

        public void OnToggleQuestsClicked()
        {

        }

        public void OnToggleBartersClicked()
        {

        }

        public void OnToggleAmmoContainersClicked()
        {

        }

        public void OnToggleAmmoClicked()
        {

        }

        public void OnOpenFullClicked()
        {
            clickAudio.Play();
            summary.SetActive(false);
            full.SetActive(true);
            transform.parent = null;
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
            TODO e: // sub to containment volume itemadded/removed/stackchanged events
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

            UpdateNeededForAreas();
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
                TODO e: // Must consider the tasks' FIR requirements
                neededForQuests.SetActive(true);
                int total = 0;
                long currentCount = Mod.GetItemCountInInventories(descriptionPack.itemData.H3ID);
                foreach (KeyValuePair<Task, int> entry in descriptionPack.itemData.neededForTasksCurrent)
                {
                    GameObject newEntry = Instantiate(neededForQuestsEntryPrefab, neededForQuestsParent.transform);
                    ItemDescriptionListEntryUI entryUI = newEntry.GetComponent<ItemDescriptionListEntryUI>();
                    entryUI.SetTask(this, entry.Key, currentCount, entry.Value);
                    total += entry.Value;
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
                        ItemDescriptionListEntryUI entryUI = newEntry.GetComponent<ItemDescriptionListEntryUI>();
                        entryUI.SetAreaLevel(this, areaEntry.Key, entry.Key, currentCount, entry.Value);
                        total += entry.Value;
                    }
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

        public bool UpdateNeededForWishlist()
        {
            if (descriptionPack.itemData.onWishlist)
            {
                summaryWishlist.SetActive(true);
                neededForWishlist.SetActive(true);
                return true;
            }
            else
            {
                summaryWishlist.SetActive(false);
                neededForWishlist.SetActive(false);
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
                            ItemDescriptionListEntryUI entryUI = newEntry.GetComponent<ItemDescriptionListEntryUI>();
                            entryUI.SetBarter(this, entry.Key, currentCount, entry.Value);
                            total += entry.Value;
                        }
                    }
                }
                neededForQuestsTotal.text = "Total: " + currentCount + "/" + total;
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
                }
            }
        }
    }
}
