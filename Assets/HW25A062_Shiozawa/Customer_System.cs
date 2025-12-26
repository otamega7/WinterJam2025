using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(LineRenderer))]
public class Customer_System : MonoBehaviour
{
    [Header("設定")]
    public float detectionRange = 5.0f;  // 範囲   
    public float stopThreshold = 0.1f;  // 速度
    public float rideTime = 1.0f;       // 乗るための時間
    public int customer = 1;

    private SphereCollider triggerCollider;
    private Bus detectedBus;
    private bool isBusNearby = false;
    private bool isBoarding = false;
    private Coroutine boardingCoroutine;
    private LineRenderer lineRenderer;

    void Awake()
    {
        triggerCollider = GetComponent<SphereCollider>();
        triggerCollider.isTrigger = true;

        //lineRendererの設定
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 51;
        lineRenderer.useWorldSpace = false;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.loop = true;

        UpdateColliderSize();
        DrawCircle();
    }

    void OnValidate()
    {
        UpdateColliderSize();
    }

    void UpdateColliderSize()
    {
        if (triggerCollider == null) triggerCollider = GetComponent<SphereCollider>();
        triggerCollider.radius = detectionRange;
        if (lineRenderer != null) DrawCircle();
    }

    void DrawCircle()
    {
        float angle = 0f;
        for (int i = 0; i < 51; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * detectionRange;
            float z = Mathf.Cos(Mathf.Deg2Rad * angle) * detectionRange;
            lineRenderer.SetPosition(i, new Vector3(x, 0, z));
            angle += 360f / 50;
        }
    }

    // バスが入ってきたとき
    private void OnTriggerEnter(Collider other)
    {
        Bus bus = other.GetComponentInParent<Bus>();    // タグを入れ替える
        if (bus != null)
        {
            detectedBus = bus;
            isBusNearby = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Bus bus = other.GetComponentInParent<Bus>();    // タグを入れ替える
        if (bus != null)
        {
            CancelBoarding();
            isBusNearby = false;
            detectedBus = null;
        }
    }

    void Update()
    {
        if (isBoarding) lineRenderer.startColor = lineRenderer.endColor = Color.green;
        else if (isBusNearby) lineRenderer.startColor = lineRenderer.endColor = Color.yellow;
        else lineRenderer.startColor = lineRenderer.endColor = Color.red;

        if (isBusNearby && detectedBus != null)
        {
            bool isBusStopped = detectedBus.CurrentSpeed <= stopThreshold;

            if (isBusStopped && !isBoarding)
            {
                boardingCoroutine = StartCoroutine(BoardingRoutine());
            }
            else if (!isBusStopped && isBoarding)
            {
                CancelBoarding();
            }
        }
    }

    IEnumerator BoardingRoutine()
    {
        isBoarding = true;

        yield return new WaitForSeconds(rideTime);

        // 乗車完了後の処理を呼び出す
        CompleteBoarding();
    }

    void CancelBoarding()
    {
        if (isBoarding)
        {
            if (boardingCoroutine != null) StopCoroutine(boardingCoroutine);
            isBoarding = false;
        }
    }

    void CompleteBoarding()
    {

        Destroy(gameObject);
        customer = customer + 1;
    }


    private void OnDrawGizmos()
    {
        if (isBusNearby)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f); // 検知中は緑
        }
        else
        {
            Gizmos.color = new Color(1, 0, 0, 0.2f); // 待機中は赤
        }

        Gizmos.DrawSphere(transform.position, detectionRange);

        if (detectedBus != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, detectedBus.transform.position);
        }
    }
}
