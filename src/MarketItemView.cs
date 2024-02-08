using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class MarketItemView : MonoBehaviour
    {
        public bool custom;
        public int value;
        public int insureValue;
        public List<MeatovItem> CIW;
        public List<VanillaItemDescriptor> VID;
    }
}
