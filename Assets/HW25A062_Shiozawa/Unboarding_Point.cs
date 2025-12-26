using UnityEngine;
using System.Collections;
using UnityEngine.Analytics;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(LineRenderer))]
public class UnboardingPoint : MonoBehaviour
{
    public float detectionRange = 5.0f;
    public float stopThreshold = 0.1f;
    public int passengersToUnboard = 1;
    public float unboardingInterval = 1.0f;

    private bool isBusAtStop = false;
    private Bus_Unboarding detectedBus;
    private bool hasUnboarded = false;
    private bool isProcessRunning = false;
    private LineRenderer lineRender;
    private SphereCollider triggerCollider;
    private Customer_System customerSystem; // バス停の人数管理

    void Awake()
    {
        lineRender = GetComponent<LineRenderer>();
        customerSystem = GetComponent<Customer_System>(); // 同じオブジェクトにある前提

        lineRender.positionCount = 51;
        lineRender.useWorldSpace = false;
        lineRender.startWidth = 0.1f;
        lineRender.endWidth = 0.1f;
        lineRender.loop = true;

        UpdateSize();
        GetComponent<SphereCollider>().isTrigger = true;
    }

    void OnValidate()
    {
        UpdateSize();
    }

    void UpdateSize()
    {
        if (lineRender == null) lineRender = GetComponent<LineRenderer>();
        lineRender.positionCount = 51;
        GetComponent<SphereCollider>().radius = detectionRange;
        if (triggerCollider == null) triggerCollider = GetComponent<SphereCollider>();
        if (triggerCollider == null) triggerCollider.radius = detectionRange;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Bus_Unboarding>(out var bus))
        {
            detectedBus = bus;
            isBusAtStop = true;
            hasUnboarded = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<Bus_Unboarding>() != null)
        {
            isBusAtStop = false;
            detectedBus = null;
        }
    }

    void Update()
    {
        if (isBusAtStop && detectedBus != null && !hasUnboarded)
        {
            if (detectedBus.CurrentSpeed <= stopThreshold)
            {
                StartCoroutine(UnboardingRoutine());
            }
        }

        // 色の更新ロジックを整理
        UpdateIndicatorColor();
    }

    IEnumerator UnboardingRoutine()
    {
        isProcessRunning = true;

        for (int i = 0; i < passengersToUnboard; i++)
        {
            detectedBus.UnboardPassenger();

            if (customerSystem != null)
            {
                customerSystem.customer++;
            }
            yield return new WaitForSeconds(unboardingInterval);

            if (detectedBus == null || detectedBus.CurrentSpeed > stopThreshold)
            {
                break;
            }
        }
        isProcessRunning = false;
        hasUnboarded = true;
    }

    void UpdateIndicatorColor()
    {
        if (lineRender == null) return;
        if (detectedBus = null)
        {
            lineRender.startColor = lineRender.endColor = Color.white;
            return;
        }
        float speed = 0f;
        try
        {
            speed = detectedBus.CurrentSpeed;
        }
        catch
        {
            return;
        }

        if (isProcessRunning)
        {
            lineRender.startColor = lineRender.endColor = Color.green;
        }
        else if (hasUnboarded)
        {
            lineRender.startColor = lineRender.endColor = Color.gray;
        }
        else if (isBusAtStop && speed <= stopThreshold)
        {
            lineRender.startColor = lineRender.endColor = Color.yellow;
        }
        else
        {
            lineRender.startColor = lineRender.endColor = Color.white;
        }

        void StartUnboardingProcess()
        {
            hasUnboarded = true;

            for (int i = 0; i < passengersToUnboard; i++)
            {
                // バス側の人数を減らし、客を生成する
                detectedBus.UnboardPassenger();

                // バス停側の人数（customer）を増やす
                if (customerSystem != null)
                {
                    customerSystem.customer++;
                }

                Debug.Log("降車処理を実行しました");
            }
        }

        void DrawCircle()
        {
            if (lineRender.positionCount < 51) lineRender.positionCount = 51;

            float angle = 0f;
            for (int i = 0; i < 51; i++)
            {
                float x = Mathf.Sin(Mathf.Deg2Rad * angle) * detectionRange;
                float z = Mathf.Cos(Mathf.Deg2Rad * angle) * detectionRange;
                lineRender.SetPosition(i, new Vector3(x, 0, z));
                angle += (360f / 50);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}