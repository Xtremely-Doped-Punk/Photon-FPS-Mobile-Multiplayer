using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable; // as there is an ambiguity bet System.Collections.Hastable and Photon.Hastable

namespace Task
{
    public abstract class NetworkObject: MonoBehaviourPunCallbacks
    {
        //[HideInInspector] 
        public string path;
    }

    public class PlayerManager : NetworkObject
    {
        public const string HASH_KILLS = "Kills";
        public const string HASH_DEATHS = "Deaths";
        public const string HASH_ASSISTS = "Assists";

        [SerializeField] private PlayerController _PlayerControllerPrefab;

        private GameObject _controllerInstance;

        int kills; int deaths; int assists;

        private void Start()
        {
            if (photonView.IsMine)
                CreateController();
        }

        private void CreateController()
        {
            Debug.Log("Instantiated Player Controller");
            Transform spawnpoint = SpawnManager.GetSpawnPoint();

            // Instantiate Player Controller
            _controllerInstance =  
                PhotonNetwork.Instantiate(
                    NetworkManager.GetAssetResoursePath(_PlayerControllerPrefab), 
                    spawnpoint.position, spawnpoint.rotation, 
                    0, new object[] { photonView.ViewID}); // need to be sent as object array 
            // giving some info as instantiation data to the instance, which can be accessed inside that instance

            _controllerInstance.GetComponent<PlayerController>().playerManager = this; 
            // just to save time, rather dhan seraching all photonview object
        }

        public void OnDie()
        {
            PhotonNetwork.Destroy(_controllerInstance);
            CreateController();

            deaths++;

            Hashtable hash = PhotonNetwork.LocalPlayer.CustomProperties;
            if (hash.ContainsKey(HASH_DEATHS))
                hash[HASH_DEATHS] = deaths;
            else
                hash.Add(HASH_DEATHS, deaths);
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        }

        public void GetKill()
        {
            photonView.RPC(nameof(RPC_GetKill), photonView.Owner);
        }

        [PunRPC] private void RPC_GetKill()
        {
            kills++;

            Hashtable hash = PhotonNetwork.LocalPlayer.CustomProperties;
            if (hash.ContainsKey(HASH_KILLS))
                hash[HASH_KILLS] = kills;
            else
                hash.Add(HASH_KILLS, kills);
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        }
        
        public void GetAssist(List<Player> owners)
        {
            foreach (Player owner in owners)
            {
                photonView.RPC(nameof(RPC_GetAssist), owner);
            }
        }

        [PunRPC] private void RPC_GetAssist()
        {
            assists++;

            Hashtable hash = PhotonNetwork.LocalPlayer.CustomProperties;
            if (hash.ContainsKey(HASH_ASSISTS))
                hash[HASH_ASSISTS] = assists;
            else
                hash.Add(HASH_ASSISTS, assists);
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        }
    }
}