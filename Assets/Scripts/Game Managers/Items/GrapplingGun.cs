using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using Task;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Items
{
    public class GrapplingGun : Item
    {
        private const string HASH_IS_GRAPPLING = "IsGrappling";
        private const string HASH_GRAPPLE_POINT = "GrapplePoint";

        public override void Use()
        {
            StartGrapple();
        }


        [Header("Grapple Gun Config")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private LayerMask grapplableLayers;
        [SerializeField] private Transform gunTip;
        [SerializeField] private float maxDistance = 20f;

        [Header("Sprint Joint Config")]
        [SerializeField, Range(0, 1)] private float jointMaxDistMultipler = .8f;
        [SerializeField, Range(0, 1)] private float jointMinDistMultipler = .2f;
        [SerializeField] private float jointSpring = 4.5f;
        [SerializeField] private float jointDamper = 7f;
        [SerializeField] private float jointMassScale = 3.5f;
        [SerializeField] private float gunRotationSpeed = 5f;

        private Vector3 _grapplePoint; public Vector3 GrapplingPoint => _grapplePoint;
        private SpringJoint _joint;
        private bool _isGrappling; public bool IsGrappling => _isGrappling;
        private Quaternion itemInitialRotation;

        protected override void Reset()
        {
            base.Reset();
            if (lineRenderer==null) lineRenderer = GetComponent<LineRenderer>();
        }

        private void Start()
        {
            itemInitialRotation = _itemModelObj.transform.localRotation;
        }

        void Update()
        {
            if (_returnCheck) return;

            if (_InputHandler_.PlayerInput.Fire.WasPressedThisFrame())
                Use();
            else if (_InputHandler_.PlayerInput.Fire.IsInProgress())
            {
                //Debug.Log("Grappling Progress");
                if (!_isGrappling) return; 
                // sometimes "IsInProgress" or btn hold option returns true for the fast frame
                // after button was actually released..., thus ensure using a fail safe

                _joint.damper -= Time.deltaTime; // reduce damping to pull up faster over time
            }
            else if (_InputHandler_.PlayerInput.Fire.WasReleasedThisFrame())
                StopGrapple();

            if (_isGrappling)
            {
                RotateGun();
            }
        }

        private void LateUpdate()
        {
            DrawRope();
        }
        public override void OnDisable()
        {
            base.OnDisable();
            if (_returnCheck) return;
            StopGrapple();
            CancelInvoke();
        }
        public override void OnEnable()
        {
            base.OnEnable();
            if (_returnCheck) return;
        }

        private void StartGrapple()
        {
            if (Physics.Raycast(_playerCam.transform.position, 
                _playerCam.transform.forward, out RaycastHit hit, maxDistance, grapplableLayers))
            {
                _grapplePoint = hit.point;
                _isGrappling = true;
                SaveGunNetworkConfig();

                // add physics components for swinging in player
                _joint = _controller.gameObject.AddComponent<SpringJoint>();
                _joint.autoConfigureConnectedAnchor = false;
                _joint.connectedAnchor = _grapplePoint;

                // distance to maintain between grapple point and player
                _joint.maxDistance = hit.distance * jointMaxDistMultipler;
                _joint.minDistance = hit.distance * jointMinDistMultipler;

                // set up joint config
                _joint.spring = jointSpring;
                _joint.damper = jointDamper;
                _joint.maxDistance = jointMassScale;

                // liner renderer
                lineRenderer.positionCount = 2;
            }
        }

        private void SaveGunNetworkConfig()
        {
            if (!_InpTest_ && photonView.IsMine)
            {
                Hashtable hash = PhotonNetwork.LocalPlayer.CustomProperties;
                if (hash.ContainsKey(HASH_IS_GRAPPLING))
                    hash[HASH_IS_GRAPPLING] = _isGrappling;
                else
                    hash.Add(HASH_IS_GRAPPLING, _isGrappling);

                if (hash.ContainsKey(HASH_GRAPPLE_POINT))
                    hash[HASH_GRAPPLE_POINT] = _grapplePoint;
                else
                    hash.Add(HASH_GRAPPLE_POINT, _grapplePoint);
                PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
            }
        }

        private void StopGrapple()
        {
            // remove joint
            Destroy(_joint);
            _isGrappling = false;
            SaveGunNetworkConfig();

            // line renderer
            lineRenderer.positionCount = 0;

            // reset rotation
            _itemModelObj.transform.localRotation = itemInitialRotation;
        }

        private void DrawRope()
        {
            // dont draw if there is no joint
            if(!_isGrappling) return;

            lineRenderer.SetPosition(0, gunTip.position);
            lineRenderer.SetPosition(1, _grapplePoint);
        }

        private void RotateGun()
        {
            //_itemModelObj.transform.LookAt(GrapplingPoint); // direct look at

            // lerp look at
            var targetRot = Quaternion.LookRotation(GrapplingPoint - _itemModelObj.transform.position);
            _itemModelObj.transform.rotation = Quaternion.Lerp(_itemModelObj.transform.rotation,
                targetRot, Time.deltaTime * gunRotationSpeed);
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) // rpc player update
        {
            // targeted updates on player instances that are not owned by local player
            if (!photonView.IsMine && targetPlayer == photonView.Owner)
            {
                if (changedProps.TryGetValue(HASH_IS_GRAPPLING, out var isGrappling))
                    _isGrappling = (bool)isGrappling;
                if (changedProps.TryGetValue(HASH_GRAPPLE_POINT, out var grapplePoint))
                    _grapplePoint = (Vector3)grapplePoint;

                if (_isGrappling)
                    lineRenderer.positionCount = 2;
                else
                    lineRenderer.positionCount = 0;
            }
        }
    }
}