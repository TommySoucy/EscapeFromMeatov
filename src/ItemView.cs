using System;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class ItemView : MonoBehaviour, IDescribable
    {
        [NonSerialized]
        public MeatovItem MIW;
        public Image itemIcon;
        public GameObject infoMod;
        public Sprite[] modTypes; // Auxilary, Barrel, Bipod, Charge, Light/Laser, GasBlock, Handguard, IronSight, Launcher, Mag, Rail, Muzzle, Grip, RailCover, Receiver Sight, Stock Tactical
        [NonSerialized]
        public Image modType;
        public GameObject infoSecure;
        public GameObject infoLocked;
        public GameObject infoSpecial;
        public Text infoShortNameText;
        public Image infoCheckmark;
        public GameObject infoInsuredIcon;
        public GameObject insuredBorder;
        public Text infoCountText;
        public GameObject infoValue;
        public Sprite[] currencyIcons;
        public Image infoValueIcon;
        public Text infoValueText;
        public GameObject toolIcon;
        public GameObject toolBorder;

        public void SetItem(MeatovItem itemData)
        {
            // TODO
            MIW = itemData;
        }

        public DescriptionPack GetDescriptionPack()
        {
            // TODO
            return new DescriptionPack();
        }

        public void SetDescriptionManager(DescriptionManager descriptionManager)
        {
            // TODO
        }
    }
}
