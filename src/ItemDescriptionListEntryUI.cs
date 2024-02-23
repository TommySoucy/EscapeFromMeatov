using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public void OnFillClicked()
        {

        }
    }
}
