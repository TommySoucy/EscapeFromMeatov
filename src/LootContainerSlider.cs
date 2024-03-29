﻿using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class LootContainerSlider : FVRInteractiveObject
	{
		public string keyID;
		public bool hasKey;
		public Transform Root;
		public float MinY;
		public float MaxY;
		public float posZ;
		private bool m_forceOpen;

		public override void Awake()
		{
			// Override interactive object awake entirely to prevent from being added to All because unnecessary
			this.GameObject = gameObject;
			this.Transform = transform;
			this.m_colliders = GetComponentsInChildren<Collider>(true);

			EndInteractionIfDistant = false;
		}

		public void ForceOpen()
		{
			m_forceOpen = true;
		}

		public override bool IsInteractable()
		{
			return m_forceOpen || !hasKey || Mod.playerInventory.ContainsKey(keyID);
		}

		public void Reset()
		{
			transform.localPosition = new Vector3(0, -0.3f, posZ);
		}

		public override void UpdateInteraction(FVRViveHand hand)
		{
			// Note: X: Right, Y: Forward, Z: Up
			base.UpdateInteraction(hand);
			// vector from drawers to hand = hand.transform.position - transform.parent.position
			// that vector projected on vector in opening direction of drawer = Vector3.Project(..., transform.parent.up)
			// how far the hand is from the surface of drawers = ....magnitude
			// if the drawer was moved to have its handle at the hand's position = ... - 0.3
			transform.localPosition = new Vector3(0, Mathf.Clamp(Vector3.Project(hand.transform.position - transform.parent.position, transform.parent.up).magnitude - 0.3f, MinY, MaxY), posZ);
		}
	}
}
