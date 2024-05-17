using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class DescriptionManager : MonoBehaviour
    {
        public DescriptionPack descriptionPack;
        public bool isFull;
        private bool destroying;

        public Image summaryIcon;
        public Text summaryAmountStackText;
        public Text summaryNameText;
        public Text summaryNeededForTotalText;
        public GameObject summaryWishlist;
        public GameObject summaryInsuredIcon;
        public GameObject summaryInsuredBorder;
        public GameObject[] summaryNeededIcons = new GameObject[5]; // Quest, AreaFulfilled, AreaRequired, Wish, FoundInRaid
        public Text summaryWeightText;
        public Text summaryVolumeText;

        public Image fullIcon;
        public Text fullAmountStackText;
        public Text fullNameText;
        public GameObject fullNeededForNone;
        public GameObject fullWishlist;
        public GameObject fullNeededForTotal;
        public Text fullNeededForTotalText;
        public GameObject fullInsuredIcon;
        public GameObject fullInsuredBorder;
        public GameObject[] fullNeededIcons = new GameObject[5]; // Quest, AreaFulfilled, AreaRequired, Wish, FoundInRaid
        public Text fullDescriptionText;
        public GameObject compatibleMagsTitle;
        public GameObject compatibleMags;
        public Text compatibleMagsText;
        public GameObject compatibleAmmoTitle;
        public GameObject compatibleAmmo;
        public Text compatibleAmmoText;
        public Text propertiesText;
        public HoverScroll downHoverScroll;
        public HoverScroll upHoverScroll;
        public GameObject ammoContainsTitle;
        public Image wishlistButtonImage;

        private AudioSource buttonClickAudio;
        private List<GameObject> areaNeededForTexts;
        private List<GameObject> ammoContainsTexts;

        public void Init()
        {
            // Set hover scrolls
            downHoverScroll = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(2).gameObject.AddComponent<HoverScroll>();
            upHoverScroll = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(1).gameObject.AddComponent<HoverScroll>();
            downHoverScroll.MaxPointingRange = 30;
            downHoverScroll.hoverSound = exitButton.hoverSound;
            downHoverScroll.scrollbar = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(1).GetComponent<Scrollbar>();
            downHoverScroll.other = upHoverScroll;
            downHoverScroll.rate = 0.5f;
            upHoverScroll.MaxPointingRange = 30;
            upHoverScroll.hoverSound = exitButton.hoverSound;
            upHoverScroll.scrollbar = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(1).GetComponent<Scrollbar>();
            upHoverScroll.other = downHoverScroll;
            upHoverScroll.up = true;
            upHoverScroll.rate = 0.5f;

            // Inactive by default
        }

        private void Update()
        {
            if(!destroying && Vector3.Distance(transform.position, GM.CurrentPlayerRoot.position) > 10)
            {
                Destroy(gameObject);
                destroying = true;
            }
        }
    }
}
