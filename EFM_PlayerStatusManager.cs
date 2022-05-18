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
        private Vector3 setPosition;
        private Quaternion setRotation;

        public Text[] partHealthTexts;
        public Image[] partHealthImages;
        public Text healthText;
        public Text healthDeltaText;
        public Text hydrationText;
        public Text hydrationDeltaText;
        public Text energyText;
        public Text energyDeltaText;
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
                partHealthImages[i] = partImageParent.GetChild(i).GetComponent<Image>();
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

            // Set background pointable
            FVRPointable backgroundPointable = transform.GetChild(0).gameObject.AddComponent<FVRPointable>();
            backgroundPointable.MaxPointingRange = 30;

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

            if (displayed && Vector3.Distance(setPosition, GM.CurrentPlayerRoot.position) > 10)
            {
                transform.GetChild(0).gameObject.SetActive(false);
                transform.GetChild(1).gameObject.SetActive(false);
                displayed = false;
            }

            // Left (TODO: Non main hand) menu button
            if (Mod.leftHand.fvrHand.Input.BYButtonDown)
            {
                if (displayed)
                {
                    displayed = false;
                    transform.GetChild(0).gameObject.SetActive(false);
                    transform.GetChild(1).gameObject.SetActive(false);
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
                setPosition = Mod.leftHand.transform.position + Mod.leftHand.transform.forward * 0.6f + Mod.leftHand.transform.right * 0.3f;
                setRotation = Quaternion.Euler(0, Mod.leftHand.transform.rotation.eulerAngles.y, 0);
            }
            if (displayed)
            {
                transform.position = setPosition;
                transform.rotation = setRotation;
            }

            UpdateStamina();
        }

        private void UpdateStamina()
        {
            Vector3 movementVector = (Vector3)typeof(FVRMovementManager).GetField("m_twoAxisVelocity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(GM.CurrentMovementManager);
            bool sprintEngaged = (bool)typeof(FVRMovementManager).GetField("m_sprintingEngaged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(GM.CurrentMovementManager);

            if (sprintEngaged)
            {
                // Reset stamina timer
                Mod.staminaTimer = 2;

                float currentStaminaDrain = Mod.sprintStaminaDrain * Time.deltaTime;

                if (Mod.weight > Mod.currentWeightLimit)
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
            else if (movementVector.magnitude > 0 && Mod.weight > Mod.currentWeightLimit)
            {
                // Reset stamina timer
                Mod.staminaTimer = 2;

                float currentStaminaDrain = Mod.overweightStaminaDrain * Time.deltaTime;

                Mod.stamina = Mathf.Max(Mod.stamina - currentStaminaDrain, 0);

                Mod.staminaBarUI.transform.GetChild(0).GetChild(1).GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mod.stamina);
            }
            else if(Mod.weight > Mod.currentWeightLimit + Mod.currentWeightLimit / 100 * 20)
            {
                // Reset stamina timer to prevent stamina regen even while not moving if we are 20% above max weight
                Mod.staminaTimer = 2;
            }
            else // Not using stamina
            {
                if (Mod.staminaTimer > 0)
                {
                    Mod.staminaTimer -= Time.deltaTime;
                }
                else
                {
                    Mod.stamina = Mathf.Min(Mod.stamina + Mod.staminaRestoration * Time.deltaTime, Mod.currentMaxStamina);

                    Mod.staminaBarUI.transform.GetChild(0).GetChild(1).GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mod.stamina);
                }
            }

            // If reach 0 stamina due to being overweight, activate overweight fatigue effect
            if (Mod.stamina == 0 && Mod.weight > Mod.currentWeightLimit)
            {
                // TODO: maybe keep whether we have overweight fatigue as a bool in effects so we dont have to check the whole list every frame
                bool found = false;
                foreach (EFM_Effect effect in EFM_Effect.effects)
                {
                    if (effect.effectType == EFM_Effect.EffectType.OverweightFatigue)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    EFM_Effect newEffect = new EFM_Effect();
                    newEffect.effectType = EFM_Effect.EffectType.OverweightFatigue;
                    EFM_Effect.effects.Add(newEffect);

                    // Activate overweight fatigue icon
                    transform.GetChild(0).GetChild(2).GetChild(9).gameObject.SetActive(true);
                }
            }
            else
            {
                for (int i = 0; i < EFM_Effect.effects.Count; ++i)
                {
                    if (EFM_Effect.effects[i].effectType == EFM_Effect.EffectType.OverweightFatigue)
                    {
                        // The overweight fatigue could also have caused an energy rate effect, need to remove that too
                        if (EFM_Effect.effects[i].caused.Count > 0)
                        {
                            EFM_Effect.effects.Remove(EFM_Effect.effects[i].caused[0]);
                        }
                        EFM_Effect.effects.RemoveAt(i);

                        // Deactivate overweight fatigue icon
                        transform.GetChild(0).GetChild(2).GetChild(9).gameObject.SetActive(false);

                        break;
                    }
                }
            }
        }

        private void OnExitClick()
        {
            buttonClickAudio.Play();
            transform.GetChild(0).gameObject.SetActive(false);
            transform.GetChild(1).gameObject.SetActive(false);
        }

        public void UpdateWeight()
        {
            weightText.text = Mod.weight.ToString() + "/ " + Mod.currentWeightLimit;
            if(Mod.weight > Mod.currentWeightLimit + Mod.currentWeightLimit / 100 * 20) // Current weight limit + 20%
            {
                weightText.color = Color.red;

                // Enable hard overweight icon, disable overweight icon
                transform.GetChild(0).GetChild(2).GetChild(7).gameObject.SetActive(true);
                transform.GetChild(0).GetChild(2).GetChild(6).gameObject.SetActive(false);
            }
            else if(Mod.weight > Mod.currentWeightLimit)
            {
                weightText.color = Color.yellow;

                // Enable overweight icon, disable hard overweight icon
                transform.GetChild(0).GetChild(2).GetChild(6).gameObject.SetActive(true);
                transform.GetChild(0).GetChild(2).GetChild(7).gameObject.SetActive(false);
            }
            else
            {
                weightText.color = Color.white;

                // Disable overweight icons
                transform.GetChild(0).GetChild(2).GetChild(6).gameObject.SetActive(false);
                transform.GetChild(0).GetChild(2).GetChild(7).gameObject.SetActive(false);
            }
        }

        public void UpdatePlayerLevel()
        {
            transform.GetChild(0).GetChild(10).GetChild(0).GetComponent<Image>().sprite = Mod.playerLevelIcons[Mod.level / 5];
            transform.GetChild(0).GetChild(10).GetChild(1).GetComponent<Text>().text = Mod.level.ToString();
        }
    }
}
