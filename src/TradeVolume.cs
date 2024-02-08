using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class TradeVolume : MonoBehaviour
    {
		public Renderer mainContainerRenderer;
		public Transform itemsRoot;
		public List<MeatovItem.ResetColPair> resetColPairs;
		public MarketManager market;

		public void SetContainerHovered(bool hovered)
		{
			Material matToUse = hovered ? Mod.quickSlotHoverMaterial : Mod.quickSlotConstantMaterial;
			mainContainerRenderer.material = matToUse;
		}

		public void AddItem(FVRPhysicalObject item)
        {
			Mod.LogInfo("Additem called");
			Collider[] cols = item.gameObject.GetComponentsInChildren<Collider>(true);
			Mod.LogInfo("got cols");
			if (resetColPairs == null)
			{
				resetColPairs = new List<MeatovItem.ResetColPair>();
			}
			Mod.LogInfo("0");
			MeatovItem.ResetColPair resetColPair = null;
			Mod.LogInfo("0");
			foreach (Collider col in cols)
			{
				Mod.LogInfo("1");
				if (col.gameObject.layer == 0 || col.gameObject.layer == 14)
				{
					Mod.LogInfo("is on layer");
					col.isTrigger = true;

					Mod.LogInfo("2");
					// Create new resetColPair for each collider so we can reset those specific ones to non-triggers when taken out of the backpack
					if (resetColPair == null)
					{
						Mod.LogInfo("first, making new pair");
						resetColPair = new MeatovItem.ResetColPair();
						resetColPair.physObj = item;
						resetColPair.colliders = new List<Collider>();
					}
					Mod.LogInfo("2");
					resetColPair.colliders.Add(col);
					Mod.LogInfo("2");
				}
			}
			if (resetColPair != null)
			{
				Mod.LogInfo("adding to list");
				resetColPairs.Add(resetColPair);
			}
			Mod.LogInfo("0");
			item.SetParentage(itemsRoot);
			Mod.LogInfo("0");
			item.RootRigidbody.isKinematic = true;
			Mod.LogInfo("0");
		}
	}
}
