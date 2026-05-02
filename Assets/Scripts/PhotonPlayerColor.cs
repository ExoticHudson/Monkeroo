using Photon.VR;
using UnityEngine;

public class PhotonPlayerColor : MonoBehaviour
{
    public SkinnedMeshRenderer[] playerRenderers;

    private void Start()
    {
        if (PlayerPrefs.HasKey("RedValue") && PlayerPrefs.HasKey("GreenValue") && PlayerPrefs.HasKey("BlueValue"))
        {
            float r = PlayerPrefs.GetFloat("RedValue");
            float g = PlayerPrefs.GetFloat("GreenValue");
            float b = PlayerPrefs.GetFloat("BlueValue");

            float redMult = PlayerPrefs.GetFloat("RedMultiplier", 1f);
            float greenMult = PlayerPrefs.GetFloat("GreenMultiplier", 1f);
            float blueMult = PlayerPrefs.GetFloat("BlueMultiplier", 1f);

            Color myColor = new Color(r * redMult, g * greenMult, b * blueMult);

            PhotonVRManager.SetColour(myColor);

            foreach (SkinnedMeshRenderer smr in playerRenderers)
            {
                smr.material.color = myColor;
            }
        }
    }
}