using UnityEngine;

public class FogController : MonoBehaviour
{
    [Header("pxp made this, Credit would be nice")]
    public bool enableFog = true;
    public Color fogColor = Color.gray;
    public float fogDensity = 0.03f;

    private bool fogChanged = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !fogChanged)
        {
            RenderSettings.fog = enableFog;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
            fogChanged = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && fogChanged)
        {
            fogChanged = false;
        }
    }
}
