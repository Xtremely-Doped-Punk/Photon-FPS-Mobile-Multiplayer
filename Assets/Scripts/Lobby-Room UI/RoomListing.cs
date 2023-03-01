using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using Task;
using TMPro;
using UnityEngine;

namespace Task
{
    public class RoomListing : MonoBehaviour
    {
        [SerializeField] TMP_Text _roomNameText;
        [SerializeField] TMP_Text _noOfPlayersText;

        public void SetRoomInfo(RoomInfo roomInfo)
        {
            _roomNameText.text = roomInfo.Name;
            _noOfPlayersText.text = roomInfo.PlayerCount.ToString() + "/" + roomInfo.MaxPlayers.ToString();
        }

        public void OnClickRoom()
        {
            RoomMenu.Instance.CreateRoom(_roomNameText.text);
        }
    }
}