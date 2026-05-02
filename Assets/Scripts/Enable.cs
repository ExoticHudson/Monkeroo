using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enable : MonoBehaviour
{
    public GameObject enableObject; // object that will be enabled

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("HandTag")) // check if the player entered the trigger
        {
            enableObject.SetActive(true); // enable the specified object
        }
    }
}
