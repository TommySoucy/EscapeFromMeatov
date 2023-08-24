﻿using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class PointableButton : FVRPointableButton
    {
		public GameObject[] hoverGraphics;
		public AudioSource hoverSound;
		public Text buttonText;
		public bool toggleTextColor;

		public override void BeginHoverDisplay()
		{
			base.BeginHoverDisplay();

			if (hoverGraphics != null)
			{
				foreach (GameObject hoverGraphic in hoverGraphics)
				{
					hoverGraphic.SetActive(!hoverGraphic.activeSelf);
				}
			}
			if(buttonText != null && toggleTextColor)
            {
				buttonText.color = new Color(1 - buttonText.color.r, 1 - buttonText.color.g, 1 - buttonText.color.b);
            }

			hoverSound.Play();
		}
		
		public override void EndHoverDisplay()
		{
			base.EndHoverDisplay();

			if (hoverGraphics != null)
			{
				foreach (GameObject hoverGraphic in hoverGraphics)
				{
					hoverGraphic.SetActive(!hoverGraphic.activeSelf);
				}
			}
			if (buttonText != null && toggleTextColor)
			{
				buttonText.color = new Color(1 - buttonText.color.r, 1 - buttonText.color.g, 1 - buttonText.color.b);
			}
		}
	}
}