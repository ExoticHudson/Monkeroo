using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.VR;
using Photon.VR.Cosmetics;
public class ChangeBodyCosmetic : MonoBehaviour
{
    public string Body;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("HandTag"))
        {
            PhotonVRManager.SetCosmetic(CosmeticType.Body, Body);
        }
    }
}
