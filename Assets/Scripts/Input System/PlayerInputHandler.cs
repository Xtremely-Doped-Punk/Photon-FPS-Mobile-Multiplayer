using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PM_FPS
{
    public class PlayerInputHandler : MonoBehaviour
    {
        // scale values to match up new input system to old input system
        //#if ENABLE_INPUT_SYSTEM
        public const float MOUSE_AXIS_SCALER = 10;

        string primaryBinding = "<Touchscreen>/delta";
        string secondaryBinding = "<Touchscreen>/touch1/delta";

        int Android_CamBindIndex;
        InputActionSetupExtensions.BindingSyntax Android_Cam_Binding;

        // singleton
        public static PlayerInputHandler Instance { get; private set; }
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
                Destroy(this.gameObject);
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
        public float MouseX_Axis => inp_Look.x / MOUSE_AXIS_SCALER;
        public float MouseY_Axis => inp_Look.y / MOUSE_AXIS_SCALER;
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


            // harcoded for now, later can be made dynamic in rebinding
            // for now, control scheme: "Android", unter Look action map,
            // binding "<Touchscreen>/delta" will be switched to 2ndary screen touch when primary touch is controlling on screen stick
            Android_CamBindIndex = PlayerInput.Look.GetBindingIndex(group: "Android", path: primaryBinding);
            Android_Cam_Binding = PlayerInput.Look.ChangeBinding(Android_CamBindIndex);
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
            // previously whenever on-screen stick was held, cam controls was disabled
            //_PlayerInput_.Look.performed -= HandleCamera; inp_Look = Vector2.zero;
            PlayerInput.Look.Disable();
            Android_Cam_Binding.WithPath(secondaryBinding);
            PlayerInput.Look.Enable();
            //_PlayerInput_.Look.performed += HandleCamera;
        }
        public void CamUI_ExitOverride()
        {
            //_PlayerInput_.Look.performed -= HandleCamera; inp_Look = Vector2.zero;
            PlayerInput.Look.Disable();
            Android_Cam_Binding.WithPath(primaryBinding);
            PlayerInput.Look.Enable();
            //_PlayerInput_.Look.performed += HandleCamera;
        }

        public void AutoSprint_TakeOverride(Vector2 dir)
        {
            PlayerInput.Move.performed -= HandleMovement;
            inp_Move = dir.normalized;
        }
        public void AutoSprint_GiveOverride()
        {
            PlayerInput.Move.performed += HandleMovement;
        }

        public void Reset()
        {
            inp_Move = Vector2.zero;
            inp_Look = Vector2.zero;
            Mouse_ScrollWheel = 0f;
            //Shift_BtnHold = LeftMouse_BtnHold = false;
        }
    }
}