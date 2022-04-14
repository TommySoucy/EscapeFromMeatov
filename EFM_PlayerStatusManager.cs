using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EFM
{
    public class EFM_PlayerStatusManager : MonoBehaviour
    {
        private bool init;

        private Text[] partHealthTexts;
        private Image[] partHealthImages;
        private Text healthText;
        private Text healthDeltaText;
        private Text hydrationText;
        private Text hydrationDeltaText;
        private Text energyText;
        private Text energyDeltaText;
        private Text weightText;

        private AudioSource buttonClickAudio;

        private bool displayed;

        public void Init()
        {
            // Set part health elements
            partHealthTexts = new Text[7];
            partHealthImages = new Image[7];
            Transform partHealthParent = transform.GetChild(0).GetChild(7);
            Transform partImageParent = transform.GetChild(0).GetChild(1);
            for (int i = 0; i < 7; ++i)
            {
                partHealthTexts[i] = partHealthParent.GetChild(i).GetChild(1).GetComponent<Text>();
                partHealthImages[i] = partHealthParent.GetChild(i).GetComponent<Image>();
            }
            // Set main stats texts
            healthText = transform.GetChild(0).GetChild(3).GetChild(1).GetComponent<Text>();
            healthDeltaText = transform.GetChild(0).GetChild(3).GetChild(3).GetComponent<Text>();
            hydrationText = transform.GetChild(0).GetChild(4).GetChild(1).GetComponent<Text>();
            hydrationDeltaText = transform.GetChild(0).GetChild(4).GetChild(3).GetComponent<Text>();
            energyText = transform.GetChild(0).GetChild(5).GetChild(1).GetComponent<Text>();
            energyDeltaText = transform.GetChild(0).GetChild(5).GetChild(3).GetComponent<Text>();
            weightText = transform.GetChild(0).GetChild(6).GetChild(1).GetComponent<Text>();
            // Set equipment slots
            Transform equipSlotParent = transform.GetChild(1);
            Mod.equipmentSlots = new List<EFM_EquipmentSlot>();
            for (int i = 0; i < 8; ++i)
            {
                GameObject slotObject = equipSlotParent.GetChild(i).GetChild(0).gameObject;
                slotObject.tag = "QuickbeltSlot";
                slotObject.SetActive(false); // Just so Awake() isn't called until we've set slot components fields

                EFM_EquipmentSlot slotComponent = slotObject.AddComponent<EFM_EquipmentSlot>();
                Mod.equipmentSlots.Add(slotComponent);
                slotComponent.QuickbeltRoot = slotObject.transform;
                slotComponent.HoverGeo = slotObject.transform.GetChild(0).GetChild(0).gameObject;
                slotComponent.HoverGeo.SetActive(false);
                slotComponent.PoseOverride = slotObject.transform.GetChild(0).GetChild(2);
                slotComponent.Shape = FVRQuickBeltSlot.QuickbeltSlotShape.Sphere;
                slotComponent.SizeLimit = FVRPhysicalObject.FVRPhysicalObjectSize.CantCarryBig;
                slotComponent.Type = FVRQuickBeltSlot.QuickbeltSlotType.Standard;
                switch (i)
                {
                    case 0:
                        slotComponent.equipmentType = Mod.ItemType.Backpack;
                        break;
                    case 1:
                        slotComponent.equipmentType = Mod.ItemType.BodyArmor;
                        break;
                    case 2:
                        slotComponent.equipmentType = Mod.ItemType.Earpiece;
                        break;
                    case 3:
                        slotComponent.equipmentType = Mod.ItemType.Headwear;
                        break;
                    case 4:
                        slotComponent.equipmentType = Mod.ItemType.FaceCover;
                        break;
                    case 5:
                        slotComponent.equipmentType = Mod.ItemType.Eyewear;
                        break;
                    case 6:
                        slotComponent.equipmentType = Mod.ItemType.Rig;
                        break;
                    case 7:
                        slotComponent.equipmentType = Mod.ItemType.Pouch;
                        break;
                }

                // Set slot sphere materials
                slotObject.transform.GetChild(0).GetChild(0).GetComponent<Renderer>().material = Mod.quickSlotHoverMaterial;
                slotObject.transform.GetChild(0).GetChild(1).GetComponent<Renderer>().material = Mod.quickSlotConstantMaterial;

                // Reactivate slot
                slotObject.SetActive(true);
            }
            // Set exit button
            EFM_PointableButton exitButton = transform.GetChild(0).GetChild(10).gameObject.AddComponent<EFM_PointableButton>();
            exitButton.SetButton();
            exitButton.MaxPointingRange = 30;
            exitButton.hoverSound = transform.GetChild(3).GetComponent<AudioSource>();
            buttonClickAudio = transform.GetChild(4).GetComponent<AudioSource>();
            exitButton.Button.onClick.AddListener(() => { OnExitClick(); });

            // Set as not active by default
            transform.GetChild(0).gameObject.SetActive(false);
            transform.GetChild(1).gameObject.SetActive(false);

            init = true;
        }

        private void Update()
        {
            if (!init)
            {
                return;
            }

            // Left menu button
            if (Mod.leftHand.fvrHand.Input.BYButtonDown)
            {
                if (displayed)
                {
                    displayed = false;
                }
                else
                {
                    // TODO: Play inventory opening sound
                    transform.GetChild(0).gameObject.SetActive(true);
                    transform.GetChild(1).gameObject.SetActive(true);

                    displayed = true;
                }
            }
            if (Mod.leftHand.fvrHand.Input.BYButtonPressed && displayed)
            {
                transform.position = Mod.leftHand.transform.position + Mod.leftHand.transform.forward * 0.6f + Mod.leftHand.transform.right * -0.3f;
            }

            // Right menu button
            if (Mod.rightHand.fvrHand.Input.BYButtonDown)
            {
                if (displayed)
                {
                    displayed = false;
                }
                else
                {
                    // TODO: Play inventory opening sound
                    transform.GetChild(0).gameObject.SetActive(true);
                    transform.GetChild(1).gameObject.SetActive(true);
                }
            }
            if (Mod.rightHand.fvrHand.Input.BYButtonPressed && displayed)
            {
                transform.position = Mod.leftHand.transform.position + Mod.leftHand.transform.forward * 0.6f + Mod.leftHand.transform.right * -0.3f;
            }

            Vector3 movementVector = (Vector3)typeof(FVRMovementManager).GetField("m_twoAxisVelocity").GetValue(GM.CurrentMovementManager);
            bool sprintEngaged = (bool)typeof(FVRMovementManager).GetField("m_sprintingEngaged").GetValue(GM.CurrentMovementManager);
            if (sprintEngaged)
            {
                // Reset stamina timer
                Mod.staminaTimer = 2;

                float currentStaminaDrain = Mod.sprintStaminaDrain * Time.deltaTime;

                if(Mod.weight > Mod.currentWeightLimit)
                {
                    currentStaminaDrain += Mod.overweightStaminaDrain * Time.deltaTime;
                }

                Mod.stamina = Mathf.Max(Mod.stamina - currentStaminaDrain, 0);

                Mod.staminaBarUI.transform.GetChild(0).GetChild(1).GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mod.stamina);

                //if(Mod.stamina == 0)
                //{
                    // Dont need to do anything here, here movement manager, we will patch to make sure that sprint is disengaged when we reach 0 stamina
                //}
            }
            else if(movementVector.magnitude > 0 && Mod.weight > Mod.currentWeightLimit)
            {
                // Reset stamina timer
                Mod.staminaTimer = 2;

                float currentStaminaDrain = Mod.overweightStaminaDrain * Time.deltaTime;

                Mod.stamina = Mathf.Max(Mod.stamina - currentStaminaDrain, 0);

                Mod.staminaBarUI.transform.GetChild(0).GetChild(1).GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mod.stamina);
            }
            else // Not using stamina
            {
                if(Mod.staminaTimer > 0)
                {
                    Mod.staminaTimer -= Time.deltaTime;
                }
                else
                {
                    Mod.stamina = Mathf.Min(Mod.stamina + Mod.staminaRestoration * Time.deltaTime, Mod.currentMaxStamina);

                    Mod.staminaBarUI.transform.GetChild(0).GetChild(1).GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mod.stamina);
                }
            }
        }

        private void OnExitClick()
        {
            buttonClickAudio.Play();
            transform.GetChild(0).gameObject.SetActive(false);
            transform.GetChild(1).gameObject.SetActive(false);
        }
    }
}
