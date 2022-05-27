using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class EFM_MarketItemView : MonoBehaviour
    {
        public bool custom;
        public int value;
        public List<EFM_CustomItemWrapper> CIW;
        public List<EFM_VanillaItemDescriptor> VID;
    }
}
