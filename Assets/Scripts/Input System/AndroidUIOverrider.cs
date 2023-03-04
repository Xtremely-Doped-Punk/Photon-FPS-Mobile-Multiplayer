using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Task
{
    public class AndroidUIOverrider : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        // wont work exactly the wanted to...
        bool isPressed;
        public void OnPointerDown(PointerEventData data)
        {
            PlayerInputHandler.Instance.CamUI_EnterOverride();
        }
        public void OnPointerUp(PointerEventData data)
        {
            PlayerInputHandler.Instance.CamUI_ExitOverride();
        }
    }
}