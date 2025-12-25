using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CargoPassengerSystem : MonoBehaviour
{
    [Header("必須設定")]
    [Tooltip("生成するPassengerのプレハブ")]
    public GameObject passengerPrefab;
    [Tooltip("沸き位置を指定するBoxCollider (Cargo_Area)")]
    public BoxCollider spawnArea;

    [Header("足元の基準点")]
    [Tooltip("Passenger内の足元基準オブジェクトの名前。\n空欄の場合、自動で 'Passenger_FootPivot' を探します。")]
    public string footPivotName = "Passenger_FootPivot";

    [Header("配置設定")]
    [Tooltip("最大人数")]
    public int maxPassengers = 10;
    [Tooltip("Passenger同士の最低間隔（半径）。重なり防止用")]
    public float passengerRadius = 0.3f;
    [Tooltip("壁からどれくらい内側に配置するか（はみ出し防止）")]
    public float wallMargin = 0.1f;
    [Tooltip("重なり判定を行うレイヤー")]
    public LayerMask passengerLayer;

    [Header("ロジック設定")]
    public int maxSpawnAttempts = 30;

    // 管理用リスト
    [SerializeField]
    private List<GameObject> currentPassengers = new List<GameObject>();
    public int CurrentCount => currentPassengers.Count;

    // プレハブ内の FootPivot のローカル座標をキャッシュしておく変数
    private Vector3 cachedPivotLocalPos = Vector3.zero;
    private bool hasCachedPivot = false;

    /// <summary>
    /// Passengerを追加するメイン処理
    /// </summary>
    public void TryAddPassenger()
    {
        currentPassengers.RemoveAll(item => item == null);

        if (currentPassengers.Count >= maxPassengers)
        {
            Debug.Log("定員オーバーです。");
            return;
        }

        // 1. プレハブからFootPivotのズレを取得（初回のみ）
        if (!hasCachedPivot) CacheFootPivotOffset();

        Vector3 targetFloorPos = Vector3.zero;
        bool foundValidSpot = false;

        // 2. 空き場所検索（Cargo_Areaの床面上の点を探す）
        Vector3 bestFallbackPos = Vector3.zero;
        float maxDistanceToNearest = -1f;

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            // 荷台の床面上のランダムな点を取得
            Vector3 candidateFloorPos = GetRandomPointOnCargoFloor();

            // その「足元位置」の少し上（半径分）を中心に球体判定を行い、空いているか確認
            // ※足元そのものだと床と接触判定してしまう可能性があるため、少し浮かせて判定
            Vector3 checkPos = candidateFloorPos + (spawnArea.transform.up * passengerRadius);
            
            Collider[] hitColliders = Physics.OverlapSphere(checkPos, passengerRadius, passengerLayer);

            if (hitColliders.Length == 0)
            {
                targetFloorPos = candidateFloorPos;
                foundValidSpot = true;
                break;
            }
            else
            {
                // 混んでいる場合の「マシな場所」計算
                float closestDist = float.MaxValue;
                foreach (var col in hitColliders)
                {
                    if (col == null) continue;
                    // 距離計算は水平距離で行うのが理想だが、簡易的に3D距離で比較
                    float d = Vector3.Distance(candidateFloorPos, col.transform.position);
                    if (d < closestDist) closestDist = d;
                }

                if (closestDist > maxDistanceToNearest)
                {
                    maxDistanceToNearest = closestDist;
                    bestFallbackPos = candidateFloorPos;
                }
            }
        }

        // 3. 決定した「床の座標」を使って生成
        if (foundValidSpot)
        {
            SpawnPassengerMatchedToPivot(targetFloorPos);
        }
        else if (maxDistanceToNearest >= 0)
        {
            Debug.LogWarning($"空きスペースなし。距離{maxDistanceToNearest:F2}の場所に配置します。");
            SpawnPassengerMatchedToPivot(bestFallbackPos);
        }
        else
        {
            SpawnPassengerMatchedToPivot(GetRandomPointOnCargoFloor());
        }
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

    /// <summary>
    /// FootPivotが指定座標に来るようにルート座標を計算して生成
    /// </summary>
    private void SpawnPassengerMatchedToPivot(Vector3 targetFloorWorldPos)
    {
        // ランダムなY軸回転を作成
        Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

        // ★重要計算★
        // ルート位置 = 目標地点(床) - (回転 * Pivotのローカル位置)
        // これにより、回転を考慮した上で FootPivot が TargetFloorPos に重なる
        Vector3 calculatedRootPos = targetFloorWorldPos - (randomRotation * cachedPivotLocalPos);

        GameObject newObj;

#if UNITY_EDITOR
        if (!Application.isPlaying && PrefabUtility.IsPartOfAnyPrefab(passengerPrefab))
        {
            newObj = (GameObject)PrefabUtility.InstantiatePrefab(passengerPrefab);
            newObj.transform.position = calculatedRootPos;
            newObj.transform.rotation = randomRotation;
            Undo.RegisterCreatedObjectUndo(newObj, "Spawn Passenger");
        }
        else
#endif
        {
            newObj = Instantiate(passengerPrefab, calculatedRootPos, randomRotation);
        }

        if (spawnArea != null)
        {
            newObj.transform.SetParent(spawnArea.transform, true);
        }
        
        currentPassengers.Add(newObj);
    }

    /// <summary>
    /// プレハブ内の FootPivot の位置を特定してキャッシュする
    /// </summary>
    private void CacheFootPivotOffset()
    {
        if (passengerPrefab == null) return;

        Transform pivotTrans = FindDeepChild(passengerPrefab.transform, footPivotName);

        if (pivotTrans != null)
        {
            // ルートからのローカル座標を保存
            // ※プレハブのルートが(0,0,0)でない場合も考慮し、localPositionではなくInverseTransformPointを使う
            cachedPivotLocalPos = passengerPrefab.transform.InverseTransformPoint(pivotTrans.position);
            Debug.Log($"FootPivot検知: オフセット {cachedPivotLocalPos}");
        }
        else
        {
            Debug.LogWarning($"'{footPivotName}' が見つかりません。オフセット(0,0,0)として扱います。");
            cachedPivotLocalPos = Vector3.zero;
        }
        hasCachedPivot = true;
    }

    // 子、孫、ひ孫...と再帰的に探すヘルパー
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

    /// <summary>
    /// Cargo_Areaの底面上のランダム座標（ワールド）を取得
    /// </summary>
    private Vector3 GetRandomPointOnCargoFloor()
    {
        if (spawnArea == null) return Vector3.zero;

        // BoxColliderの中心とサイズ
        Vector3 center = spawnArea.center;
        Vector3 size = spawnArea.size;

        // 底面のY座標 (ローカル)
        float floorY = center.y - (size.y * 0.5f);

        // X, Zのランダム範囲 (ローカル)
        float safeX = Mathf.Max(0, (size.x * 0.5f) - wallMargin - passengerRadius);
        float safeZ = Mathf.Max(0, (size.z * 0.5f) - wallMargin - passengerRadius);

        float randomX = center.x + Random.Range(-safeX, safeX);
        float randomZ = center.z + Random.Range(-safeZ, safeZ);

        // ローカル座標作成
        Vector3 localPos = new Vector3(randomX, floorY, randomZ);

        // ワールド座標へ変換
        return spawnArea.transform.TransformPoint(localPos);
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnArea == null) return;

        Gizmos.matrix = spawnArea.transform.localToWorldMatrix;

        // 有効範囲を青い板で表示
        float safeX = Mathf.Max(0, spawnArea.size.x - (wallMargin + passengerRadius) * 2);
        float safeZ = Mathf.Max(0, spawnArea.size.z - (wallMargin + passengerRadius) * 2);
        
        // 底面の位置
        float floorY = spawnArea.center.y - (spawnArea.size.y * 0.5f);
        
        Vector3 drawCenter = new Vector3(spawnArea.center.x, floorY, spawnArea.center.z);
        Vector3 drawSize = new Vector3(safeX, 0.02f, safeZ);

        Gizmos.color = new Color(0, 1, 1, 0.4f); // 水色半透明
        Gizmos.DrawCube(drawCenter, drawSize);
        Gizmos.DrawWireCube(drawCenter, drawSize);
    }
}