using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CargoPassengerSystem : MonoBehaviour
{
    // ▼ 向きの種類を定義
    public enum RotationMode
    {
        Random360,      // 完全にランダム（0～360度）
        Fixed,          // 指定した角度に固定（全員同じ向き）
        FixedWithNoise  // 指定した角度を中心に、少しバラつかせる
    }

    [Header("必須設定")]
    public GameObject passengerPrefab;
    public BoxCollider spawnArea;
    public string footPivotName = "Passenger_FootPivot";

    [Header("向きの設定")]
    [Tooltip("向きの決め方を選択")]
    public RotationMode rotationMode = RotationMode.Random360;

    [Tooltip("Fixedモード時の基準角度（0=荷台の前方, 180=後方, 90=右, -90=左）")]
    [Range(0, 360)]
    public float facingAngle = 0f;

    [Tooltip("FixedWithNoiseモード時のバラつき具合（例: 30なら ±30度の範囲でランダム）")]
    [Range(0, 180)]
    public float angleNoise = 45f;

    [Header("配置設定")]
    public int maxPassengers = 10;
    public float passengerRadius = 0.3f;
    public float wallMargin = 0.1f;
    public LayerMask passengerLayer;

    [Header("ロジック設定")]
    public int maxSpawnAttempts = 30;

    [SerializeField]
    private List<GameObject> currentPassengers = new List<GameObject>();
    public int CurrentCount => currentPassengers.Count;

    private Vector3 cachedPivotLocalPos = Vector3.zero;
    private bool hasCachedPivot = false;

    public void TryAddPassenger()
    {
        currentPassengers.RemoveAll(item => item == null);

        if (currentPassengers.Count >= maxPassengers)
        {
            Debug.Log("定員オーバーです。");
            return;
        }

        if (!hasCachedPivot) CacheFootPivotOffset();

        Vector3 targetPos = Vector3.zero;
        bool foundValidSpot = false;
        Vector3 bestFallbackPos = Vector3.zero;
        float maxDistanceToNearest = -1f;

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector3 candidatePos = GetRandomPointInCargoVolume();
            Collider[] hitColliders = Physics.OverlapSphere(candidatePos, passengerRadius, passengerLayer);

            if (hitColliders.Length == 0)
            {
                targetPos = candidatePos;
                foundValidSpot = true;
                break;
            }
            else
            {
                float closestDist = float.MaxValue;
                foreach (var col in hitColliders)
                {
                    if (col == null) continue;
                    float d = Vector3.Distance(candidatePos, col.transform.position);
                    if (d < closestDist) closestDist = d;
                }

                if (closestDist > maxDistanceToNearest)
                {
                    maxDistanceToNearest = closestDist;
                    bestFallbackPos = candidatePos;
                }
            }
        }

        if (foundValidSpot) SpawnPassengerMatchedToPivot(targetPos);
        else if (maxDistanceToNearest >= 0) SpawnPassengerMatchedToPivot(bestFallbackPos);
        else SpawnPassengerMatchedToPivot(GetRandomPointInCargoVolume());
    }

    public void RemovePassenger()
    {
        currentPassengers.RemoveAll(item => item == null);
        if (currentPassengers.Count == 0) return;

        GameObject target = currentPassengers[currentPassengers.Count - 1];
        currentPassengers.RemoveAt(currentPassengers.Count - 1);

#if UNITY_EDITOR
        if (!Application.isPlaying) Undo.DestroyObjectImmediate(target);
        else Destroy(target);
#else
        Destroy(target);
#endif
    }

    private void SpawnPassengerMatchedToPivot(Vector3 targetWorldPos)
    {
        // ▼ 向きの計算ロジックを変更 ▼
        Quaternion finalRotation = Quaternion.identity;
        Quaternion cargoRotation = (spawnArea != null) ? spawnArea.transform.rotation : Quaternion.identity;

        switch (rotationMode)
        {
            case RotationMode.Random360:
                // 完全にランダム
                finalRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                break;

            case RotationMode.Fixed:
                // 荷台の向き + 指定角度
                finalRotation = cargoRotation * Quaternion.Euler(0, facingAngle, 0);
                break;

            case RotationMode.FixedWithNoise:
                // 指定角度 + ランダムなバラつき
                float noise = Random.Range(-angleNoise, angleNoise);
                finalRotation = cargoRotation * Quaternion.Euler(0, facingAngle + noise, 0);
                break;
        }

        // Pivotの位置合わせ計算（回転を適用した状態で行う）
        Vector3 calculatedRootPos = targetWorldPos - (finalRotation * cachedPivotLocalPos);

        GameObject newObj;

#if UNITY_EDITOR
        if (!Application.isPlaying && PrefabUtility.IsPartOfAnyPrefab(passengerPrefab))
        {
            newObj = (GameObject)PrefabUtility.InstantiatePrefab(passengerPrefab);
            newObj.transform.position = calculatedRootPos;
            newObj.transform.rotation = finalRotation;
            Undo.RegisterCreatedObjectUndo(newObj, "Spawn Passenger");
        }
        else
#endif
        {
            newObj = Instantiate(passengerPrefab, calculatedRootPos, finalRotation);
        }

        if (spawnArea != null)
        {
            newObj.transform.SetParent(spawnArea.transform, true);
        }
        
        currentPassengers.Add(newObj);
    }

    private void CacheFootPivotOffset()
    {
        if (passengerPrefab == null) return;
        Transform pivotTrans = FindDeepChild(passengerPrefab.transform, footPivotName);
        if (pivotTrans != null)
        {
            cachedPivotLocalPos = passengerPrefab.transform.InverseTransformPoint(pivotTrans.position);
        }
        else
        {
            cachedPivotLocalPos = Vector3.zero;
        }
        hasCachedPivot = true;
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        Transform result = parent.Find(name);
        if (result != null) return result;
        foreach (Transform child in parent)
        {
            result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private Vector3 GetRandomPointInCargoVolume()
    {
        if (spawnArea == null) return Vector3.zero;

        Vector3 center = spawnArea.center;
        Vector3 size = spawnArea.size;

        float safeX = Mathf.Max(0, (size.x * 0.5f) - wallMargin - passengerRadius);
        float safeY = Mathf.Max(0, (size.y * 0.5f) - wallMargin - passengerRadius);
        float safeZ = Mathf.Max(0, (size.z * 0.5f) - wallMargin - passengerRadius);

        float randomX = center.x + Random.Range(-safeX, safeX);
        float randomY = center.y + Random.Range(-safeY, safeY);
        float randomZ = center.z + Random.Range(-safeZ, safeZ);

        Vector3 localPos = new Vector3(randomX, randomY, randomZ);

        return spawnArea.transform.TransformPoint(localPos);
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnArea == null) return;

        Gizmos.matrix = spawnArea.transform.localToWorldMatrix;

        float safeX = Mathf.Max(0, spawnArea.size.x - (wallMargin + passengerRadius) * 2);
        float safeY = Mathf.Max(0, spawnArea.size.y - (wallMargin + passengerRadius) * 2);
        float safeZ = Mathf.Max(0, spawnArea.size.z - (wallMargin + passengerRadius) * 2);
        
        Vector3 drawCenter = spawnArea.center;
        Vector3 drawSize = new Vector3(safeX, safeY, safeZ);

        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawCube(drawCenter, drawSize);
        Gizmos.color = new Color(0, 1, 1, 1.0f);
        Gizmos.DrawWireCube(drawCenter, drawSize);
        
        // 向きの目安を矢印で表示
        Gizmos.color = Color.magenta;
        Vector3 arrowStart = drawCenter;
        Vector3 arrowDirection = Quaternion.Euler(0, facingAngle, 0) * Vector3.forward;
        Gizmos.DrawRay(drawCenter, arrowDirection * 1.5f);
    }
}