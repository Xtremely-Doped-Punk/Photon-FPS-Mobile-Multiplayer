using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PM_FPS
{
    public class PlayerInputHandler : MonoBehaviour
    {
        private const string HORIZONTAL_SENSTIVITY = "HorizontalSenstivity";
        private const string VERTICAL_SENSTIVITY = "VericalSenstivity";
        public const float SENS_MIN = 1, SENS_MAX = 100;

        public event EventHandler OnAutoSprintUnLocked;

        // singleton
        public static PlayerInputHandler Instance { get; private set; }

        private const float _lookSenstivityMultiplier = 0.01f;
        [SerializeField, Range(SENS_MIN, SENS_MAX)] private float lookSenstivityX = 50f;
        [SerializeField, Range(SENS_MIN, SENS_MAX)] private float lookSenstivityY = 50f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
                Destroy(this.gameObject);

            lookSenstivityX = PlayerPrefs.GetFloat(HORIZONTAL_SENSTIVITY, lookSenstivityX);
            lookSenstivityY = PlayerPrefs.GetFloat(VERTICAL_SENSTIVITY, lookSenstivityY);
        }

        /*
            Old Input System to New Input System Mapping
        
        Camera Controls: Axis => { "Mouse X" , "Mouse Y" }
        Movement Controls: Axis => { "Horizontal" , "Vertical" }
        Sprint: BtnHold => { LeftShift }
        Jump Controls: BtnDown => { Space }
        Switch Weapons: Axis => { "Mouse ScrollWheel" }; BtnDown => { number keys resp }
        Cursor Mode: BtnDown => { Esc }
        Fire: BtnDown, BtnHold => { LeftMouseBtn }
        Reload: BtnDown => { R }
        Score Board View: BtnDown, BtnUp => { Tab }

        ---------------------------------------------------------------------

            New Input System Mapping Cross Platform

        Camera Controls: Right Stick
        Movement Controls: Left Stick
        Jump Controls: Button West
        Switch Weapons: D-Pad Up, D-Pad Down
        Fire: D-Pad Left, D-Pad Right
        Reload: Button East
        Score Board View:

         */

        private GameInputActionMap _InputActions_;

        // Per frame updates can be directly accesed from input handler, using:
        // WasPressedThisFrame(); .WasReleasedThisFrame(); IsInProgress()
        public GameInputActionMap.PlayerControllerActions PlayerInput { get; private set; }

        // public assesables (note BtnDown, BtnUp) => they need to be implemented as events
        // or should be resetted every frame after their use is invoked (thus here they are public set)
        public float MouseX_Axis => lookSenstivityX * _lookSenstivityMultiplier * inp_Look.x;
        public float MouseY_Axis => lookSenstivityY * _lookSenstivityMultiplier * inp_Look.y;
        public float Horizontal_Axis => inp_Move.x;
        public float Vertical_Axis => inp_Move.y;

        [field: SerializeField] public float Mouse_ScrollWheel { get; private set; } = 0f;
        [field: SerializeField] public bool SprintHold { get; set; } = false; // android custom ui

        /*
        [field: SerializeField] public bool LeftMouse_BtnHold { get; private set; } = false;

        // Per frame updates can be directly accesed from input handler
        public bool Space_BtnDown => PlayerInput.Jump.WasPerformedThisFrame();
        public bool LeftMouseBtn_BtnDown => PlayerInput.Fire.WasPressedThisFrame();
        public bool R_BtnDown => PlayerInput.Reload.WasPressedThisFrame();
        public bool WeaponNext => PlayerInput.WeaponSwitchNext.WasPressedThisFrame();
        public bool WeaponPrev => PlayerInput.WeaponSwitchPrev.WasPressedThisFrame();
        public bool Tab_BtnDown => PlayerInput.ScoreBoard.WasPressedThisFrame();
        public bool Tab_BtnUp => PlayerInput.ScoreBoard.WasReleasedThisFrame();
        */

        // private calc, serialized just for reference/debugging
        [SerializeField] private Vector2 inp_Move; public float moveDeltaMagnitude => inp_Move.magnitude;
        [SerializeField] private Vector2 inp_Look;

        public void SetMoveInput(Vector2 val)
        {
            if (PlayerInput.Move.enabled)
            {
                MoveUI_EnterOverride(); // sometimes touch enter not triggered as it might consider it as multiple count
                //Debug.LogError("PlayerInput:Move from InputActions is not disabled, pls disable it before setting input values directly!");
            }
            inp_Move = val;
        }
        public void SetLookInput(Vector2 val)
        {
            if (PlayerInput.Look.enabled)
            {
                CamUI_EnterOverride(); // sometimes touch enter not triggered as it might consider it as multiple count
                //Debug.LogError("PlayerInput:Look from InputActions is not disabled, pls disable it before setting input values directly!");
            }
                inp_Look = val;
        }

        // Work around for button hold technique
        //public bool Shift_BtnHold => actions_Shift.Any(x => x.IsPressed());
        //[SerializeField] InputAction[] actions_Shift = _PlayerInput_.Sprint.actionMap.actions.ToArray(); // initialize on enable

        // Another work around for reset after reference
        //[SerializeField] private bool inp_Jump = false;
        //public bool Space_BtnDown { get { var temp = inp_Jump; inp_Jump = false; return temp; } } // if accessed, it resets


        private void OnEnable()
        {
            if (_InputActions_ == null)
                _InputActions_ = new GameInputActionMap();

            PlayerInput = _InputActions_.PlayerController;

            /*
            foreach (var bindings in _PlayerInput_.Look.bindings)
            {
                Debug.Log(bindings);
            }*/

            // declare lamda fns to save locally
            PlayerInput.Move.performed += HandleMovement;
            PlayerInput.Look.performed += HandleCamera;

            PlayerInput.WeaponScroll.performed += InpActCB => Mouse_ScrollWheel = InpActCB.ReadValue<float>();

            PlayerInput.Sprint.performed += InpActCB => { SprintHold = InpActCB.action.IsPressed(); };
            PlayerInput.Sprint.canceled += InpActCB => { SprintHold = InpActCB.action.IsPressed(); };

            /*
            PlayerInput.Fire.performed += InpActCB => { LeftMouse_BtnHold = InpActCB.action.IsPressed(); };
            PlayerInput.Fire.canceled += InpActCB => { LeftMouse_BtnHold = InpActCB.action.IsPressed(); };

            _PlayerInput_.Jump.performed += InpActCB => { Space_BtnDown = InpActCB.action.IsPressed(); }; 
            _PlayerInput_.Reload.performed += InpActCB => { R_BtnDown = InpActCB.action.IsPressed(); };
            _PlayerInput_.WeaponSwitchNext.performed += InpActCB => { WeaponNext = InpActCB.action.IsPressed(); };
            _PlayerInput_.WeaponSwitchPrev.performed += InpActCB => { WeaponPrev = InpActCB.action.IsPressed(); };
            */
            // in late update, need to reset the per frame accesors

            _InputActions_.Enable();
            PlayerInput.Enable();
        }

        private void HandleMovement(InputAction.CallbackContext InpActCB)
        {
            inp_Move = InpActCB.ReadValue<Vector2>();
        }

        private void HandleCamera(InputAction.CallbackContext InpActCB)
        {
            inp_Look = InpActCB.ReadValue<Vector2>();
        }

        private void OnDisable()
        {
            if (_InputActions_ == null) return;
            _InputActions_.Disable();
            PlayerInput.Disable();
        }

        /* better way found
        private void LateUpdate()
        {
            // reset all one frame only inputs here
            //Space_BtnDown = LeftMouseBtn_BtnDown = R_BtnDown = WeaponNext = WeaponPrev = false;
        }*/

        public void CamUI_EnterOverride()
        {
            PlayerInput.Look.Disable();
            inp_Look = Vector2.zero;
        }
        public void CamUI_ExitOverride()
        {
            inp_Look = Vector2.zero;
            PlayerInput.Look.Enable();
        }

        public void MoveUI_EnterOverride()
        {
            PlayerInput.Move.Disable();
            inp_Move = Vector2.zero;
            AutoSprint_GiveOverride();
        }

        public void MoveUI_ExitOverride(bool reset = true)
        {
            if (reset)
                inp_Move = Vector2.zero;
            
            PlayerInput.Move.Enable();
        }

        private bool failsafeCancelAutoRunAdded = false;
        public void AutoSprint_TakeOverride(Vector2 dir)
        {
            if (!failsafeCancelAutoRunAdded)
            {
                PlayerInput.Move.performed += CancelAutoSprintOnMovePerformed;
                failsafeCancelAutoRunAdded = true;
            }
            // give override when move is enabled

            inp_Move = dir.normalized;
            SprintHold = true;
            Debug.Log("Sprint Locked");
        }

        private void CancelAutoSprintOnMovePerformed(InputAction.CallbackContext obj)
        {
            AutoSprint_GiveOverride();
            // this event subscription should be made to trigger once per call

            OnAutoSprintUnLocked?.Invoke(this, EventArgs.Empty);
        }

        public void AutoSprint_GiveOverride()
        {
            if (failsafeCancelAutoRunAdded)
            {
                PlayerInput.Move.performed -= CancelAutoSprintOnMovePerformed;
                failsafeCancelAutoRunAdded = false;
            }

            if (SprintHold)
            {
                SprintHold = false;
                Debug.Log("Sprint Unlocked");
            }
        }

        public void Reset()
        {
            inp_Move = Vector2.zero;
            inp_Look = Vector2.zero;
            Mouse_ScrollWheel = 0f;
            //Shift_BtnHold = LeftMouse_BtnHold = false;
        }
        public float GetLookSenstivityX() => lookSenstivityX;
        public float GetLookSenstivityY() => lookSenstivityY;
        public void SetLookSenstivityX(float val)
        {
            lookSenstivityX = val;
            PlayerPrefs.SetFloat(HORIZONTAL_SENSTIVITY, lookSenstivityX);
        }
        public void SetLookSenstivityY(float val)
        {
            lookSenstivityY = val;
            PlayerPrefs.SetFloat(VERTICAL_SENSTIVITY, lookSenstivityY);
        }
    }
}