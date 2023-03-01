using UnityEngine;
using TMPro;
using Photon.Pun;
using System.Collections.Generic;
using Photon.Realtime;
using UnityEngine.Rendering;

namespace Task
{
    public class RoomMenu : MonoBehaviourPunCallbacks
    {
        [SerializeField] private CanvasesLoader _canvasesLoader;
        [SerializeField] private TMP_Text _inputRoomName;
        [SerializeField] private Transform _contentParent;
        [SerializeField] private RoomListing _roomListingPrefab;
        [SerializeField] private TMP_Text _loadStats;

        [SerializeField] private SerializedDictionary<string, RoomListing> _listings = new();

        public static RoomMenu Instance;
        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            //Load Player Stats
            var (player_name, (k, d, a)) = NetworkManager.singleton.LoadPlayerStats();
            var nick_name = PhotonNetwork.LocalPlayer.NickName;
            if ((player_name != null) && (string.Compare(player_name.ToLower(), nick_name.ToLower()) == 0))
                _loadStats.text = "Player Stats Found\n" + $"{nick_name} : KDA = [{k}/{d}/{a}]";
            else
                _loadStats.text = "Player Stats Not Found\n" + $"{nick_name} : KDA = [na/na/na]";
        }

        public void OnClickCreateRoom()
        {
            CreateRoom(_inputRoomName.text);
        }

        public void CreateRoom(string roomname)
        {
            NetworkManager.singleton.JoinOrCreateRoom(roomname);

            // diable room menu canvas and show player list canvas
            _canvasesLoader.ActivateNext();
        }

        #region Callbacks

        public override void OnJoinedLobby()
        {
            foreach (RoomListing roomListing in _listings.Values)
            {
                Destroy(roomListing.gameObject);
            }
            _listings.Clear();
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {// this callback in seen only seen at lobby (not while in any room or not while not connected to lobby)
            RoomListing listing;

            foreach (RoomInfo info in roomList)
            {
                if (info.RemovedFromList)
                {
                    if (_listings.TryGetValue(info.Name, out listing))
                    {
                        Destroy(listing.gameObject);
                        _listings.Remove(info.Name);
                    }
                }
                else
                {
                    if (_listings.TryGetValue(info.Name, out listing))
                    {
                        listing.SetRoomInfo(info);
                    }
                    else
                    {
                        listing = Instantiate(_roomListingPrefab, _contentParent);

                        _listings.Add(info.Name, listing);
                        listing.SetRoomInfo(info);

                    }
                }
            }
        }

        #endregion
    }
}