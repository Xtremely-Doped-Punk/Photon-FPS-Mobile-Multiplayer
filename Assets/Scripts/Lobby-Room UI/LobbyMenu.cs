using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Task
{
    public class LobbyMenu : MonoBehaviourPunCallbacks
    {
        [SerializeField] private CanvasesLoader _canvasesLoader;
        [SerializeField] private GameObject UserNameUI;
        [SerializeField] private Button GoogleLoginBtn;
        [SerializeField] private Button GuestLoginBtn;
        [SerializeField] private TMP_InputField nameInputField;
        [SerializeField] private TMP_Text _connecting;

        #region Callbacks

        public override void OnConnectedToMaster()
        {
            _connecting.text = "Connected";

            GoogleLoginBtn.interactable = false;
            GuestLoginBtn.interactable = false;

            UserNameUI.SetActive(true);
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            _connecting.text = "Disconnected";

            GoogleLoginBtn.interactable = true;
            GuestLoginBtn.interactable = true;

            UserNameUI.SetActive(false);
        }

        #endregion

        #region Button Fn

        public void OnCLickGoogleLogin()
        {
            // login googgle
            NetworkManager.singleton.StartConnection();

            // retrive user name and show in username inp field
            nameInputField.text = "google_user";


            // disable login buttons if successfull
            _connecting.text = "Google Login Connecting...";
        }

        public void OnClickGuestLogin()
        {
            // local login in
            NetworkManager.singleton.StartConnection();

            // retrive user name if available locally
            nameInputField.text = GameSettings.Instance.UserName;

            // disable login buttons if successfull
            _connecting.text = "Local Login Connecting...";
        }

        public void OnClickJoinLobby()
        {
            // save info in local settings
            GameSettings.Instance.UserName = nameInputField.text;

            NetworkManager.singleton.JoinLobby(); // join default lobby to get room list updates

            // diable login menu canvas and show room list canvas
            _canvasesLoader.ActivateNext();
        }

        #endregion
    }
}