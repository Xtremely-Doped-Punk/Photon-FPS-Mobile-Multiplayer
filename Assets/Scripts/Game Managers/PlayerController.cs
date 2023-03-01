using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable; // as there is an ambiguity bet System.Collections.Hastable and Photon.Hastable

namespace Task
{
    public interface IDamageable 
    {
        // we are using this interface so that collider can easily
        // get generic component that implements this (refer GunBehaviour for why)
        void TakeDamage(int damageAmt); 
        // make sure to implement as a RPC fn inside it to update it on all clients as well
    }

    //[RequireComponent(typeof(Rigidbody))]
    public class PlayerController : NetworkObject, IDamageable
    {
        public const int MAX_PLAYER_HEALTH = 100;
        public const float NO_ITEM_EQUIPPED_MULTIPLIER = 1.125f;
        public const string HASH_GUN_INDEX = "EquippedGunIndex";

        private void Reset()
        {
            _Rigidbody = GetComponent<Rigidbody>();
        }

        [Header("Referneces Required")]
        [SerializeField] private Rigidbody _Rigidbody;
        [field: SerializeField] public Camera FPS_Camera { get; private set; } = null;
        [SerializeField] private PlayerGroundCheck _PlayerGroundCheck;
        [SerializeField] private Canvas _OverLay_UI_Canvas;
        [SerializeField] private Image _HP_BarReverse;
        [SerializeField] private Image _ui_HpDisp;
        [field: SerializeField] public TMP_Text AmmoTxt { get; private set; } = null;
        [field: SerializeField] public Image ReloadImg { get; private set; } = null;
        [SerializeField] private GunBehaviour[] _gunLoadout;

        [Header("Player Config")]
        [SerializeField] private int _currentHealth = MAX_PLAYER_HEALTH; // serialized for debug purposes
        [SerializeField, Range(0, 10)] private float _mouseSensitivity = 3.5f;
        [SerializeField, Range(100, 1000)] private float _jumpForce = 250;
        [SerializeField] private float _sprintBoostSpeed, _walkSpeed;
        [SerializeField, Range(0, 1)] private float _smoothTime = .15f;

        private float verticalLookRotation;
        private Vector3 smoothMoveVelocity;
        private Vector3 moveAmount;
        private int equippedGunIdx = -1;
        private float mobilityMultiplier = NO_ITEM_EQUIPPED_MULTIPLIER;
        [HideInInspector] public PlayerManager playerManager;

        private void Awake()
        {
            photonView.RPC(nameof(RPC_InstanciatePlayerMaterial), RpcTarget.AllBuffered);
        }


        [PunRPC] private void RPC_InstanciatePlayerMaterial()
        {
            // we are not editing material in prefab, we will edit in its instance again through a rpc callback on all client's end
            GetComponent<Renderer>().material = NetworkManager.singleton.RuntimePlayerMaterialAssets[photonView.OwnerActorNr];
        }

        private void Start()
        {
            /* information of other objects that habe instanciated this object can be found in photonView.InstantiationData as
            player maganager intializes this obj, we are searching for that obj's unique viewID in the set of gameObjs in scene */
            if (playerManager == null)
                playerManager = PhotonView.Find((int)photonView.InstantiationData[0]).GetComponent<PlayerManager>();

            if (photonView.IsMine)
            {
                FPS_Camera.gameObject.AddComponent<AudioListener>(); // revomed in prefab
                EquipGun(0); // equip gun initially
                if (Camera.main != null && Camera.main != FPS_Camera) Camera.main.enabled = false;


            }
            else
            {
                // we arent using rigidbody photon view to sync, we are using transform view only
                // so we can de-activate this component in non owned player to save cpu time
                Destroy(_Rigidbody);

                // destroy screen overlay canvas of of health bar of non players
                Destroy(_OverLay_UI_Canvas.gameObject);

                // destroy non-playable player's FPS cams (URP bug - cant destroy)
                FPS_Camera.enabled = false;
            }
        }

        #region Updates (per frame)
        private void Update()
        {
            if (!photonView.IsMine)
                return;

            CameraCtrlUpdate();
            MovementUpdate();
            TryJumpUpdate();
            SwitchGunsUpdate();

            if (transform.position.y < -10f) // Die if you fall out of the world
            {
                Dead();
            }
        }

        private void CameraCtrlUpdate()
        {
            // horizontal rotation (by player)
            transform.Rotate(Vector3.up * Input.GetAxisRaw("Mouse X") * _mouseSensitivity);

            // vertical rotation (by camera)
            verticalLookRotation += Input.GetAxisRaw("Mouse Y") * _mouseSensitivity;
            verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f); // clamp value bet top and bottom

            var eulerAng = Vector3.left * verticalLookRotation;
            FPS_Camera.transform.localEulerAngles = eulerAng;

            // update item view along with cam (no need as it a child of camera)
            /*
            if (equippedGunIdx!=-1)
            {
                _gunLoadout[equippedGunIdx].gameObject.transform.localEulerAngles = eulerAng;
            }
            */
        }

        private void MovementUpdate()
        {
            // horizontal axis & vertical axis both give value between [-1,1]; thus should be normalized to direction of movement as magnitude 1
            Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
            var speed = _walkSpeed + (Input.GetKey(KeyCode.LeftShift) ? _sprintBoostSpeed : 0);

            // gun equiping mobility comes into play here as a multipler factor
            speed *= mobilityMultiplier;

            moveAmount = Vector3.SmoothDamp(moveAmount, moveDir * speed, ref smoothMoveVelocity, _smoothTime); 
            // ref => call by reference parameter, i.e., smoothMoveVelocity is updated in SmoothDamp() itself
        }

        void TryJumpUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Space) && _PlayerGroundCheck.isGrounded)
            {
                _Rigidbody.AddForce(transform.up * _jumpForce);
            }
        }

        private void SwitchGunsUpdate()
        {
            // Number Key Inputs
            for (int i = 0; i < _gunLoadout.Length; i++)
            {
                if (Input.GetKeyDown((i + 1 +1).ToString())) // range of weapons [-1..] as -1 => equip none, thus +1
                {
                    EquipGun(i); // in keyboard starts from num '1' to ...
                    break;
                }
            }

            // Mouse Scroll Inputs
            if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f) // upward scroll (increment idx)
            {
                EquipGun(((equippedGunIdx +1 +1) % (_gunLoadout.Length + 1)) - 1);
            }
            else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f) // downward scroll (decrement idx)
            {
                EquipGun(((equippedGunIdx +1 + _gunLoadout.Length +1 -1) % (_gunLoadout.Length + 1)) - 1);
            }
        }

        void FixedUpdate() // update rigidbody physics here (to make it independent of fps, use fixed delta time step)
        {
            if (!photonView.IsMine)
                return;

            _Rigidbody.MovePosition(_Rigidbody.position + transform.TransformDirection(moveAmount) * Time.fixedDeltaTime);
        }
        #endregion

        #region Interfaces
        private List<Player> assistedPlayers = new();

        public void TakeDamage(int damageAmt) // interface fn needs to be in public
        {
            photonView.RPC(nameof(RPC_DealDamage), photonView.Owner, damageAmt, playerManager.photonView.ViewID); 
            // targeted rpc call => call rpc on specific player (who is the owner, here)
        }

        [PunRPC] private void RPC_DealDamage(int dmg, int senderPlayerManagerViewID, PhotonMessageInfo senderInfo)
        {
            _currentHealth = Mathf.Max(_currentHealth - dmg, 0);
            var fillAmt = (float)_currentHealth / MAX_PLAYER_HEALTH;
            _HP_BarReverse.fillAmount = 1 - fillAmt;

            photonView.RPC(nameof(RPC_UpdateUI_HealthBar), RpcTarget.Others, fillAmt);
            // ui hp-bar and username is inactive in local player's obj, thus only need to update them in other clients

            if (_currentHealth == 0)
            {
                // get opponent's Player Manager (two ways to get)
                //var opponentPM = FindObjectsOfType<PlayerManager>().SingleOrDefault(x => x.photonView.Owner == senderInfo.Sender);
                var opponentPM = PhotonView.Find(senderPlayerManagerViewID).GetComponent<PlayerManager>();

                // add kill count to last shot opponent
                opponentPM.GetKill();

                // add assists to all player who shot earlier
                opponentPM.GetAssist(assistedPlayers);

                // die
                Dead();
            }
            else
            {
                assistedPlayers.Add(senderInfo.Sender);
            }
        }

        [PunRPC] private void RPC_UpdateUI_HealthBar(float fillAmt)
        {
            _ui_HpDisp.fillAmount = fillAmt;
        }

        private void Dead()
        {
            playerManager.OnDie();
        }
        #endregion

        #region Gun Loadouts
        void EquipGun(int _index) // local player end update
        {
            if (_index == equippedGunIdx)
                return;

            // _index = -1, then all guns will be disabled, maybe can add more sprint/walk speed
            if (equippedGunIdx != -1)
            {
                _gunLoadout[equippedGunIdx].gameObject.SetActive(false);
                equippedGunIdx = -1; mobilityMultiplier = NO_ITEM_EQUIPPED_MULTIPLIER;
            }
            if (_index != -1)
            {
                _gunLoadout[_index].gameObject.SetActive(true);
                equippedGunIdx = _index; mobilityMultiplier = _gunLoadout[_index].ConfigInfo.MobilityMultiplier;
            }

            if (photonView.IsMine) // owner if this player, must broadcast his equiped gun, so that it can synced from other player's end
            {
                // using photon's networked hash table we can share the required information as custom properties
                // acts like a dictionary <string: property name, byte array, i.e. object> (that need to type casted accordingly)
                Hashtable hash = PhotonNetwork.LocalPlayer.CustomProperties;
                if (hash.ContainsKey(HASH_GUN_INDEX))
                    hash[HASH_GUN_INDEX] = equippedGunIdx;
                else
                    hash.Add(HASH_GUN_INDEX, equippedGunIdx);
                PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
            }
        }

        // this call back will be called on all rpc client objs whenever the player updates his CustomProperties
        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) // rpc player update
        {
            // targeted updates on player instances that are not owned by local player
            if (!photonView.IsMine && targetPlayer == photonView.Owner && changedProps.TryGetValue(HASH_GUN_INDEX,out var index))
            {
                EquipGun((int)index);
            }
        }
        #endregion

        #region CursorLock
        public override void OnEnable()
        {
            base.OnEnable();
            Cursor.lockState = CursorLockMode.Locked;
        }
        public override void OnDisable()
        {
            base.OnDisable();
            Cursor.lockState = CursorLockMode.Confined;
        }
        #endregion
    }
}