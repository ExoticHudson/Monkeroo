using UnityEngine;
using Photon.Pun;

public class PvpZone : MonoBehaviour
{
    public string objectPath = "Body/YourCosmeticName"; // path to the object inside the player prefab

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PhotonView pv = other.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                other.transform.Find(objectPath)?.gameObject.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PhotonView pv = other.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                other.transform.Find(objectPath)?.gameObject.SetActive(false);
            }
        }
    }
}