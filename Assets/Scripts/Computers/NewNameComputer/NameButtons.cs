using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.VR;
using Photon.Pun;
using PlayFab;
using PlayFab.ClientModels;

public class NameButtons : MonoBehaviour
{
    private string Letter;
    public NameManager NameManager;

    [Header("You only need to do this for the enter button")]
    public List<string> BannedNames;

    public Type1 NameType;

    private void Start()
    {
        Letter = gameObject.name;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("HandTag"))
        {
            if (NameType == Type1.NameButton)
            {
                NameManager.Name += Letter;
            }
            if (NameType == Type1.EnterName)
            {
                if(NameManager.Name == string.Empty)
                {
                    NameManager.Name = "Chimp" + Random.Range(1000000, 9999999);
                }

                else foreach (string BannedName in BannedNames)
                {
                    if (NameManager.Name.Contains(BannedName))
                    {
                        NameManager.Name = "Chimp" + Random.Range(1000000, 9999999);
                        Debug.Log("You have now been kicked!");

                        PhotonNetwork.LeaveRoom();
                    }
                }
                
                if (NameManager.Name != string.Empty)
                {
                    PhotonVRManager.SetUsername(NameManager.Name);
                    UpdatePlayFabDisplayName(NameManager.Name);
                }
                
            }
            if (NameType == Type1.BackspaceName)
            {
                NameManager.Name = NameManager.Name.Remove(NameManager.Name.Length - 1);
            }
        }
    }

    void UpdatePlayFabDisplayName(string newName)
    {
        var request = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = newName
        };

        PlayFabClientAPI.UpdateUserTitleDisplayName(request, OnDisplayNameUpdated, OnPlayFabError);
    }

    void OnDisplayNameUpdated(UpdateUserTitleDisplayNameResult result)
    {
        Debug.Log("PlayFab display name updated successfully.");
    }

    void OnPlayFabError(PlayFab.PlayFabError error)
    {
        Debug.LogError("PlayFab error: " + error.ErrorMessage);
    }

    public enum Type1
    {
        NameButton,
        EnterName,
        BackspaceName
    }
}