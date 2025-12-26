using UnityEngine;

public class Bus : MonoBehaviour
{
    private Rigidbody rb;
    public float CurrentSpeed => rb.linearVelocity.magnitude;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
}
