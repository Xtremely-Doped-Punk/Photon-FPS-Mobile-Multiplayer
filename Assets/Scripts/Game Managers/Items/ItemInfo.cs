using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Items
{
    [CreateAssetMenu(menuName = "Items/New Item Config")]
    public class ItemInfo : ScriptableObject
    {
        public const int MAX_MOBILITY = 100;

        [field: SerializeField, Tooltip("Name of the Item")]
        public string itemName { get; private set; } = null;

        [field: SerializeField, Range(1, MAX_MOBILITY), Tooltip("Mobility value affect player sprint speed inversely")]
        public int Mobility { get; private set; } = 100;

        public float MobilityMultiplier => (float)Mobility / MAX_MOBILITY;
    }
}