using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.IO;
using UnityEditor;
using UnityEngine.Rendering;
using System.Collections;

namespace Task
{
    public class NetworkManager : MonoBehaviourPunCallbacks // overwriting existing photon callbacks
    {
        private const string RECENT_PLAYER_SAVE_FILE = "/Resources/LastGameStats.json";
        // singleton
        public static NetworkManager singleton;
        [SerializeField] private GameSettings settings;

        // dont destroy on scene change
        private void Awake()
        {
            if (singleton == null)
            {
                singleton = this;
                DontDestroyOnLoad(gameObject);
            }
            else
                Destroy(this.gameObject);

            if (settings == null) settings = GameSettings.Instance;
            
            //photonView.ViewID = PhotonNetwork.MAX_VIEW_IDS = 999; // cant be done in script, just in inspector put 999 manually
            /* as this gameobject not intialized using Photon.Instanciate(),
            its view id should be manually assigned so that it doesnt mess up other view id no.s
            as each view id of a gameobject are supposed to be unique. */

            PhotonNetwork.LogLevel = PunLogLevel.Full;
        }

        [SerializeField] private bool isConnectedToMaster = false;
        [SerializeField] private bool isJoinedToRoom = false;
        private RoomOptions roomOptions = new RoomOptions();
        TypedLobby lobbyOptions = TypedLobby.Default;
        bool AutoJoinLobby = false;


        #region CallBacks

        public override void OnConnectedToMaster()
        {
            Debug.Log("Connected to Master");
            isConnectedToMaster = true;
            PhotonNetwork.AutomaticallySyncScene = true; // to load game scene at the same time for all players in the lobby
            if (AutoJoinLobby)
            {
                PhotonNetwork.JoinLobby(lobbyOptions);
                AutoJoinLobby = false;
            }
        }

        public override void OnJoinedLobby()
        {
            Debug.Log("Joined Lobby Name: " + PhotonNetwork.CurrentLobby.ToString());
        }

        public override void OnCreatedRoom()
        {
            Debug.Log("Created Room Name: " + PhotonNetwork.CurrentRoom.Name);
        }

        public override void OnJoinedRoom()
        {
            Debug.Log("Joined Room Name: " + PhotonNetwork.CurrentRoom.ToString());
            //PhotonNetwork.LeaveRoom();
        }

        public override void OnLeftRoom()
        {
            Debug.Log("Left Room");

            // leaving room join back from game-server to master-server doesnt automatically connect to lobby
            AutoJoinLobby = true; // thus we add this feature
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogWarning("Room already exists! Can't create room :: "+message);
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.Log("Disconnected from Server :: " + cause.ToString());
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            Debug.Log("Rooms Updated! " + roomList.Count);
        }

        #endregion

        #region Button fns
        public void JoinOrCreateRoom(string roomName)
        {
            if (isConnectedToMaster)
            {
                PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, lobbyOptions);
            }
            else
                Debug.LogError("Can't create a room as the API is not connected to Master!");
        }

        public void StartConnection()
        {
            Debug.Log("Connecting...");
            PhotonNetwork.GameVersion = settings.GameVersion;
            roomOptions.MaxPlayers = (byte)Enum.GetNames(typeof(GameSettings.PlayerCharacter)).Length;
            PhotonNetwork.ConnectUsingSettings(); // connect to master
        }

        public void JoinLobby()
        {
            PhotonNetwork.NickName = settings.UserName;
            if (isConnectedToMaster)
                PhotonNetwork.JoinLobby(lobbyOptions);
        }
        #endregion

        #region Scene fns
        [SerializeField] private PlayerManager PlayerManagerPrefab;

        public SerializedDictionary<int, Material> RuntimePlayerMaterialAssets = new();
        // assigned by player menu (lobby scene) and accessed in each resp player controller (game scene)
                
        public IEnumerator ChangeScene(int sceneIdx = -1, string sceneName = null, float delay = 0.5f)
        {
            Debug.Log("Changing Scene: scene_name = " + sceneName + " or build index = " + sceneIdx);

            /* we are using delay so that other parallel computations can finish first before loading game scenewe are changing
              game scene delayed so that player materials can be set up as both these funtion calls run parallely in unity */
            yield return new WaitForSeconds(delay);

            if (sceneIdx < 0)
            {
                if (sceneName == null)
                {
                    // by default load the next build index scene
                    PhotonNetwork.LoadLevel(SceneManager.GetActiveScene().buildIndex + 1);
                }
                else
                {
                    PhotonNetwork.LoadLevel(sceneName);
                    // pausing network syncing for a while when scene loads (asynchronously)
                }
            }
            else
                PhotonNetwork.LoadLevel(sceneIdx);
        }

        public override void OnEnable()
        {
            base.OnEnable();
            SceneManager.sceneLoaded += HandleOnSceneLoaded;
        }
        public override void OnDisable()
        {
            base.OnDisable();
            SceneManager.sceneLoaded += HandleOnSceneLoaded;
        }

        private void HandleOnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (scene.name.ToLower().Contains("game"))
            {
                // save player stats on application quit automatically by adding it to event
                Application.quitting += SavePlayerStats;

                // instanciate game server objects (note prefab path string must be passed as argument)
                PhotonNetwork.Instantiate(GetAssetResoursePath(PlayerManagerPrefab), Vector3.zero, Quaternion.identity);
            }
        }
        #endregion

        #region Save Stats Json
        private class PlayerStats_SaveData
        {
            public string name;
            public int kills, deaths, assists;
        }
        public void SavePlayerStats()
        {
            var kda = PlayerManager.localInstance.KDA;
            var saveDat = new PlayerStats_SaveData()
            {
                name = PhotonNetwork.LocalPlayer.NickName,
                kills = kda.Item1,
                deaths = kda.Item2,
                assists = kda.Item3
            };
            var jsonDat = JsonUtility.ToJson(saveDat);
            File.WriteAllText(Application.dataPath + RECENT_PLAYER_SAVE_FILE, jsonDat);
            Debug.Log(jsonDat);
        }

        public (string, (int, int, int)) LoadPlayerStats()
        {
            try
            {
                var jsonDat = File.ReadAllText(Application.dataPath + RECENT_PLAYER_SAVE_FILE);
                var savDat =  JsonUtility.FromJson<PlayerStats_SaveData>(jsonDat);
                return (savDat.name, (savDat.kills, savDat.deaths, savDat.assists));
            }
            catch // if file doesnt exits initially
            {
                return (null, (-1, -1, -1));
            }
        }
        #endregion

        #region Assets & Paths
        // NOTE: run atleast once in editor before building,
        // to properly set up paths of prefabs so that they can be intanciated through photon

        public static string GetAssetResoursePath(NetworkObject prefab)
        {
        #if UNITY_EDITOR
            var AbsolutePath = AssetDatabase.GetAssetPath(prefab);
            var RelativePath = Path.GetRelativePath("Assets\\Resources\\", AbsolutePath);
            prefab.path = RelativePath[..RelativePath.LastIndexOf('.')]; // relative path without extention
        #endif
            return prefab.path;
        }
        #endregion
    }
}