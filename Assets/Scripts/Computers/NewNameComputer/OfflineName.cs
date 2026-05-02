using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Photon;
using Photon.VR;
using Photon.Pun;

public class OfflineName : MonoBehaviour
{
    private TextMeshPro OfflineNameText;

    private void Start()
    {
        OfflineNameText = GetComponent<TextMeshPro>();
    }

    private void Update()
    {
        OfflineNameText.text = PhotonNetwork.LocalPlayer.NickName;

        if (PhotonNetwork.InRoom)
        {
            OfflineNameText.text = " ";
        }
    }
}
