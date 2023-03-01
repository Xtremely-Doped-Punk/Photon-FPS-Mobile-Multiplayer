using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Task
{
    public class DisplayNameFacingCamera : MonoBehaviour
    {
        #region Display Player Name
        [SerializeField] PhotonView playerPV;
        [SerializeField] TMP_Text text;

        void Start()
        {
            if (playerPV.IsMine)
            {
                Destroy(gameObject);
                //gameObject.SetActive(false);
            }

            text.text = playerPV.Owner.NickName;

            cam = Camera.main;
        }
        #endregion

        #region Face Camera Update UI
        Camera cam;

        void Update()
        {
            if (cam == null)
            {
                if ((cam = Camera.main) == null)
                    cam = FindObjectsOfType<Camera>().Where(x => x.enabled).ToArray()[0];
            }
            // caz of URP camera script bug, we have only disabled the cam

            if (cam == null)
                return;

            transform.LookAt(cam.transform);
            transform.Rotate(Vector3.up * 180);
        }
        #endregion
    }
}