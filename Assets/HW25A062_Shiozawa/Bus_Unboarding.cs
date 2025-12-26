using UnityEngine;

public class Bus_Unboarding : MonoBehaviour
{
    public GameObject passegerPrefab;       // 客のプレハブ
    public Transform doorTransform;

    private Rigidbody rb;
    public float CurrentSpeed => rb.linearVelocity.magnitude;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void UnboardPassenger()
    {
        if (passegerPrefab != null && doorTransform != null)
        {
            Instantiate(passegerPrefab, doorTransform.position, doorTransform.rotation);
        }
    }
}
