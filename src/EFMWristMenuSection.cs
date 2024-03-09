using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using H3MP.Scripts;
using H3MP.Tracking;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class EFMWristMenuSection : FVRWristMenuSection
    {
        public static bool init;

        delegate void ButtonClick(Text text);
        Dictionary<int, List<KeyValuePair<FVRPointableButton, Vector3>>> pages;
        int currentPage = -1;

        public override void Enable()
        {
            // Init buttons if not already done
            InitButtons();

            SetPage(0);
        }

        private void InitButtons()
        {
            if (pages != null)
            {
                return;
            }

            pages = new Dictionary<int, List<KeyValuePair<FVRPointableButton, Vector3>>>();

            Image background = gameObject.AddComponent<Image>();
            background.rectTransform.sizeDelta = new Vector2(500, 350);
            background.color = new Color(0.1f, 0.1f, 0.1f, 1);

            Text textOut = null;
            InitButton(new List<int>() { 0 }, new List<Vector3>() { new Vector3(0, 75, 0) }, new Vector2(1000, 150), new Vector2(140, 70), OnDevClicked, "Dev", out textOut);
            InitButton(new List<int>() { 0 }, new List<Vector3>() { new Vector3(0, 0, 0) }, new Vector2(1200, 150), new Vector2(140, 70), OnStatusClicked, "Status", out textOut);
            InitButton(new List<int>() { 1 }, new List<Vector3>() { new Vector3(0, 75, 0) }, new Vector2(500, 240), new Vector2(140, 70), OnItemsClicked, "Items", out textOut);
            //InitButton(new List<int>() { 1 }, new List<Vector3>() { Vector3.zero }, new Vector2(500, 240), new Vector2(140, 70), OnConnectClicked, "Join", out textOut);
            //InitButton(new List<int>() { 0, 1, 2, 3 }, new List<Vector3>() { new Vector3(0, -75, 0), new Vector3(0, -75, 0), new Vector3(0, -75, 0), new Vector3(0, -75, 0) }, new Vector2(500, 150), new Vector2(140, 70), OnOptionsClicked, "Options", out textOut);
            //InitButton(new List<int>() { 2 }, new List<Vector3>() { new Vector3(0, 75, 0) }, new Vector2(500, 240), new Vector2(140, 70), OnCloseClicked, "Close\nserver", out textOut);
            //InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(0, 75, 0) }, new Vector2(500, 240), new Vector2(140, 70), OnDisconnectClicked, "Disconnect", out textOut);
            InitButton(new List<int>() { 1, 2 }, new List<Vector3>() { new Vector3(-215, 140, 0), new Vector3(-215, 140, 0), new Vector3(-215, 140, 0), new Vector3(-215, 140, 0) }, new Vector2(240, 240), new Vector2(70, 70), OnBackClicked, "Back", out textOut);
            //InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(0, 150, 0) }, new Vector2(1200, 150), new Vector2(270, 45), OnReloadConfigClicked, "Reload config", out textOut);
            //InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(0, 100, 0) }, new Vector2(1200, 150), new Vector2(270, 45), OnItemInterpolationClicked, "Item interpolation (ON)", out textOut);
            //InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(0, 50, 0) }, new Vector2(1200, 150), new Vector2(270, 45), OnTNHReviveClicked, "TNH revive", out textOut);
            //InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(0, 0, 0) }, new Vector2(1200, 150), new Vector2(270, 45), OnIFFClicked, "Current IFF: " + GM.CurrentPlayerBody.GetPlayerIFF(), out IFFText);
            //InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(155, 0, 0) }, new Vector2(150, 150), new Vector2(45, 45), OnNextIFFClicked, ">", out textOut);
            //InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(-155, 0, 0) }, new Vector2(150, 150), new Vector2(45, 45), OnPreviousIFFClicked, "<", out textOut);
            //InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(0, -50, 0) }, new Vector2(1200, 150), new Vector2(270, 45), OnColorByIFFClicked, "Color by IFF (" + GameManager.colorByIFF + ")", out colorByIFFText);
            //InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(0, -100, 0) }, new Vector2(1200, 150), new Vector2(340, 45), OnNameplatesClicked, "Nameplates (" + (GameManager.nameplateMode == 0 ? "All" : (GameManager.nameplateMode == 1 ? "Friendly Only" : "None")) + ")", out nameplateText);
            //InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(0, -150, 0) }, new Vector2(1200, 150), new Vector2(340, 45), OnHostStartHoldClicked, "Debug: Host start hold", out textOut);

            //InitButton(new List<int>() { 5 }, new List<Vector3>() { new Vector3(0, 150, 0) }, new Vector2(1200, 150), new Vector2(340, 45), OnTNHRadarModeClicked, "Radar mode (" + (GameManager.radarMode == 0 ? "All" : (GameManager.radarMode == 1 ? "Friendly Only" : "None")) + ")", out radarModeText);
            //InitButton(new List<int>() { 5 }, new List<Vector3>() { new Vector3(0, 100, 0) }, new Vector2(1200, 150), new Vector2(340, 45), OnTNHRadarColorClicked, "Radar color IFF (" + GameManager.radarColor + ")", out radarColorText);
            //InitButton(new List<int>() { 5 }, new List<Vector3>() { new Vector3(0, 50, 0) }, new Vector2(1200, 150), new Vector2(340, 45), OnMaxHealthClicked, "Max health: " + (GameManager.maxHealthIndex == -1 ? "Not set" : GameManager.maxHealths[GameManager.maxHealthIndex].ToString()), out maxHealthText);
            //InitButton(new List<int>() { 5 }, new List<Vector3>() { new Vector3(155, 50, 0) }, new Vector2(150, 150), new Vector2(45, 45), OnNextMaxHealthClicked, ">", out textOut);
            //InitButton(new List<int>() { 5 }, new List<Vector3>() { new Vector3(-155, 50, 0) }, new Vector2(150, 150), new Vector2(45, 45), OnPreviousMaxHealthClicked, "<", out textOut);
            //InitButton(new List<int>() { 5 }, new List<Vector3>() { new Vector3(0, 0, 0) }, new Vector2(1200, 150), new Vector2(340, 45), OnSetRespawnPointClicked, "Set respawn point", out textOut);
            //InitButton(new List<int>() { 5 }, new List<Vector3>() { new Vector3(0, -50, 0) }, new Vector2(1200, 150), new Vector2(340, 45), OnDisableHoldSosigsClicked, "Debug: Disable Hold Sosigs", out textOut);
            //InitButton(new List<int>() { 5 }, new List<Vector3>() { new Vector3(0, -100, 0) }, new Vector2(1200, 150), new Vector2(340, 45), OnInvulnerableClicked, "Debug: Invulnerable: " + invulnerable, out invulnerableText);
            //InitButton(new List<int>() { 5 }, new List<Vector3>() { new Vector3(0, -150, 0) }, new Vector2(1200, 150), new Vector2(340, 45), OnTPToPlayerClicked, "Debug: TP to player", out textOut);
            //InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(215, -140, 0) }, new Vector2(240, 240), new Vector2(70, 70), OnNextOptionsClicked, "Next", out textOut);
            //InitButton(new List<int>() { 5 }, new List<Vector3>() { new Vector3(-215, -140, 0) }, new Vector2(240, 240), new Vector2(70, 70), OnPrevOptionsClicked, "Prev", out textOut);
        }

        private void InitButton(List<int> pageIndices, List<Vector3> positions, Vector2 sizeDelta, Vector2 boxSize, ButtonClick clickMethod, string defaultText, out Text textOut)
        {
            GameObject button = Instantiate(this.Menu.BaseButton, transform);
            RectTransform buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = sizeDelta;
            buttonRect.GetChild(0).GetComponent<RectTransform>().sizeDelta = sizeDelta;
            BoxCollider boxCollider = button.GetComponent<BoxCollider>();
            boxCollider.size = new Vector3(boxSize.x, boxSize.y, boxCollider.size.z);
            button.transform.localPosition = positions[0];
            button.transform.localRotation = Quaternion.identity;
            Destroy(button.GetComponent<FVRWristMenuSectionButton>());
            button.GetComponent<Text>().text = defaultText;
            FVRPointableButton BTN_Ref = button.GetComponent<FVRPointableButton>();
            Text buttonText = button.GetComponent<Text>();
            textOut = buttonText;
            BTN_Ref.Button.onClick.AddListener(() => clickMethod(buttonText));

            for (int i = 0; i < pageIndices.Count; ++i)
            {
                if (pages.TryGetValue(pageIndices[i], out List<KeyValuePair<FVRPointableButton, Vector3>> buttons))
                {
                    buttons.Add(new KeyValuePair<FVRPointableButton, Vector3>(BTN_Ref, positions[i]));
                }
                else
                {
                    KeyValuePair<FVRPointableButton, Vector3> entry = new KeyValuePair<FVRPointableButton, Vector3>(BTN_Ref, positions[i]);
                    pages.Add(pageIndices[i], new List<KeyValuePair<FVRPointableButton, Vector3>>() { entry });
                }
            }
        }

        private void OnDevClicked(Text textRef)
        {
            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

            SetPage(1);
        }

        private void OnStatusClicked(Text textRef)
        {
            if(StatusUI.instance == null)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);
            }
            else
            {
                StatusUI.instance.Toggle();
                transform.position = GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.forward * 0.5f;
                transform.rotation = Quaternion.Euler(0, GM.CurrentPlayerBody.Head.rotation.eulerAngles.y, 0);
            }
        }

        private void OnItemsClicked(Text textRef)
        {
            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

            if (DevItemSpawner.instance == null)
            {
                Instantiate(Mod.devItemSpawnerPrefab);
                Vector3 forwardFlat = Vector3.ProjectOnPlane(GM.CurrentPlayerBody.Head.forward, Vector3.up);
                DevItemSpawner.instance.transform.position = GM.CurrentPlayerBody.Head.position + 2 * forwardFlat;
                DevItemSpawner.instance.transform.rotation = Quaternion.LookRotation(forwardFlat);
            }
            else
            {
                Destroy(DevItemSpawner.instance.gameObject);
            }
        }

        private void OnBackClicked(Text textRef)
        {
            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

            SetPage(0);
        }

        private void SetPage(int index)
        {
            // Disable buttons from previous page if applicable
            if (currentPage != -1 && pages.TryGetValue(currentPage, out List<KeyValuePair<FVRPointableButton, Vector3>> previousButtons))
            {
                for (int i = 0; i < previousButtons.Count; ++i)
                {
                    previousButtons[i].Key.gameObject.SetActive(false);
                }
            }

            // Enable buttons of new page and set their positions
            if (pages.TryGetValue(index, out List<KeyValuePair<FVRPointableButton, Vector3>> newButtons))
            {
                for (int i = 0; i < newButtons.Count; ++i)
                {
                    newButtons[i].Key.gameObject.SetActive(true);
                    newButtons[i].Key.GetComponent<RectTransform>().localPosition = newButtons[i].Value;
                }
            }

            currentPage = index;
        }
    }
}
