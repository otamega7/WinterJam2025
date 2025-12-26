using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Analytics;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(LineRenderer))]
public class UnboardingPoint : MonoBehaviour
{
    public float detectionRange = 5.0f;
    public float stopThreshold = 0.1f;
    public int passengersToUnboard = 3; // このバス停で降りる人数

    private bool isBusAtStop = false;
    private Bus_Unboarding detectedBus;
    private bool hasUnboarded = false; // 二重発生防止
    private LineRenderer lineRender;

    void Awake()
    {
        lineRender = GetComponent<LineRenderer>();
        lineRender.positionCount = 51;
        lineRender.useWorldSpace = false;
        lineRender.startWidth = 0.1f;
        lineRender.endWidth = 0.1f;
        lineRender.loop = true;

        DrawCircle();
        GetComponent<SphereCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Bus_Unboarding>(out var bus))
        {
            detectedBus = bus;
            isBusAtStop = true;
            hasUnboarded = false;
        }
        if (lineRender != null) DrawCircle();
    }

    void DrawCircle()
    {
        float angle = 0f;
        for (int i = 0; i < 51; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * detectionRange;
            float z = Mathf.Cos(Mathf.Deg2Rad * angle) * detectionRange;
            lineRender.SetPosition(i, new Vector3(x, 0, z));
            angle += (360f / 50);
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
        // バスが範囲内にいて、止まっていて、まだ降ろしていないなら
        if (isBusAtStop && detectedBus != null && !hasUnboarded)
        {
            if (detectedBus.CurrentSpeed <= stopThreshold)
            {
                StartUnboardingProcess();
            }
        }
        if (isBusAtStop) lineRender.startColor = lineRender.endColor = Color.blue;
        else if (detectedBus) lineRender.startColor = lineRender.endColor = Color.yellow;

    }

    void StartUnboardingProcess()
    {
        hasUnboarded = true;

        // 設定した人数分だけ降ろす
        for (int i = 0; i < passengersToUnboard; i++)
        {
            detectedBus.UnboardPassenger();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue; // 降車は青色などで区別
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}