using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableScript : MonoBehaviour
{
    public GameObject DisableObject; // thing that will be disabled

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("HandTag"))
        {
            DisableObject.SetActive(false);
        }
    }
}
