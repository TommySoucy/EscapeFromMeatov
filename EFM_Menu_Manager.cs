using UnityEngine;
using UnityEngine.UI;
using FistVR;

namespace EFM
{
    class EFM_Menu_Manager : EFM_Manager
    {
        private Button[][] buttons;
        private Transform canvas;
        private AudioSource clickAudio;

        public override void InitUI()
        {
            buttons = new Button[2][];
            buttons[0] = new Button[4];
            buttons[1] = new Button[7];

            // Fetch buttons
            canvas = GameObject.Find("Menu").transform.GetChild(0).GetChild(0);
            buttons[0][0] = canvas.GetChild(0).GetChild(1).GetComponent<Button>(); // New Game
            buttons[0][1] = canvas.GetChild(0).GetChild(2).GetComponent<Button>(); // Continue
            buttons[0][2] = canvas.GetChild(0).GetChild(3).GetComponent<Button>(); // Load
            buttons[0][3] = canvas.GetChild(0).GetChild(4).GetComponent<Button>(); // Back

            buttons[1][0] = canvas.GetChild(1).GetChild(1).GetComponent<Button>(); // Load Slot 0
            buttons[1][1] = canvas.GetChild(1).GetChild(2).GetComponent<Button>(); // Load Slot 1
            buttons[1][2] = canvas.GetChild(1).GetChild(3).GetComponent<Button>(); // Load Slot 2
            buttons[1][3] = canvas.GetChild(1).GetChild(4).GetComponent<Button>(); // Load Slot 3
            buttons[1][4] = canvas.GetChild(1).GetChild(5).GetComponent<Button>(); // Load Slot 4
            buttons[1][5] = canvas.GetChild(1).GetChild(6).GetComponent<Button>(); // Load Auto save
            buttons[1][6] = canvas.GetChild(1).GetChild(7).GetComponent<Button>(); // Load Back

            // Fetch audio sources
            AudioSource hoverAudio = canvas.transform.GetChild(4).GetComponent<AudioSource>();
            clickAudio = canvas.transform.GetChild(5).GetComponent<AudioSource>();

            // Create an EFM_PointableButton for each button
            for (int i = 0; i < buttons.Length; ++i)
            {
                for (int j = 0; j < buttons[i].Length; ++j)
                {
                    EFM_PointableButton pointableButton = buttons[i][j].gameObject.AddComponent<EFM_PointableButton>();

                    pointableButton.SetButton();
                    pointableButton.MaxPointingRange = 5;
                    pointableButton.hoverGraphics = new GameObject[2];
                    pointableButton.hoverGraphics[0] = pointableButton.transform.GetChild(0).gameObject; // Background
                    pointableButton.hoverGraphics[1] = pointableButton.transform.GetChild(1).gameObject; // Hover
                    pointableButton.buttonText = pointableButton.transform.GetChild(2).GetComponent<Text>();
                    pointableButton.toggleTextColor = true;
                    pointableButton.hoverSound = hoverAudio;
                }
            }

            // Set OnClick for each button
            buttons[0][0].onClick.AddListener(OnNewGameClicked);
            buttons[0][1].onClick.AddListener(OnContinueClicked);
            buttons[0][2].onClick.AddListener(OnLoadClicked);
            buttons[0][3].onClick.AddListener(OnMainBackClicked);

            buttons[1][0].onClick.AddListener(() => { OnLoadSlotClicked(0); });
            buttons[1][1].onClick.AddListener(() => { OnLoadSlotClicked(1); });
            buttons[1][2].onClick.AddListener(() => { OnLoadSlotClicked(2); });
            buttons[1][3].onClick.AddListener(() => { OnLoadSlotClicked(3); });
            buttons[1][4].onClick.AddListener(() => { OnLoadSlotClicked(4); });
            buttons[1][5].onClick.AddListener(() => { OnLoadSlotClicked(5); });
            buttons[1][6].onClick.AddListener(OnBackClicked);

            // Set buttons activated depending on presence of save files
            if(availableSaveFiles.Count > 0)
            {
                for (int i = 0; i < 6; ++i)
                {
                    buttons[1][i].gameObject.SetActive(availableSaveFiles.Contains(i));
                }
            }
            else
            {
                buttons[0][1].gameObject.SetActive(false);
                buttons[0][2].gameObject.SetActive(false);
            }
        }

        public void OnNewGameClicked()
        {
            LoadBase();
            clickAudio.Play();
        }

        public void OnContinueClicked()
        {
            LoadBase(-1, true);
            clickAudio.Play();
        }

        public void OnLoadClicked()
        {
            canvas.GetChild(0).gameObject.SetActive(false);
            canvas.GetChild(1).gameObject.SetActive(true);
            clickAudio.Play();
        }

        public void OnLoadSlotClicked(int slotIndex)
        {
            LoadBase(slotIndex);
            clickAudio.Play();
        }

        public void OnBackClicked()
        {
            canvas.GetChild(1).gameObject.SetActive(false);
            canvas.GetChild(0).gameObject.SetActive(true);
            clickAudio.Play();
        }

        public void OnMainBackClicked()
        {
            Mod.menuBundle.Unload(true);
            if (Mod.baseBundle != null)
            {
                Mod.baseBundle.Unload(true);
            }
            clickAudio.Play();

            SteamVR_LoadLevel.Begin("MainMenu3", false, 0, 0f, 0f, 0f, 1f);
        }
    }
}
