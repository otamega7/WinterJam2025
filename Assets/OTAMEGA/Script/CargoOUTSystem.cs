using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(LineRenderer))]
public class CargoOUTSystem : MonoBehaviour
{
    [Header("--- 降車システム設定 ---")]
    public float detectionRange = 5.0f;
    public float stopThreshold = 0.1f;
    
    [Tooltip("停車してから降車開始までの待機時間")]
    public float waitBeforeUnloading = 1.0f;

    [Tooltip("1人降りるごとの間隔（秒）")]
    public float unboardInterval = 0.5f;

    [Header("--- 降車エリア設定 ---")]
    [Tooltip("客が降り立つ範囲")]
    public BoxCollider dropOffArea; 
    
    [Tooltip("客の半径（重なり防止用）")]
    public float passengerRadius = 0.3f;
    public LayerMask passengerLayer;
    public string footPivotName = "Passenger_FootPivot"; // 足元合わせ用

    // 内部変数
    private SphereCollider triggerCollider;
    private Indian_Truck detectedTruck;
    private bool isTruckNearby = false;
    private bool isUnloading = false;
    private Coroutine unloadingCoroutine;
    private LineRenderer lineRenderer;
    
    // 降ろした人数カウント
    private int unboardedCount = 0;

    void Awake()
    {
        triggerCollider = GetComponent<SphereCollider>();
        triggerCollider.isTrigger = true;

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

    // トラック検知
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
        // 線の色制御（青色を降車中とする）
        if (isUnloading) lineRenderer.startColor = lineRenderer.endColor = Color.blue;
        else if (isTruckNearby) lineRenderer.startColor = lineRenderer.endColor = Color.yellow;
        else lineRenderer.startColor = lineRenderer.endColor = Color.cyan; // 待機色は水色

        if (isTruckNearby && detectedTruck != null)
        {
            bool isStopped = detectedTruck.CurrentSpeed <= stopThreshold;

            if (isStopped && !isUnloading)
            {
                // トラックに客が乗っていれば降車開始
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

        // 1. 停車後の待機
        yield return new WaitForSeconds(waitBeforeUnloading);

        // 2. 客がいる限り降ろし続ける
        while (detectedTruck != null && detectedTruck.PassengerCount > 0)
        {
            // トラックが動いてしまったら中断
            if (detectedTruck.CurrentSpeed > stopThreshold)
            {
                CancelUnloading();
                yield break;
            }

            // --- 降車処理 ---
            GameObject npc = detectedTruck.UnloadOnePassenger();
            
            if (npc != null)
            {
                // 降車エリアのどこかに配置する
                PlaceNPCInDropOffArea(npc);
                unboardedCount++;
            }

            // 次の人が降りるまで待つ
            yield return new WaitForSeconds(unboardInterval);
        }

        Debug.Log($"降車完了: 合計 {unboardedCount} 人が降りました。");
        isUnloading = false;
    }

    // NPCをエリア内に配置する
    void PlaceNPCInDropOffArea(GameObject npc)
    {
        if (dropOffArea == null) return;

        // NPCを降車エリアの子にする（整理のため）
        npc.transform.SetParent(this.transform);

        // 配置場所を探す
        Vector3 targetPos = GetRandomPointInDropZone();
        
        // 足元の補正（簡易版）
        Transform footPivot = FindDeepChild(npc.transform, footPivotName);
        if(footPivot != null)
        {
            Vector3 offset = npc.transform.InverseTransformPoint(footPivot.position);
            targetPos -= offset;
        }

        npc.transform.position = targetPos;
        
        // 向きをランダムに
        npc.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
    }

    Vector3 GetRandomPointInDropZone()
    {
        Vector3 center = dropOffArea.center;
        Vector3 size = dropOffArea.size;
        
        float randomX = center.x + Random.Range(-size.x * 0.4f, size.x * 0.4f);
        float randomZ = center.z + Random.Range(-size.z * 0.4f, size.z * 0.4f);
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
        Gizmos.color = isUnloading ? Color.blue : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (dropOffArea != null)
        {
            Gizmos.matrix = dropOffArea.transform.localToWorldMatrix;
            Gizmos.color = new Color(0, 1, 1, 0.5f); // 水色のボックス
            Gizmos.DrawWireCube(dropOffArea.center, dropOffArea.size);
        }
    }
}