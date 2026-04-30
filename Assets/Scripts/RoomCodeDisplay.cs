using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class RoomCodeDisplay : MonoBehaviour
{
    public TextMeshPro roomCodeText;

    void Update()
    {
        if (PhotonNetwork.InRoom)
        {
            roomCodeText.text = "Code: " + PhotonNetwork.CurrentRoom.Name;
        }
        else
        {
            roomCodeText.text = "Code: Not connected";
        }
    }
}