using FistVR;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    class MenuController : UIController
    {
        public Button loadButton;
        public Button continueButton;
        public Button[] loadButtons;
        public AudioSource clickAudio;
        public GameObject[] pages;
        public Transform spawn;
        public Text loadProgressText;

        public override void Awake()
        {
            base.Awake();

            Mod.currentLocationIndex = 0;

            // TP Player
            GM.CurrentMovementManager.TeleportToPoint(spawn.position, true, spawn.rotation.eulerAngles);

            // Set to no quickbelt slot
            GM.CurrentPlayerBody.ConfigureQuickbelt(-4);
        }

        public override void Update()
        {
            base.Update();

            if (loadingHideoutAssets)
            {
                loadProgressText.text = ((int)loadingHideoutAVGProgress * 100).ToString()+"%";
            }
        }

        public override void InitUI()
        {
            // Set buttons activated depending on presence of save files
            if(availableSaveFiles.Count > 0)
            {
                for (int i = 0; i < 6; ++i)
                {
                    loadButtons[i].gameObject.SetActive(availableSaveFiles.Contains(i));
                }
            }
            else
            {
                loadButton.gameObject.SetActive(false);
                continueButton.gameObject.SetActive(false);
            }
        }

        public void OnNewGameClicked()
        {
            LoadHideout();
            pages[0].SetActive(false);
            pages[2].SetActive(true);
            clickAudio.Play();
        }

        public void OnContinueClicked()
        {
            LoadHideout(-1, true);
            pages[0].SetActive(false);
            pages[2].SetActive(true);
            clickAudio.Play();
        }

        public void OnLoadClicked()
        {
            pages[0].SetActive(false);
            pages[1].SetActive(true);
            clickAudio.Play();
        }

        public void OnLoadSlotClicked(int slotIndex)
        {
            LoadHideout(slotIndex);
            pages[1].SetActive(false);
            pages[2].SetActive(true);
            clickAudio.Play();
        }

        public void OnBackClicked()
        {
            pages[1].SetActive(false);
            pages[0].SetActive(true);
            clickAudio.Play();
        }

        public void OnMainBackClicked()
        {
            clickAudio.Play();

            SteamVR_LoadLevel.Begin("MainMenu3", false, 0, 0f, 0f, 0f, 1f);
        }
    }
}
