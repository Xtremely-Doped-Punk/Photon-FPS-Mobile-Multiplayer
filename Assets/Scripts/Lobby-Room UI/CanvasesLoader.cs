using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Task
{
    public class CanvasesLoader : MonoBehaviour
    {
        [SerializeField] Canvas[] _canvasList = new Canvas[0];
        private int _idx = 0;

        private void Awake()
        {
            foreach (Canvas canvasObj in _canvasList) 
            {
                canvasObj.gameObject.SetActive(false);
            }
            _canvasList[_idx].gameObject.SetActive(true);
        }

        public void ActivateNext()
        {
            _canvasList[_idx].gameObject.SetActive(false);
            _idx = (_idx + 1) % _canvasList.Length;
            _canvasList[_idx].gameObject.SetActive(true);
        }
        public void ActivatePrev()
        {
            _canvasList[_idx].gameObject.SetActive(false);
            _idx = (_idx - 1) % _canvasList.Length;
            _canvasList[_idx].gameObject.SetActive(true);
        }
    }
}