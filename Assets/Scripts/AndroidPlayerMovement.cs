using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AndroidPlayerMovement : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    // need to do... ( not yet sync with player movement)
    bool isPressed;
    public void OnPointerDown(PointerEventData data)
    {
        isPressed = true;
    }
    public void OnPointerUp(PointerEventData data)
    {
        isPressed = false;
    }
}
