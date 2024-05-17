using FistVR;
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
            summaryWeight.text = "Weight: "+(descriptionPack.item == null ? descriptionPack.itemData.weight / 1000f : descriptionPack.item.currentWeight / 1000f).ToString("0.0")+"kg";
            summaryVolume.text = "Volume: "+(descriptionPack.item == null ? descriptionPack.itemData.volumes[0] / 1000f : descriptionPack.item.volumes[descriptionPack.item.mode] / 1000f).ToString("0.0")+"L";

            // Full
            cont from here
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
            summaryWeight.text = "Weight: "+(descriptionPack.item == null ? descriptionPack.itemData.weight / 1000f : descriptionPack.item.currentWeight / 1000f).ToString("0.0")+"kg";
            summaryVolume.text = "Volume: "+(descriptionPack.item == null ? descriptionPack.itemData.volumes[0] / 1000f : descriptionPack.item.volumes[descriptionPack.item.mode] / 1000f).ToString("0.0")+"L";
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

        public void OnNeededForChanged(int index)
        {
            if(index == 2)
            {
                summaryWishlist.SetActive(descriptionPack.itemData.onWishlist);
                neededForWishlist.SetActive(descriptionPack.itemData.onWishlist);
            }

            UpdateNeededForLists();
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
            summaryWeight.text = "Weight: " + (descriptionPack.item.currentWeight / 1000f).ToString("0.0") + "kg";
            summaryVolume.text = "Volume: " + (descriptionPack.item.volumes[descriptionPack.item.mode] / 1000f).ToString("0.0") + "L";
            propertiesText.text = "Weight: " + descriptionPack.item.weight.ToString("0.0")+"kg, Volume: "+(descriptionPack.item.volumes[descriptionPack.item.mode]/1000f).ToString("0.0")+"L";
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
