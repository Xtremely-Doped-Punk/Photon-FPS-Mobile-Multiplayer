using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PM_FPS
{
    public class FloatingJoyStick : MonoBehaviour
    {
        [SerializeField] private RectTransform joyStickRectTransform;
        [SerializeField] private RectTransform stickKnobRectTransform;
        [SerializeField] private Vector2 joyStickSize = Vector2.zero;
        [SerializeField] private float stickKnobRange = 100f;
        private Vector2 initialJoyStickAnchorPos;

        // Start is called before the first frame update
        void Awake()
        {
            if (joyStickRectTransform == null) joyStickRectTransform = GetComponent<RectTransform>();
            if (joyStickSize == Vector2.zero) joyStickSize = joyStickRectTransform.sizeDelta;

            initialJoyStickAnchorPos = joyStickRectTransform.anchoredPosition;
        }

        public void SetJoyStickAnchorPos(Vector2 anchorPos) => joyStickRectTransform.anchoredPosition = anchorPos;
        public void ResetJoyStickAnchorPos() => joyStickRectTransform.anchoredPosition = initialJoyStickAnchorPos;
        public Vector2 GetJoyStickAnchorPos() => joyStickRectTransform.anchoredPosition;

        public Vector2 GetJoyStickStickSize() => joyStickSize;
        public float GetStickKnobRange() => stickKnobRange;

        public void SetStickKnobAnchorPos(Vector2 anchorPos) => stickKnobRectTransform.anchoredPosition = anchorPos;
        public void ResetStickKnobAnchorPos() => stickKnobRectTransform.anchoredPosition = Vector2.zero;
    }
}