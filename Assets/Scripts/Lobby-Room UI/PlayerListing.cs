using Photon.Pun;
using Photon.Realtime;
using PM_FPS;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Task
{
    public class PlayerListing : MonoBehaviourPun, IPointerEnterHandler, IPointerExitHandler, IPunObservable, IPunOwnershipCallbacks
    {
        [SerializeField] private Image BG_Selector;
        [SerializeField] private Image FG_Selected;
        [SerializeField] private Image IMG_Avatar;
        [SerializeField] private TMP_Text _playerName;
        [HideInInspector] public int idx;

        public Texture AvatarTexture => IMG_Avatar.mainTexture;
        public bool IsSelected { get { return FG_Selected.enabled; } private set { FG_Selected.enabled = value; } }
        public string Name { get { return _playerName.text; } private set { _playerName.text = value; } }
        public bool IsOwned => photonView.IsMine && photonView.Owner != null; 
        // in case host, owner obj in photon view is never assigned but IsMine is true
        // thus we ensure even master client must request and become owner rightfully
        
        public void SelectAvatar()
        {
            IsSelected = true;
            Name = PhotonNetwork.NickName;
        }

        public void ClearSelection()
        {
            Name = "- none -";
            IsSelected = false;
        }

        public void OnClickAvatar() // need to have transferable ownership
        {
            if (IsSelected) return;
            
            if (IsOwned)
                PlayerMenu.Instance.OnClickAvatar(idx);
            else
                photonView.RequestOwnership();
        }

        #region Add Target CallBacks to Network
        // need to add this call back target obj into phtoton-network as it implements IPunOwnershipCallbacks
        public void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);
        }
        public void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }
        #endregion

        #region Interfaces

        public void OnPointerEnter(PointerEventData eventData)
        {
            BG_Selector.enabled = true;
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            BG_Selector.enabled = false;
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting) // owner is writing data to send to its receivers
            {
                stream.SendNext(IsSelected);
                stream.SendNext(Name);
                //Debug.Log("("+PhotonNetwork.LocalPlayer+", "+photonView.ViewID +") is writting to" + info.Sender + ", view id " + info.photonView.ViewID);
            }
            else if (stream.IsReading) // non owner is reading the data sent by the owner to sync up from his end
            {
                IsSelected = (bool)stream.ReceiveNext();
                Name = (string)stream.ReceiveNext();
                //Debug.Log("(" + PhotonNetwork.LocalPlayer + ", " + photonView.ViewID + ") is readding from " + info.Sender + ", view id " + info.photonView.ViewID);
            }
        }

        public void OnOwnershipRequest(PhotonView targetView, Player requestingPlayer)
        {            
            if (targetView != base.photonView) return;
            Debug.Log(requestingPlayer + " is requesting " + targetView);

            if (!IsSelected)
                photonView.TransferOwnership(requestingPlayer);
        }
        public void OnOwnershipTransfered(PhotonView targetView, Player previousOwner)
        {
            if (targetView != base.photonView) return;
            Debug.Log("authority has been given to " + targetView + " to " + targetView.Owner);

            OnClickAvatar();
        }
        public void OnOwnershipTransferFailed(PhotonView targetView, Player senderOfFailedRequest)
        {
            if (targetView != base.photonView) return;
        }

        #endregion

        public void CreatePlayerMaterial()
        {
            photonView.RPC(nameof(RPC_CreatePlayerMaterial), RpcTarget.AllBuffered);
        }

        [PunRPC] private void RPC_CreatePlayerMaterial()
        {
            var temp_material = new Material(PlayerMenu.Instance.MaterialRef);
            temp_material.SetTexture("_BaseMap", AvatarTexture);
            NetworkManager.singleton.RuntimePlayerMaterialAssets.Add(photonView.OwnerActorNr, temp_material);
        }
    }
}