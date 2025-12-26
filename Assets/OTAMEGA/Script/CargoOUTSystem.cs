using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(AudioSource))] // ★追加: 音を鳴らすパーツを必須にする
public class CargoOUTSystem : MonoBehaviour
{
    [Header("--- 降車システム設定 ---")]
    public float detectionRange = 5.0f;
    public float stopThreshold = 0.1f;
    public float waitBeforeUnloading = 1.0f;
    public float unboardInterval = 0.5f;

    [Header("--- 音響設定 (Sound) ---")] // ★追加
    [Tooltip("チャリン！という効果音")]
    public AudioClip cashSfx; 
    
    [Tooltip("1回ごとに上がるピッチの量 (例: 0.1)")]
    public float pitchStep = 0.1f; 
    
    [Tooltip("ピッチの最大値 (これ以上は高くならない)")]
    public float maxPitch = 3.0f;

    [Header("--- 降車エリア設定 ---")]
    public BoxCollider dropOffArea; 
    public float passengerRadius = 0.3f;
    public string footPivotName = "Passenger_FootPivot";

    // 内部変数
    private SphereCollider triggerCollider;
    private Indian_Truck detectedTruck;
    private bool isTruckNearby = false;
    private bool isUnloading = false;
    private Coroutine unloadingCoroutine;
    private LineRenderer lineRenderer;
    private AudioSource audioSource; // ★追加
    
    private int unboardedCount = 0;

    void Awake()
    {
        triggerCollider = GetComponent<SphereCollider>();
        triggerCollider.isTrigger = true;
        
        // ★追加: AudioSourceの取得と設定
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f; // 1.0 = 3Dサウンド (カメラとの距離で音量が変わる)

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

    private void OnTriggerEnter(Collider other)
    {
        Indian_Truck truck = other.GetComponentInParent<Indian_Truck>();
        if (truck != null)
        {
            detectedTruck = truck;
            isTruckNearby = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Indian_Truck truck = other.GetComponentInParent<Indian_Truck>();
        if (truck != null)
        {
            CancelUnloading();
            isTruckNearby = false;
            detectedTruck = null;
        }
    }

    void Update()
    {
        if (isUnloading) lineRenderer.startColor = lineRenderer.endColor = Color.blue;
        else if (isTruckNearby) lineRenderer.startColor = lineRenderer.endColor = Color.yellow;
        else lineRenderer.startColor = lineRenderer.endColor = Color.cyan; 

        if (isTruckNearby && detectedTruck != null)
        {
            bool isStopped = detectedTruck.CurrentSpeed <= stopThreshold;

            if (isStopped && !isUnloading)
            {
                if (detectedTruck.PassengerCount > 0)
                {
                    unloadingCoroutine = StartCoroutine(UnloadingRoutine());
                }
            }
            else if (!isStopped && isUnloading)
            {
                CancelUnloading();
            }
        }
    }

    IEnumerator UnloadingRoutine()
    {
        isUnloading = true;

        // ★追加: コンボ開始時にピッチをリセット（1.0 = 標準の高さ）
        float currentPitch = 1.0f;

        yield return new WaitForSeconds(waitBeforeUnloading);

        while (detectedTruck != null && detectedTruck.PassengerCount > 0)
        {
            if (detectedTruck.CurrentSpeed > stopThreshold)
            {
                CancelUnloading();
                yield break;
            }

            GameObject npc = detectedTruck.UnloadOnePassenger();
            
            if (npc != null)
            {
                PlaceNPCInDropOffArea(npc);
                unboardedCount++;

                // ★追加: 音を鳴らす処理
                PlayCashSound(ref currentPitch);
            }

            yield return new WaitForSeconds(unboardInterval);
        }

        Debug.Log($"降車完了: 合計 {unboardedCount} 人が降りました。");
        isUnloading = false;
    }

    // ★追加: 音を鳴らしてピッチを上げる関数
    void PlayCashSound(ref float pitch)
    {
        if (cashSfx != null && audioSource != null)
        {
            audioSource.pitch = pitch;           // 現在のピッチを適用
            audioSource.PlayOneShot(cashSfx);    // 再生
            
            // 次回のためにピッチを上げる
            pitch += pitchStep;
            
            // 最大値を超えないように制限
            if (pitch > maxPitch) pitch = maxPitch;
        }
    }

    void PlaceNPCInDropOffArea(GameObject npc)
    {
        if (dropOffArea == null) return;

        npc.transform.SetParent(this.transform);

        Vector3 targetPos = GetRandomPointInDropZone();
        
        Transform footPivot = FindDeepChild(npc.transform, footPivotName);
        if(footPivot != null)
        {
            Vector3 offset = npc.transform.InverseTransformPoint(footPivot.position);
            targetPos -= offset; 
        }

        npc.transform.position = targetPos;
        npc.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
    }

    Vector3 GetRandomPointInDropZone()
    {
        Vector3 center = dropOffArea.center;
        Vector3 size = dropOffArea.size;
        
        float margin = passengerRadius;
        float rx = (size.x * 0.5f) - margin;
        float rz = (size.z * 0.5f) - margin;
        if(rx < 0) rx = 0; 
        if(rz < 0) rz = 0;

        float randomX = center.x + Random.Range(-rx, rx);
        float randomZ = center.z + Random.Range(-rz, rz);
        float bottomY = center.y - (size.y * 0.5f);

        Vector3 localPos = new Vector3(randomX, bottomY, randomZ);
        return dropOffArea.transform.TransformPoint(localPos);
    }

    void CancelUnloading()
    {
        if (isUnloading)
        {
            if (unloadingCoroutine != null) StopCoroutine(unloadingCoroutine);
            isUnloading = false;
        }
    }
    
    private Transform FindDeepChild(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        Transform result = parent.Find(name);
        if (result != null) return result;
        foreach (Transform child in parent)
        {
            result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isUnloading ? new Color(0, 0, 1, 0.3f) : new Color(0, 1, 1, 0.2f);
        Gizmos.DrawSphere(transform.position, detectionRange);

        if (dropOffArea != null)
        {
            Gizmos.matrix = dropOffArea.transform.localToWorldMatrix;
            Gizmos.color = new Color(0, 1, 1, 0.5f);
            Gizmos.DrawWireCube(dropOffArea.center, dropOffArea.size);
            Gizmos.color = new Color(0, 1, 1, 0.2f);
            Gizmos.DrawCube(dropOffArea.center, dropOffArea.size);
        }
    }
}