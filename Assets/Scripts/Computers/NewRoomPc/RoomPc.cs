using Photon.Pun;
using Photon.Realtime;
using Photon.VR;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RoomPc : MonoBehaviourPunCallbacks
{
    public ButtonTypeEnum ButtonType;
    public TextMeshPro RoomText;
    public string Tag;
    private string Letter;
    private bool CanTouch = true;

    private void Start()
    {
        Letter = gameObject.name;

        if (!PhotonNetwork.IsConnected)
        {
            PhotonVRManager.Connect();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(Tag))
        {
            if (ButtonType == ButtonTypeEnum.Letter)
            {
                RoomText.text += gameObject.name;
                if (RoomText.text.Length > 12)
                {
                    RoomText.text = RoomText.text.Substring(0, RoomText.text.Length - 1);
                }
                CanTouch = false;
            }
            if (ButtonType == ButtonTypeEnum.Enter)
            {
                if (PhotonNetwork.InRoom)
                {
                    PhotonNetwork.LeaveRoom();
                    Debug.Log("Left Room");
                }
                StartCoroutine(WaitUntilLeftRoom());
                CanTouch = false;
            }
            if (ButtonType == ButtonTypeEnum.Backspace)
            {
                if (RoomText.text.Length > 0)
                    RoomText.text = RoomText.text.Substring(0, RoomText.text.Length - 1);
                CanTouch = false;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(Tag))
        {
            CanTouch = true;
        }
    }

    private void JoinRoomName()
    {
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 10,
            IsVisible = false
        };
        PhotonNetwork.JoinOrCreateRoom(RoomText.text, roomOptions, TypedLobby.Default);
        Debug.Log("Attempting to join/create room: " + RoomText.text);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log("Room not found: " + message);
        RoomText.text = "NOT FOUND";
    }

    IEnumerator WaitUntilLeftRoom()
    {
        yield return new WaitUntil(() => !PhotonNetwork.InRoom);
        yield return new WaitUntil(() => PhotonNetwork.NetworkClientState == ClientState.JoinedLobby
                                      || PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer);
        JoinRoomName();
    }

    public enum ButtonTypeEnum
    {
        Letter,
        Enter,
        Backspace
    }
}