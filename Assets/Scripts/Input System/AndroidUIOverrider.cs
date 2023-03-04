using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;

namespace Task
{
    public class AndroidUIOverrider : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [field: SerializeField] public OnScreenStick joystick { get; private set; } = null;
        [SerializeField] private GameObject sprintabilityUI;
        public PlayerInputHandler InputHandler => PlayerInputHandler.Instance;


        // wont work exactly the wanted to...
        public void OnPointerDown(PointerEventData data)
        {
            InputHandler.Shift_BtnHold = false;
            InputHandler.AutoSprint_GiveOverride(); // tap the joystick again to stop auto sprint
            InputHandler.CamUI_EnterOverride();
            //Debug.Log("Joystick pointer down");
        }
        public void OnPointerUp(PointerEventData data)
        {
            InputHandler.CamUI_ExitOverride();
            //Debug.Log("Joystick pointer up");
        }

        private void Update()
        {
            if (InputHandler == null) return;
            var canSprint = InputHandler.moveDelta.magnitude > 0.9f;
            if (canSprint && !sprintabilityUI.activeSelf)
            {
                sprintabilityUI.SetActive(true);
            }
            else if (!canSprint && sprintabilityUI.activeSelf)
                sprintabilityUI.SetActive(false);
        }

        // event trigger buttonbutton
        public void SprintLock()
        {
            InputHandler.Shift_BtnHold = true;
            InputHandler.AutoSprint_TakeOverride(new Vector2(0, 1)); // set auto run in forward direction
            //Debug.Log("SprintLock pointer enter");
        }
    }
}