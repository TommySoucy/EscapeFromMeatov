using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class ItemDescriptionUI : MonoBehaviour
    {
        public GameObject summary;
        public Image summaryIcon;
        public Text summaryAmountStackText;
        public GameObject summaryInsuredIcon;
        public GameObject summaryInsuredBorder;
        public Image summaryCheckmark;
        public Text summaryName;
        public Text summaryNeededText;
        public GameObject summaryWishlist;
        public Text summaryWeight;
        public Text summaryVolume;

        public GameObject full;
        public Image fullIcon;
        public Text fullAmountStackText;
        public GameObject fullInsuredIcon;
        public GameObject fullInsuredBorder;
        public Image fullCheckmark;
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
    }
}
