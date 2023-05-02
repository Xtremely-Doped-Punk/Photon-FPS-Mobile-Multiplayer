using Photon.Pun;
using PM_FPS;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Items
{
    public class BasicGunBehaviour : Item
    {
        public override float GetMobiltyMultiplier()
        {
            return ConfigInfo.MobilityMultiplier; // incase of additional decays
        }

        public override void Use()
        {
            Shoot();
        }

        public GunConfigInfo ConfigInfo { get { return (GunConfigInfo)_configInfo; } private set { _configInfo = value; } }

        [field: Header("Referneces Required")]
        [SerializeField] private MeshRenderer _BulletImpactPrefab;
        private TMP_Text _magUI => _controller.AmmoTxt;
        private Image _reloadUI => _controller.ReloadImg;

        [Header("Behaviour Configurations")]
        [SerializeField] private float _bulletImpactDestroyDelay = 7.5f;
        [SerializeField] private bool _autoReload = true;

        private int currMagCap;
        private bool isReloaing = false;
        private float fireRateTimer = 0;
        private float reloadCountdown;

        private void Start()
        {
            GunReloaded();
        }

        public override void OnDisable()
        {
            base.OnDisable();
            if (_returnCheck) return;
            _magUI.gameObject.SetActive(false);
            _reloadUI.gameObject.SetActive(false);
            //Debug.Log(gameObject+"disabled");
            CancelInvoke(); // cancel reload invoke fn called earlier
        }

        public override void OnEnable()
        {
            base.OnEnable();
            if (_returnCheck) return;
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
            if (_returnCheck) return;

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
                    var left_mouse_down = _InputHandler_.PlayerInput.Fire.WasPressedThisFrame();
                    /*if (_InpTest_)
                        Debug.Log("Left Mouse Btn Down = " + left_mouse_down);*/

                    if (left_mouse_down)
                    {
                        Use();
                    }
                }
                else
                {
                    //var left_mouse_hold = Input.GetMouseButton((int)UnityEngine.UIElements.MouseButton.LeftMouse);
                    var left_mouse_hold = _InputHandler_.PlayerInput.Fire.IsInProgress();
                    if (_InpTest_)
                        Debug.Log("Left Mouse Btn Hold = " + left_mouse_hold);

                    if (left_mouse_hold)
                    {
                        fireRateTimer -= Time.deltaTime;

                        // shoot will fire rate
                        if (fireRateTimer <= 0)
                        {
                            fireRateTimer = ConfigInfo.FireRateDelay;
                            Use();
                        }
                    }
                }

                //var r_down = Input.GetKeyDown(KeyCode.R);
                var r_down = _InputHandler_.PlayerInput.Reload.WasPressedThisFrame();
                /*if (_InpTest_)
                    Debug.Log("R Btn Down = " + r_down);*/

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