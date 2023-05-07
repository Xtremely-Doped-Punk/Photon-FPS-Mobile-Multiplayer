using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PM_FPS
{
    public class InputSenstivityUI : MonoBehaviour
    {
        [SerializeField] private Slider horizontalSenstivity;
        [SerializeField] private TextMeshProUGUI horizontalSliderHandleText;
        [SerializeField] private Slider verticalSenstivity;
        [SerializeField] private TextMeshProUGUI verticalSliderHandleText;

        private float precision = 1;

        private void Start()
        {
            verticalSenstivity.minValue = horizontalSenstivity.minValue = PlayerInputHandler.SENS_MIN;
            verticalSenstivity.maxValue = horizontalSenstivity.maxValue = PlayerInputHandler.SENS_MAX;
            StartCoroutine(InitializeSliderControls());
        }

        private IEnumerator InitializeSliderControls()
        {
            yield return new WaitUntil(() => PlayerInputHandler.Instance != null);

            horizontalSenstivity.value = PlayerInputHandler.Instance.GetLookSenstivityX();
            verticalSenstivity.value = PlayerInputHandler.Instance.GetLookSenstivityY();
            horizontalSliderHandleText.text = horizontalSenstivity.value.ToString();
            verticalSliderHandleText.text = verticalSenstivity.value.ToString();

            float decimalVal = Mathf.Pow(10, precision);

            horizontalSenstivity.onValueChanged.AddListener((val) => 
            {
                horizontalSenstivity.value = Mathf.Round(val * decimalVal) / decimalVal;
                PlayerInputHandler.Instance.SetLookSenstivityX(horizontalSenstivity.value);
                horizontalSliderHandleText.text = horizontalSenstivity.value.ToString();
            });

            verticalSenstivity.onValueChanged.AddListener((val) =>
            {
                verticalSenstivity.value = Mathf.Round(val * decimalVal) / decimalVal;
                PlayerInputHandler.Instance.SetLookSenstivityY(verticalSenstivity.value);
                verticalSliderHandleText.text = verticalSenstivity.value.ToString();
            });
        }
    }
}