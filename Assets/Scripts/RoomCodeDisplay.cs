using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class RoomCodeDisplay : MonoBehaviourPunCallbacks
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

    // fires when you join a room
    public override void OnJoinedRoom()
    {
        roomCodeText.text = "Code: " + PhotonNetwork.CurrentRoom.Name;
    }

    // fires when you leave a room
    public override void OnLeftRoom()
    {
        roomCodeText.text = "Code: Not connected";
    }

    // fires when you join a different room
    public override void OnJoinedLobby()
    {
        roomCodeText.text = "Code: Not connected";
    }
}