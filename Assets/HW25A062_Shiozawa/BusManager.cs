using UnityEngine;

public class BusManager : MonoBehaviour
{
    public int currentPassengers = 0;

    private Rigidbody rb;
    public float currentSpeed => rb != null ? rb.linearVelocity.magnitude : 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

}
