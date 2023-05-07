using UnityEngine;
using ETouch = UnityEngine.InputSystem.EnhancedTouch; // enhansed' touch and unity engine' touch ambiguity

using UnityEngine.InputSystem;

namespace PM_FPS
{
    public class AndroidCamLookOverrider : MonoBehaviour
    {
        private PlayerInputHandler InputHandler => PlayerInputHandler.Instance;

        private RectTransform rectTransform;
        private ETouch.Finger activeFinger;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

            // reset anchors inorder to check pointer within rect
            if (rectTransform.anchorMin != Vector2.zero || rectTransform.anchorMax != Vector2.one)
                Debug.LogError("RectTransform Anchor not properly set for floating joystick area!");
        }

        private void OnEnable()
        {
            ETouch.EnhancedTouchSupport.Enable();
            ETouch.Touch.onFingerDown += HandleFingerDown;
            ETouch.Touch.onFingerMove += HandleFingerMove;
            ETouch.Touch.onFingerUp += HandleFingerUp;
        }

        private void OnDisable()
        {
            ETouch.Touch.onFingerDown -= HandleFingerDown;
            ETouch.Touch.onFingerMove -= HandleFingerMove;
            ETouch.Touch.onFingerUp -= HandleFingerUp;
            ETouch.EnhancedTouchSupport.Disable();
        }

        private void HandleFingerDown(ETouch.Finger touchedFinger)
        {
            if (activeFinger != null) return;

            Vector2 localSpacePosition = transform.InverseTransformPoint(touchedFinger.screenPosition);

            if (rectTransform.rect.Contains(localSpacePosition))
            {
                activeFinger = touchedFinger;
                InputHandler.CamUI_EnterOverride();
            }
        }

        private void HandleFingerMove(ETouch.Finger movedFinger)
        {
            if (activeFinger != movedFinger) return;

            InputHandler.SetLookInput(activeFinger.currentTouch.screen.delta.value);
        }

        private void HandleFingerUp(ETouch.Finger lostFinger)
        {
            if (activeFinger != lostFinger) return;
            
            activeFinger = null;
            InputHandler.CamUI_ExitOverride();
        }
    }
}