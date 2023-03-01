using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Task
{

    [CreateAssetMenu(menuName="Singletons/GameSettings")]
    public class GameSettings : ScriptableObject
    {
        public enum PlayerCharacter { player1, player2, player3, player4 };

        [SerializeField] private string _gameVersion = "0.0.0"; 
        [SerializeField] private string _userName = null; 

        private static GameSettings _instance = null;
        public static GameSettings Instance 
        { 
            get 
            { 
                if (_instance == null)
                {
                    var instances = Resources.FindObjectsOfTypeAll<GameSettings>();
                    if (instances.Length == 0) 
                    {
                        Debug.LogError("No Game-Settings File Found!");
                        return null;
                    }
                    else if (instances.Length > 1) 
                    {
                        Debug.LogError("Multiple Game-Settings File Found!");
                        return null;
                    }
                    else
                    {
                        _instance = instances[0];
                    }
                }
                return _instance; 
            } 
        }

        public string GameVersion => _gameVersion;
        public string UserName { get { return _userName; } set { _userName = value; } }
    }
}