using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Task
{
    [CreateAssetMenu(menuName ="Guns/New Gun Config")]
    public class GunConfigInfo : ScriptableObject
    {
        public const int MAX_DAMAGE = 125;
        public const int MAX_FIRE_RATE = 1000;
        public const int MAX_MOBILITY = 100;
        public const int MAX_CAPACITY = 100;
        public const int MAX_RELOAD_DELAY = 5;

        [field: SerializeField,Tooltip("Name of the Gun")]
        public string GunName { get; private set; } = null;

        [field: SerializeField, Range(1, MAX_DAMAGE), Tooltip("Damage dealed per bullet")] 
        public int Damage { get; private set; } = 5;

        
        [field: SerializeField, Range(1, MAX_FIRE_RATE), Tooltip("Rate of bullets fired per min without considering reload time")]
        public int FireRate { get; private set; } = 750; // set fire rate to 1 if its single shot gun

        
        [field: SerializeField, Range(1, MAX_MOBILITY), Tooltip("Mobility value affect player sprint speed inversely")]
        public int Mobility { get; private set; } = 100;

        
        [field: SerializeField, Range(1, MAX_CAPACITY), Tooltip("Mag capacity")]
        public int Capacity { get; private set; } = 30;

        [field: SerializeField, Range(0, MAX_RELOAD_DELAY), Tooltip("Mag capacity")]
        public float ReloadDelay { get; private set; } = 1.25f;

        public float MobilityMultiplier => (float)Mobility / MAX_MOBILITY;
        public bool IsSingleShot => FireRate == 1;

        public float FireRateDelay => 60f / FireRate;
    }
}