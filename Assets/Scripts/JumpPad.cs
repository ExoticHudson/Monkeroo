using UnityEngine;
public class JumpPad : MonoBehaviour
{
    [Header("Script Made By Nelom._.! Make Sure To Try Crediting Me!")]

    [Header("Settings")]
    public Rigidbody rb;
    public float force = 5f;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("HandTag"))
        {
            rb.AddForce(0, force, 0);
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            rb.AddForce(0, force, 0);
        }
    }
}