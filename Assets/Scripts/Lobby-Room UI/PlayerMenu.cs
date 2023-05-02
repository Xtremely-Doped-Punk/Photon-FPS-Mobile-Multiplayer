using Photon.Pun;
using Photon.Realtime;
using PM_FPS;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Task
{
    public class PlayerMenu : MonoBehaviourPunCallbacks
    {
        [SerializeField] private CanvasesLoader _canvasesLoader;
        [SerializeField] private List<PlayerListing> _avatars = null;
        [SerializeField] private GameObject _startBtn;
        [SerializeField] private Material _playerMaterialRef; public Material MaterialRef => _playerMaterialRef;

        private List<Player> _players = new();

        private List<PlayerListing> AvailableAvatars => _avatars.FindAll(x => !x.IsSelected);
        private List<PlayerListing> NotAvailableAvatars => _avatars.FindAll(x => x.IsSelected);
        private bool IsHost => PhotonNetwork.IsMasterClient;

        private int _selectedIndex = -1;

        public static PlayerMenu Instance;
        private void Awake()
        {
            Instance = this;
            for (int i=0; i< _avatars.Count; i++)
            {
                _avatars[i].idx = i;
            }
        }
        #region CallBacks
        public override void OnJoinedRoom()
        {
            _players = PhotonNetwork.CurrentRoom.Players.Values.ToList();
            /*
            foreach (PlayerListing playerListing in _avatars) 
            {
                playerListing.ClearSelection();
            }
            */
            // initialize start button to host only
            _startBtn.SetActive(IsHost);
        }
        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            // pass over master client host scene authority if an player left in room
            _startBtn.SetActive(IsHost);
        }
        public override void OnLeftRoom()
        {
            _selectedIndex = -1;
        }
        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            _players.Add(newPlayer);
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            _players.Remove(otherPlayer);

            if(!otherPlayer.HasRejoined && IsHost)
            {
                // if the player has not rejoined, remove his character selection
                foreach (var avatar in _avatars)
                {
                    // if the avatar is already selected while ownership is given, it needs to be reset
                    // this happens when a player leave the room after selecting a character, ownership is given back to master client
                    if (avatar.IsOwned && avatar.idx != _selectedIndex)
                        avatar.ClearSelection();
                }
            }
        }
        #endregion

        #region Button Punctionallities
        public void OnClickAvatar(int id)
        {
            // if avatar not selected, select it
            if (_selectedIndex == -1)
            {
                _selectedIndex = id;
                _avatars[_selectedIndex].SelectAvatar();
            }
            // if avatar selected, diselect it
            else
            {
                _avatars[_selectedIndex].ClearSelection();
                _selectedIndex = id;
                _avatars[_selectedIndex].SelectAvatar();
            }
        }

        public void OnClickRandom()
        {
            if (_selectedIndex != -1)
            {
                _avatars[_selectedIndex].ClearSelection();
                _selectedIndex = -1;
            }
            else // two time press will select random avatar
            {
                var NotSelectedAvatars = AvailableAvatars;
                _selectedIndex = Random.Range(0, NotSelectedAvatars.Count);
                NotSelectedAvatars[_selectedIndex].SelectAvatar();
            }
        }

        public void OnClickStartGane()
        {
            if (_selectedIndex == -1)
                OnClickRandom();

            if (IsHost)
            {
                SetupPlayerMaterials(); 
                StartCoroutine(NetworkManager.singleton.ChangeScene(1)); // build index of game scene = 1
            }
        }

        private void SetupPlayerMaterials()
        {
            NetworkManager.singleton.RuntimePlayerMaterialAssets.Clear();
            foreach (PlayerListing avatar in NotAvailableAvatars)
            {
                avatar.CreatePlayerMaterial();
            }
        }

        public void OnClickExit()
        {
            foreach (PlayerListing avatar in NotAvailableAvatars) // updated only locallly
            {
                avatar.ClearSelection();
            }

            PhotonNetwork.LeaveRoom(true);
            _canvasesLoader.ActivatePrev();
        }
        #endregion
    }
}