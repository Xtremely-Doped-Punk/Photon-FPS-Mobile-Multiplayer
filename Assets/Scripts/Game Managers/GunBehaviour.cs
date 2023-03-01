using Photon.Pun;
using TMPro;
using UnityEngine;
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
            if (!photonView.IsMine) return;
            _magUI.gameObject.SetActive(false);
            _reloadUI.gameObject.SetActive(false);
            Debug.Log(gameObject+"disabled");
        }

        private void OnEnable()
        {
            if (!photonView.IsMine) return;
            reloadCountdown = ConfigInfo.ReloadDelay;
            _magUI.gameObject.SetActive(true);
            _reloadUI.gameObject.SetActive(true);
            Debug.Log(gameObject+"enabled");
        }

        private void Update()
        {
            if (!photonView.IsMine) return;

            if (isReloaing)
            {
                reloadCountdown -= Time.deltaTime;
                // update reload ui
                _reloadUI.fillAmount = reloadCountdown / ConfigInfo.ReloadDelay;
            }
            else
            {
                if (ConfigInfo.IsSingleShot)
                {
                    if (Input.GetMouseButtonDown((int)UnityEngine.UIElements.MouseButton.LeftMouse))
                    {
                        Shoot();
                    }
                }
                else
                {
                    // fire rate and reload fn to be added
                    if (Input.GetMouseButton((int)UnityEngine.UIElements.MouseButton.LeftMouse))
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

                if (Input.GetKeyDown(KeyCode.R)) // manual reload
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
            _reloadUI.fillAmount = reloadCountdown = 0;
        }

        [PunRPC]
        void RPC_ShootImpact(Vector3 hitPosition, Vector3 hitNormal)
        {
            // creating a small radius collider on the impact position, to get other game objs that are nearby
            Collider[] colliders = Physics.OverlapSphere(hitPosition, 0.3f);

            if (colliders.Length != 0)
            {
                Debug.Log("Hit: " + colliders[0].gameObject.name + ", Count: " + colliders.Length);

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