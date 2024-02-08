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
    public class PlayerStatusManager : MonoBehaviour
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
        public Text extractionTimerText;
        private Transform notificationsParent;
        private GameObject notificationPrefab;
        private AudioSource notificationSound;
        private AudioSource openSound;
        private AudioSource closeSound;
        public SkillUI[] skills;
        public string currentZone;

        private AudioSource buttonClickAudio;
        private List<TraderTask> taskList;
        private List<GameObject> taskUIList;
        private Dictionary<string, TraderTask> taskByID;
        private Dictionary<string, GameObject> taskUIByID;

        public bool displayed;
        private int mustUpdateTaskListHeight;

        public void AddNotification(string text)
        {
            notificationSound.Play();
            GameObject notification = Instantiate(notificationPrefab, notificationsParent);
            notification.SetActive(true);
            notification.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = text;
            notification.AddComponent<DestroyTimer>().timer = 5;
            if(notificationsParent.childCount > 10)
            {
                GameObject firstChild = notificationsParent.GetChild(1).gameObject;
                firstChild.SetActive(false);
                Destroy(firstChild);
            }
        }

        public void SetDisplayed(bool displayed)
        {
            this.displayed = displayed;

            transform.GetChild(0).gameObject.SetActive(this.displayed);
            transform.GetChild(1).gameObject.SetActive(this.displayed);
            transform.GetChild(2).gameObject.SetActive(this.displayed);
            transform.GetChild(9).gameObject.SetActive(this.displayed);

            if (this.displayed)
            {
                openSound.Play();
            }
            else
            {
                closeSound.Play();
            }

            foreach (EquipmentSlot equipSlot in StatusUI.instance.equipmentSlots)
            {
                if (equipSlot.CurObject != null)
                {
                    equipSlot.CurObject.gameObject.SetActive(this.displayed);
                }
            }
        }

        private void OnExitClick()
        {
            buttonClickAudio.Play();

            SetDisplayed(false);
        }

        private void OnToggleTaskListClick()
        {
            bool listNowActive = !transform.GetChild(1).GetChild(0).GetChild(1).gameObject.activeSelf;
            transform.GetChild(1).GetChild(0).GetChild(1).gameObject.SetActive(listNowActive);
            transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(!listNowActive);
            transform.GetChild(1).GetChild(0).GetChild(0).GetChild(2).gameObject.SetActive(listNowActive);
        }

        private void OnToggleSkillListClick()
        {
            bool listNowActive = !transform.GetChild(9).GetChild(0).GetChild(1).gameObject.activeSelf;
            transform.GetChild(9).GetChild(0).GetChild(1).gameObject.SetActive(listNowActive);
            transform.GetChild(9).GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(!listNowActive);
            transform.GetChild(9).GetChild(0).GetChild(0).GetChild(2).gameObject.SetActive(listNowActive);
        }

        public void UpdateWeight()
        {
            weightText.text = String.Format("{0:0.#}", Mod.weight / 1000.0f) + "/ " + String.Format("{0:0.#}", Mod.currentWeightLimit / 1000.0f);
            if(Mod.weight > Mod.currentWeightLimit + Mod.currentWeightLimit / 100.0f * 20) // Current weight limit + 20%
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
    }
}
