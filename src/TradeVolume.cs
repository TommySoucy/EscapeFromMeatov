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
		public List<CustomItemWrapper.ResetColPair> resetColPairs;
		public MarketManager market;

		public void SetContainerHovered(bool hovered)
		{
			Material matToUse = hovered ? Mod.quickSlotHoverMaterial : Mod.quickSlotConstantMaterial;
			mainContainerRenderer.material = matToUse;
		}

		public void AddItem(FVRPhysicalObject item)
        {
			Mod.instance.LogInfo("Additem called");
			Collider[] cols = item.gameObject.GetComponentsInChildren<Collider>(true);
			Mod.instance.LogInfo("got cols");
			if (resetColPairs == null)
			{
				resetColPairs = new List<CustomItemWrapper.ResetColPair>();
			}
			Mod.instance.LogInfo("0");
			CustomItemWrapper.ResetColPair resetColPair = null;
			Mod.instance.LogInfo("0");
			foreach (Collider col in cols)
			{
				Mod.instance.LogInfo("1");
				if (col.gameObject.layer == 0 || col.gameObject.layer == 14)
				{
					Mod.instance.LogInfo("is on layer");
					col.isTrigger = true;

					Mod.instance.LogInfo("2");
					// Create new resetColPair for each collider so we can reset those specific ones to non-triggers when taken out of the backpack
					if (resetColPair == null)
					{
						Mod.instance.LogInfo("first, making new pair");
						resetColPair = new CustomItemWrapper.ResetColPair();
						resetColPair.physObj = item;
						resetColPair.colliders = new List<Collider>();
					}
					Mod.instance.LogInfo("2");
					resetColPair.colliders.Add(col);
					Mod.instance.LogInfo("2");
				}
			}
			if (resetColPair != null)
			{
				Mod.instance.LogInfo("adding to list");
				resetColPairs.Add(resetColPair);
			}
			Mod.instance.LogInfo("0");
			item.SetParentage(itemsRoot);
			Mod.instance.LogInfo("0");
			item.RootRigidbody.isKinematic = true;
			Mod.instance.LogInfo("0");
		}
	}
}
