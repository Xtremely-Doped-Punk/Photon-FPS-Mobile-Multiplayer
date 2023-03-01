using TMPro;
using Photon.Realtime;
using Photon.Pun;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Task
{
    public class ScoreboardItem : MonoBehaviourPunCallbacks
    {
        [SerializeField] private TMP_Text _usernameText;
        [SerializeField] private TMP_Text _killsText;
        [SerializeField] private TMP_Text _deathsText;
        [SerializeField] private TMP_Text _assistsText;

        private Player player;

        public void Initialize(Player player)
        {
            this.player = player;

            _usernameText.text = player.NickName;
            StatsUpdate();
        }

        void StatsUpdate(object kills = null, object deaths = null, object assists = null)
        {
            if (kills != null)
                _killsText.text = kills.ToString();

            if (deaths != null)
                _deathsText.text = deaths.ToString();
            
            if (assists != null)
                _assistsText.text = assists.ToString();
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            if (targetPlayer != player) return;

            object k = null, d = null , a = null;

            if (player.CustomProperties.TryGetValue(PlayerManager.HASH_KILLS, out k) || 
                player.CustomProperties.TryGetValue(PlayerManager.HASH_DEATHS, out d) ||
                player.CustomProperties.TryGetValue(PlayerManager.HASH_ASSISTS, out a))
            {
                StatsUpdate(k, d, a);
            }
        }
    }
}