using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using PM_FPS;
using UnityEngine.InputSystem.XR;

namespace Items
{
    public abstract class Item : MonoBehaviourPunCallbacks
    {
        [SerializeField] protected GameObject _itemModelObj;
        [SerializeField] protected ItemInfo _configInfo;

        [SerializeField] protected PlayerController _controller;
        protected Camera _playerCam => _controller.FPS_Camera;

        protected PlayerInputHandler _InputHandler_ => _controller._InputHandler_;
        protected bool _InpTest_ => _controller._InpTest_;

        protected bool _returnCheck => !_InpTest_ && !photonView.IsMine;

        public abstract void Use();
        public virtual float GetMobiltyMultiplier()
        {
            return _configInfo.MobilityMultiplier;
        }

        protected virtual void Reset()
        {
            _controller = GetComponentInParent<PlayerController>();
        }
        protected virtual void Awake()
        {
            // assuming we will have the main cam set to player's first person view
            //playerCam = Camera.main;

            if (_controller == null)
                Reset();
        }
    }
}