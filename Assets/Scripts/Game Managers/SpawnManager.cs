using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Task
{
    public class SpawnManager : MonoBehaviour
    {
        private static SpawnManager Instance;
        [System.Serializable] public enum SpawnType{  Random, RoundRobin }

        [SerializeField] private SpawnType spawnMode = SpawnType.Random;

        private Transform[] spawnPoints;
        int prevSpawnPoint = -1;

        private void Awake()
        {
            Instance = this;

            // remove the parent transform, can be done in 2 ways
            // -> skip 1st one which is obviously parent transform
            // -> search this obj's transform and not include it
            spawnPoints = GetComponentsInChildren<Transform>().Skip(1).ToArray(); //.Where(t => t != transform).ToArray();
        }

        public static Transform GetSpawnPoint()
        {
            return Instance.GetSpawnPointTransform();
        }

        private Transform GetSpawnPointTransform()
        {
            int idx;
            if (spawnMode == SpawnType.Random)
            {
                while ((idx = Random.Range(0, spawnPoints.Length)) == prevSpawnPoint) { }
            }
            else
            {
                idx = (prevSpawnPoint + 1) % spawnPoints.Length;
            }

            prevSpawnPoint = idx;
            return spawnPoints[idx];
        }
    }
}