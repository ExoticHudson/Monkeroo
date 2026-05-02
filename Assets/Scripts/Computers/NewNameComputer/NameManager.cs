using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NameManager : MonoBehaviour
{
    public TextMeshPro NameText;
    public string Name;

    public TextMeshPro CurrentNameText;

    private void Update()
    {
        if (Name.Length > 12)
        {
            Name = Name.Substring(0, 11);
        }
        NameText.text = Name;

        CurrentNameText.text = PhotonNetwork.LocalPlayer.NickName;
    }
}
