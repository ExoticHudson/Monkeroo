using Photon.VR;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FlimcyColorPC
{
    public class ColorManagerMain : MonoBehaviour
    {
        public float redMultiplier = 1f;
        public float greenMultiplier = 1f;
        public float blueMultiplier = 1f;
        public Color currentColor;

        public TextMeshPro redValueText;
        public TextMeshPro greenValueText;
        public TextMeshPro blueValueText;

        public List<SkinnedMeshRenderer> OfflinePlayerSkin;

        private void Start()
        {
            LoadColor();
        }

        public void UpdateColor()
        {
            float trueRed = currentColor.r * redMultiplier;
            float trueGreen = currentColor.g * greenMultiplier;
            float trueBlue = currentColor.b * blueMultiplier;
            Color myColour = new Color(trueRed, trueGreen, trueBlue);
            PhotonVRManager.SetColour(myColour);

            foreach (SkinnedMeshRenderer SMR in OfflinePlayerSkin)
            {
                SMR.material.color = myColour;
            }

            UpdateColorCode();
            SaveColor();
        }

        private void UpdateColorCode()
        {
            float redValue = 0;
            float greenValue = 0;
            float blueValue = 0;
            if (currentColor.r != 0)
            {
                redValue = (currentColor.r - 1) * 10;
            }

            if (currentColor.g != 0)
            {
                greenValue = (currentColor.g - 1) * 10;
            }

            if (currentColor.b != 0)
            {
                blueValue = (currentColor.b - 1) * 10;
            }

            redValueText.text = "Red: " + redValue.ToString();
            greenValueText.text = "Green: " + greenValue.ToString();
            blueValueText.text = "Blue: " + blueValue.ToString();
        }

        private void SaveColor()
        {
            PlayerPrefs.SetFloat("RedMultiplier", redMultiplier);
            PlayerPrefs.SetFloat("GreenMultiplier", greenMultiplier);
            PlayerPrefs.SetFloat("BlueMultiplier", blueMultiplier);
            PlayerPrefs.SetFloat("RedValue", currentColor.r);
            PlayerPrefs.SetFloat("GreenValue", currentColor.g);
            PlayerPrefs.SetFloat("BlueValue", currentColor.b);

            for (int i = 0; i < OfflinePlayerSkin.Count; i++)
            {
                Color smrColor = OfflinePlayerSkin[i].material.color;
                PlayerPrefs.SetFloat($"SMR{i}_Red", smrColor.r);
                PlayerPrefs.SetFloat($"SMR{i}_Green", smrColor.g);
                PlayerPrefs.SetFloat($"SMR{i}_Blue", smrColor.b);
            }

            PlayerPrefs.Save();
        }

        private void LoadColor()
        {
            if (PlayerPrefs.HasKey("RedMultiplier"))
            {
                redMultiplier = PlayerPrefs.GetFloat("RedMultiplier");
            }

            if (PlayerPrefs.HasKey("GreenMultiplier"))
            {
                greenMultiplier = PlayerPrefs.GetFloat("GreenMultiplier");
            }

            if (PlayerPrefs.HasKey("BlueMultiplier"))
            {
                blueMultiplier = PlayerPrefs.GetFloat("BlueMultiplier");
            }

            if (PlayerPrefs.HasKey("RedValue") && PlayerPrefs.HasKey("GreenValue") && PlayerPrefs.HasKey("BlueValue"))
            {
                currentColor = new Color(PlayerPrefs.GetFloat("RedValue"), PlayerPrefs.GetFloat("GreenValue"), PlayerPrefs.GetFloat("BlueValue"));
                UpdateColorCode();
            }

            for (int i = 0; i < OfflinePlayerSkin.Count; i++)
            {
                if (PlayerPrefs.HasKey($"SMR{i}_Red") && PlayerPrefs.HasKey($"SMR{i}_Green") && PlayerPrefs.HasKey($"SMR{i}_Blue"))
                {
                    Color smrColor = new Color(PlayerPrefs.GetFloat($"SMR{i}_Red"), PlayerPrefs.GetFloat($"SMR{i}_Green"), PlayerPrefs.GetFloat($"SMR{i}_Blue"));
                    OfflinePlayerSkin[i].material.color = smrColor;
                }
            }
        }
    }
}