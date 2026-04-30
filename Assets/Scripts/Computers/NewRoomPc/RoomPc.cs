using Photon.Pun;
using Photon.Realtime;
using Photon.VR;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RoomPc : MonoBehaviour
{
    public ButtonTypeEnum ButtonType;

    public TextMeshPro RoomText;
    public string Tag;

    private string Letter;
    private bool CanTouch = true;

    private void Start()
    {
        Letter = gameObject.name;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(Tag))
        {
            if (ButtonType == ButtonTypeEnum.Letter)
            {
                RoomText.text += gameObject.name;

                if (RoomText.text.Length > 12 )
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
        RoomOptions roomOptions = new RoomOptions { MaxPlayers = 10};
        PhotonNetwork.JoinOrCreateRoom(RoomText.text, roomOptions, TypedLobby.Default);
        Debug.Log("Joined Room" + RoomText.text);
    }

    IEnumerator WaitUntilLeftRoom()
    {
        yield return new WaitForSeconds(0.5f);
        JoinRoomName();
    }

    public enum ButtonTypeEnum
    {
        Letter,
        Enter,
        Backspace
    }
}