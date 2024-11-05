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

            Mod.currentLocationIndex = -1;

            // TP Player
            GM.CurrentMovementManager.TeleportToPoint(spawn.position, false, spawn.rotation.eulerAngles);

            // Set to no quickbelt slot
            ConfigureQuickbeltPatch.overrideIndex = true;
            ConfigureQuickbeltPatch.actualConfigIndex = -4;
            GM.CurrentPlayerBody.ConfigureQuickbelt(-4);

            // Enforce specific options
            QualitySettings.lodBias = 5;
            GM.Options.MovementOptions.TPLocoSpeedIndex = 2; // Limits walk speed to 1.8, sprint to 3.6
            GM.Options.SimulationOptions.ObjectGravityMode = SimulationOptions.GravityMode.Realistic;
            GM.Options.SimulationOptions.PlayerGravityMode = SimulationOptions.GravityMode.Realistic;
            GM.Options.SimulationOptions.BallisticGravityMode = SimulationOptions.GravityMode.Realistic;
            GM.Options.SaveToFile();
        }

        public override void InitUI()
        {
            // Set buttons activated depending on presence of save files
            Mod.FetchAvailableSaveFiles();
            if(Mod.availableSaveFiles.Count > 0)
            {
                for (int i = 0; i < 6; ++i)
                {
                    loadButtons[i].gameObject.SetActive(Mod.availableSaveFiles.Contains(i));
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
            Mod.LoadHideout();
            pages[0].SetActive(false);
            pages[2].SetActive(true);
            clickAudio.Play();
        }

        public void OnContinueClicked()
        {
            Mod.LoadHideout(-1, true);
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
            Mod.LoadHideout(slotIndex);
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
