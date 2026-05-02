using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace FlimcyColorPC
{
    public class ColorButtons : MonoBehaviour
    {
        public ColorType ColorButtonType;

        public string Tag = "FC";


        public ColorManagerMain ColorManagerScript;
        public float Value = 1f;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(Tag))
            {
                if (ColorButtonType == ColorType.Red)
                {
                    ColorManagerScript.currentColor.r = Value;
                    ColorManagerScript.UpdateColor();
                }

                if (ColorButtonType == ColorType.Green)
                {
                    ColorManagerScript.currentColor.g = Value;
                    ColorManagerScript.UpdateColor();
                }

                if (ColorButtonType == ColorType.Blue)
                {
                    ColorManagerScript.currentColor.b = Value;
                    ColorManagerScript.UpdateColor();
                }
            }
        }

        public enum ColorType
        {
            Red,
            Green,
            Blue
        }
    }
}