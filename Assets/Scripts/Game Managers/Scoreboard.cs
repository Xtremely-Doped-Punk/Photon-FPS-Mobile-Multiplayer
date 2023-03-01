﻿using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;
using System.IO;

namespace Task
{
	public class Scoreboard : MonoBehaviourPunCallbacks
	{
		[SerializeField] private Transform _context;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private ScoreboardItem ScoreboardItemPrefab;

		private readonly Dictionary<Player, ScoreboardItem> scoreboardItems = new();

        #region Initilaztion & CallBack Updates
        private void Awake()
        {
            if (_context == null)
				_context = GetComponent<Transform>();
			if (_canvasGroup == null)
			{
				if ((_canvasGroup = _context.GetComponent<CanvasGroup>()) == null)
					_canvasGroup = _context.gameObject.AddComponent<CanvasGroup>();
			}
        }

        private void Start()
		{
			foreach (Player player in PhotonNetwork.PlayerList)
			{
				AddScoreboardItem(player);
			}
		}

		public override void OnPlayerEnteredRoom(Player newPlayer)
		{
			AddScoreboardItem(newPlayer);
		}

		public override void OnPlayerLeftRoom(Player otherPlayer)
		{
			RemoveScoreboardItem(otherPlayer);
		}

		void AddScoreboardItem(Player player)
		{
			ScoreboardItem item = Instantiate(ScoreboardItemPrefab, _context);
			item.Initialize(player);
			scoreboardItems[player] = item;
		}

		void RemoveScoreboardItem(Player player)
		{
			Destroy(scoreboardItems[player].gameObject);
			scoreboardItems.Remove(player);
		}
        #endregion


        void Update()
		{
			// setting game object active per frame is slow, thus we use canvas group
			if (Input.GetKeyDown(KeyCode.Tab))
			{
				_canvasGroup.alpha = 1; // show
			}
			else if (Input.GetKeyUp(KeyCode.Tab))
			{
				_canvasGroup.alpha = 0; // hide
			}
		}

		// temp btn fn to save last played game stats on game exit
		public void ExitGame()
		{
			NetworkManager.singleton.SavePlayerStats(); // for editor
			Application.Quit();
		}
	}
}