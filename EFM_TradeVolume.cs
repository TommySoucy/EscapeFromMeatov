using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class EFM_TradeVolume : MonoBehaviour
    {
		public Renderer mainContainerRenderer;
		public Transform itemsRoot;
		public List<EFM_CustomItemWrapper.ResetColPair> resetColPairs;
		public EFM_MarketManager market;

		public void SetContainerHovered(bool hovered)
		{
			Material matToUse = hovered ? Mod.quickSlotHoverMaterial : Mod.quickSlotConstantMaterial;
			mainContainerRenderer.material = matToUse;
		}

		public void AddItem(FVRPhysicalObject item)
        {
			Collider[] cols = item.gameObject.GetComponentsInChildren<Collider>(true);
			if (resetColPairs == null)
			{
				resetColPairs = new List<EFM_CustomItemWrapper.ResetColPair>();
			}
			EFM_CustomItemWrapper.ResetColPair resetColPair = null;
			foreach (Collider col in cols)
			{
				if (col.gameObject.layer == 0 || col.gameObject.layer == 14)
				{
					col.isTrigger = true;

					// Create new resetColPair for each collider so we can reset those specific ones to non-triggers when taken out of the backpack
					if (resetColPair == null)
					{
						resetColPair = new EFM_CustomItemWrapper.ResetColPair();
						resetColPair.physObj = item;
						resetColPair.colliders = new List<Collider>();
					}
					resetColPair.colliders.Add(col);
				}
			}
			if (resetColPair != null)
			{
				resetColPairs.Add(resetColPair);
			}
			item.SetParentage(itemsRoot);
			item.RootRigidbody.isKinematic = true;
		}
	}
}
