using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public interface EFM_Describable
    {
        DescriptionPack GetDescriptionPack();
    }

    public class DescriptionPack
    {
        public bool isCustom;
        public EFM_CustomItemWrapper customItem;
        public EFM_VanillaItemDescriptor vanillaItem;
        public Dictionary<string, int> containedAmmoClasses;
        public string name;
        public string description;
        public Sprite icon;
        public int stack = -1;
        public int maxStack = -1;
        public float containingVolume = -1;
        public float maxVolume = -1;
        public int amount;
        public int amountRequired;
        public bool onWishlist;
        public int[] amountRequiredPerArea = new int[22];
        public int amountRequiredQuest;
        public bool insured;
        public float weight;
        public float volume;
        public Dictionary<string, int> compatibleAmmoContainers;
        public Dictionary<string, int> compatibleAmmo;
    }
}
