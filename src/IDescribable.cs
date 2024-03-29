﻿using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public interface IDescribable
    {
        DescriptionPack GetDescriptionPack();

        void SetDescriptionManager(DescriptionManager descriptionManager);
    }

    public class DescriptionPack
    {
        public bool isPhysical;
        public IDescribable nonPhysDescribable;
        public string ID;
        public MeatovItem.ItemType itemType;
        // Dogtag
        public int level;

        public bool isCustom;
        public MeatovItem MI;
        public Dictionary<FireArmRoundType, Dictionary<FireArmRoundClass, int>> containedAmmoClassesByType;
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
        public int weight;
        public int volume;
        public Dictionary<string, int> compatibleAmmoContainers;
        public Dictionary<string, int> compatibleAmmo;
        public bool foundInRaid;
    }
}
