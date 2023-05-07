using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using ETouch = UnityEngine.InputSystem.EnhancedTouch; // enhansed' touch and unity engine' touch ambiguity

namespace PM_FPS
{
    public class AndroidMovementOverrider : MonoBehaviour
    {
        private PlayerInputHandler InputHandler => PlayerInputHandler.Instance;

        [SerializeField] private FloatingJoyStick floatingJoyStick;
        [SerializeField] private Transform sprintabilityUIParent;
        [SerializeField] private EventTrigger sprintLockEventTrigger;

        private bool SprintLockTriggered => InputHandler.SprintHold; 
        // assuming in android override controls we only have sprint lock as a option

        private RectTransform rectTransform;
        private ETouch.Finger activeFinger;
        private Vector2 moveAmount;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

            // reset anchors inorder to check pointer within rect
            if (rectTransform.anchorMin != Vector2.zero || rectTransform.anchorMax != Vector2.one)
                Debug.LogError("RectTransform Anchor not properly set for floating joystick area!");
            // make sure rectUI is make with achors set fully, and child anchors default center only

            //Debug.Log("RectTransform anchor: " + rectTransform.anchoredPosition);
            //Debug.Log("RectTransform sizeDelta: " + rectTransform.sizeDelta);
            //Debug.Log("RectTransform rect: " + rectTransform.rect);

            EventTrigger.Entry entry = new() { eventID = EventTriggerType.PointerEnter };
            entry.callback.AddListener((eventData) => SprintLock());
            sprintLockEventTrigger.triggers.Add(entry);

            StartCoroutine(ListenInputEvents());
        }

        private IEnumerator ListenInputEvents()
        {
            yield return new WaitUntil(()=> InputHandler != null);
            InputHandler.OnAutoSprintUnLocked += UpdateJoyStick_OnAutoSprintUnLocked;
        }

        private void UpdateJoyStick_OnAutoSprintUnLocked(object sender, EventArgs e)
        {
            SetupJoytStick();
            floatingJoyStick.ResetStickKnobAnchorPos();
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
            //if (sprintLockTriggered) // cancel sprint lock on any touch, which would be nice, just for testing
                //SprintUnlock();

            if (activeFinger != null) return;

            Vector2 localSpacePosition = transform.InverseTransformPoint(touchedFinger.screenPosition);
            //Debug.Log("Mouse Position: " + Input.mousePosition);
            //Debug.Log("Touch Screen Space Position: " + touchedFinger.screenPosition);
            //Debug.Log("Touch Local Space Position: " + localSpacePosition);
            //Debug.Log("Touch in Range of RectTransform: " + rectTransform.rect.Contains(localSpacePosition));
            //Debug.Log("Mouse in Range of RectTransform: " + RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition));

            if (rectTransform.rect.Contains(localSpacePosition))
            {
                SetupJoytStick(touchedFinger);
                //Debug.Log("Clamp Anchor Value = " + ClampJoyStickPosition(localSpacePosition, rectTransform.sizeDelta));
                floatingJoyStick.SetJoyStickAnchorPos(ClampJoyStickPosition(localSpacePosition, rectTransform.rect.size));
                Debug.Log("Touch in move area range");
                InputHandler.MoveUI_EnterOverride();
            }
        }

        private void HandleFingerMove(ETouch.Finger movedFinger)
        {
            if (SprintLockTriggered || activeFinger != movedFinger) return;

            float maxMove = floatingJoyStick.GetStickKnobRange();
            ETouch.Touch currentTouch = movedFinger.currentTouch;
            Vector2 localSpacePosition = transform.InverseTransformPoint(currentTouch.screenPosition);
            Vector2 dir = localSpacePosition - floatingJoyStick.GetJoyStickAnchorPos();
            Vector2 knobPosition;

            if (Vector2.SqrMagnitude(dir) > (maxMove * maxMove))
                knobPosition = dir.normalized * maxMove;
            else
                knobPosition = dir;
            
            floatingJoyStick.SetStickKnobAnchorPos(knobPosition);
            moveAmount = knobPosition / maxMove; // gives normalized movement compsite vector
            InputHandler.SetMoveInput(moveAmount);
        }

        private void HandleFingerUp(ETouch.Finger lostFinger)
        {
            if (activeFinger != lostFinger) return;
            
            if (SprintLockTriggered)
            {
                activeFinger = null;
                floatingJoyStick.ResetJoyStickAnchorPos();
                InputHandler.MoveUI_ExitOverride(reset: false);
            }
            else
            {
                SetupJoytStick();
                floatingJoyStick.ResetStickKnobAnchorPos();
                InputHandler.MoveUI_ExitOverride(reset: true);
            }
        }

        private void SetupJoytStick(ETouch.Finger activeFinger = null)
        {
            moveAmount = Vector2.zero;
            this.activeFinger = activeFinger;
            floatingJoyStick.gameObject.SetActive(activeFinger != null);
        }

        private Vector2 ClampJoyStickPosition(Vector2 position, Vector2 boundSize)
        {
            position += boundSize / 2; // eliminate negative vals for easilier understanding
            Vector2 joyStickSize = floatingJoyStick.GetJoyStickStickSize();
            Vector2 minLimits = joyStickSize / 2;
            Vector2 maxLimits = boundSize - minLimits;

            if (position.x < minLimits.x)
                position.x = minLimits.x;
            else if (position.x > maxLimits.x)
                position.x = maxLimits.x;

            if (position.y < minLimits.y)
                position.y = minLimits.y;
            else if (position.y > maxLimits.y)
                position.y = maxLimits.y;

            position -= boundSize / 2;
            return position;
        }

        private void Update()
        {
            if (InputHandler == null) return;
            var canSprint = InputHandler.moveDeltaMagnitude > 0.9f;
            if (canSprint && !sprintabilityUIParent.gameObject.activeSelf)
            {
                sprintabilityUIParent.gameObject.SetActive(true);
            }
            else if (!canSprint && sprintabilityUIParent.gameObject.activeSelf)
                sprintabilityUIParent.gameObject.SetActive(false);
        }

        // event trigger button
        private void SprintLock()
        {
            InputHandler.AutoSprint_TakeOverride(new Vector2(0, 1)); // set auto run in forward direction
        }

        private void SprintUnlock()
        {
            InputHandler.AutoSprint_GiveOverride(); // set auto run in forward direction
        }
    }
}