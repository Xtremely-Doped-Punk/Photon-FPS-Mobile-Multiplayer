using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Task
{
    public class GunBehaviour : MonoBehaviourPun
    {
        [field: Header("Referneces Required")]
        [field: SerializeField] public GunConfigInfo ConfigInfo { get; private set; } = null;
        [SerializeField] private GameObject _modelGameObject;
        [SerializeField] private MeshRenderer _BulletImpactPrefab;
        [SerializeField] private PlayerController _controller;
        private Camera _playerCam => _controller.FPS_Camera;
        private TMP_Text _magUI => _controller.AmmoTxt;

        private Image _reloadUI => _controller.ReloadImg;
        private PlayerInputHandler _InputHandler_ => _controller._InputHandler_;
        private bool _InpTest_ => _controller._InpTest_;

        [Header("Behaviour Configurations")]
        [SerializeField] private float _bulletImpactDestroyDelay = 7.5f;
        [SerializeField] private bool _autoReload = true;

        private int currMagCap;
        private bool isReloaing = false;
        private float fireRateTimer = 0;
        private float reloadCountdown;


        private void Reset()
        {
            _controller = GetComponentInParent<PlayerController>();
        }
        private void Awake()
        {
            // assuming we will have the main cam set to player's first person view
            //playerCam = Camera.main;

            if (_controller == null)
                Reset();
        }

        private void Start()
        {
            GunReloaded();
        }

        private void OnDisable()
        {
            if (!_InpTest_ && !photonView.IsMine) return;
            _magUI.gameObject.SetActive(false);
            _reloadUI.gameObject.SetActive(false);
            //Debug.Log(gameObject+"disabled");
            CancelInvoke(); // cancel reload invoke fn called earlier
        }

        private void OnEnable()
        {
            if (!_InpTest_ && !photonView.IsMine) return;
            if (isReloaing)
            {
                if (!_autoReload || currMagCap != 0)
                {
                    isReloaing = false; // cancels out auto reload when weapons are switched
                    reloadCountdown = 0; ReloadUI_Update();
                }
                else
                    GunReloading();
            }
            _magUI.gameObject.SetActive(true); MagUI_Update();
            _reloadUI.gameObject.SetActive(true); ReloadUI_Update();
            //Debug.Log(gameObject+"enabled");
        }

        private void Update()
        {
            if (!_InpTest_ && !photonView.IsMine) return;

            if (isReloaing)
            {
                reloadCountdown -= Time.deltaTime;
                // update reload ui
                ReloadUI_Update();
            }
            else
            {
                if (ConfigInfo.IsSingleShot)
                {
                    //var left_mouse_down = Input.GetMouseButtonDown((int)UnityEngine.UIElements.MouseButton.LeftMouse);
                    var left_mouse_down = _InputHandler_.LeftMouseBtn_BtnDown;
                    if (_InpTest_)
                        Debug.Log("Left Mouse Btn Down = " + left_mouse_down);

                    if (left_mouse_down)
                    {
                        Shoot();
                    }
                }
                else
                {
                    //var left_mouse_hold = Input.GetMouseButton((int)UnityEngine.UIElements.MouseButton.LeftMouse);
                    var left_mouse_hold = _InputHandler_.LeftMouse_BtnHold;
                    if (_InpTest_)
                        Debug.Log("Left Mouse Btn Hold = " + left_mouse_hold);

                    if (left_mouse_hold)
                    {
                        fireRateTimer -= Time.deltaTime;

                        // shoot will fire rate
                        if (fireRateTimer <= 0)
                        {
                            fireRateTimer = ConfigInfo.FireRateDelay;
                            Shoot();
                        }
                    }
                }

                //var r_down = Input.GetKeyDown(KeyCode.R);
                var r_down = _InputHandler_.R_BtnDown;
                if (_InpTest_)
                    Debug.Log("R Btn Down = " + r_down);

                if (r_down && currMagCap != ConfigInfo.Capacity) // manual reload (wont reload if at full mag)
                {
                    GunReloading();
                }
            }
        }

        private void Shoot()
        {
            if (currMagCap == 0) return;

            currMagCap--;
            MagUI_Update();

            Ray ray = _playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f)); // taking the centre point of screen as the aim

            ray.origin = _playerCam.transform.position; // source of ray casted is camera position of local player

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // if the ray casted hits any damagable obj, then deal dmg
                hit.collider.gameObject.GetComponent<IDamageable>()?.TakeDamage(ConfigInfo.Damage);
                // using a common interface for any object that is damagable,
                // makes the whole concept of shooting, damage dealing generalized

                photonView.RPC(nameof(RPC_ShootImpact), RpcTarget.All, hit.point, hit.normal);
            }

            if (currMagCap == 0 && _autoReload)
            {
                GunReloading();
            }
        }

        private void MagUI_Update()
        {
            _magUI.text = currMagCap.ToString() + "/" + ConfigInfo.Capacity;
        }
        private void ReloadUI_Update()
        {
            _reloadUI.fillAmount = reloadCountdown / ConfigInfo.ReloadDelay;
        }

        private void GunReloading()
        {
            isReloaing = true; // turn on reloading ui also
            Invoke(nameof(GunReloaded), ConfigInfo.ReloadDelay);
            _reloadUI.fillAmount = 1; reloadCountdown = ConfigInfo.ReloadDelay;
        }

        private void GunReloaded()
        {
            isReloaing = false; // turn off reloading ui also
            currMagCap = ConfigInfo.Capacity;
            MagUI_Update();
            reloadCountdown = 0;
            ReloadUI_Update();
        }

        [PunRPC]
        void RPC_ShootImpact(Vector3 hitPosition, Vector3 hitNormal)
        {
            // creating a small radius collider on the impact position, to get other game objs that are nearby
            Collider[] colliders = Physics.OverlapSphere(hitPosition, 0.3f);

            if (colliders.Length != 0)
            {
                //Debug.Log("Hit: " + colliders[0].gameObject.name + ", Count: " + colliders.Length);
                MeshRenderer bulletImpact =
                    Instantiate(
                        _BulletImpactPrefab, hitPosition + hitNormal * 0.001f,
                        Quaternion.LookRotation(hitNormal, Vector3.up) * _BulletImpactPrefab.transform.rotation
                        );

                bulletImpact.material = RandomizeImpact.GetRandomMaterial();

                Destroy(bulletImpact, _bulletImpactDestroyDelay);
                bulletImpact.transform.SetParent(colliders[0].transform);
            }
        }
    }
}