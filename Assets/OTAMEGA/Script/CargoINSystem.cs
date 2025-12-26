using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(LineRenderer))]
public class CargoINSystem : MonoBehaviour
{
    // ▼▼▼ 生成モードの定義 ▼▼▼
    public enum SpawnCountMode
    {
        Fixed,          // 固定人数
        RandomRange     // ランダムな範囲
    }

    [Header("--- トラック積み込みシステム設定 ---")]
    public float detectionRange = 5.0f;
    public float stopThreshold = 0.1f;
    
    [Tooltip("最初の1人目が乗るまでの待機時間")]
    public float rideTime = 1.0f;

    [Tooltip("1人乗るごとの間隔（秒）")]
    public float boardingInterval = 0.5f; // ★追加：この時間ごとに乗ります
    
    [HideInInspector] public int totalCargoLoaded = 0; 

    [Header("--- NPC生成設定 (配置) ---")]
    public BoxCollider spawnArea; 
    public GameObject npcPrefab;
    
    public SpawnCountMode countMode = SpawnCountMode.RandomRange;
    public int fixedCount = 3;
    public int minCount = 1;
    public int maxCount = 5;

    public float passengerRadius = 0.3f;
    public int maxSpawnAttempts = 30;
    public LayerMask passengerLayer;
    public string footPivotName = "Passenger_FootPivot";

    [Header("--- NPC生成設定 (見た目) ---")]
    public Color[] headColors = new Color[] { Color.white, Color.gray, Color.black };
    public Color[] bodyColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow, Color.cyan };


    private List<GameObject> spawnedNPCs = new List<GameObject>();
    private SphereCollider triggerCollider;
    
    private Indian_Truck detectedTruck;
    
    private bool isTruckNearby = false;
    private bool isLoading = false;
    private Coroutine loadingCoroutine;
    private LineRenderer lineRenderer;
    private Vector3 cachedPivotLocalPos = Vector3.zero;
    private bool hasCachedPivot = false;

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

    void Start()
    {
        GenerateWaitingPassengers();
    }

    // === NPC生成ロジック (変更なし) ===
    void GenerateWaitingPassengers()
    {
        if (spawnArea == null || npcPrefab == null) return;

        int countToSpawn = (countMode == SpawnCountMode.Fixed) ? fixedCount : Random.Range(minCount, maxCount + 1);

        for (int i = 0; i < countToSpawn; i++) TrySpawnSinglePassenger();
        
        // 生成した時点では「まだ乗っていない」のでカウントは確定させないが、管理用としてリスト数を使う
    }

    void TrySpawnSinglePassenger()
    {
        if (!hasCachedPivot) CacheFootPivotOffset();

        Vector3 targetPos = Vector3.zero;
        bool foundValidSpot = false;

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector3 candidatePos = GetRandomPointInSpawnArea();
            Collider[] hitColliders = Physics.OverlapSphere(candidatePos, passengerRadius, passengerLayer);
            
            bool hitObstacle = false;
            foreach(var col in hitColliders) if(col != spawnArea) hitObstacle = true;

            if (!hitObstacle)
            {
                targetPos = candidatePos;
                foundValidSpot = true;
                break;
            }
        }

        if (foundValidSpot)
        {
            Vector3 spawnPos = targetPos - cachedPivotLocalPos;
            Quaternion spawnRot = Quaternion.Euler(0, Random.Range(0, 360f), 0);

            GameObject newNPC = Instantiate(npcPrefab, spawnPos, spawnRot);
            newNPC.transform.SetParent(this.transform);
            RandomizeNPCVisuals(newNPC);
            spawnedNPCs.Add(newNPC);
        }
    }

    void RandomizeNPCVisuals(GameObject npc)
    {
        Transform hairPivot = FindDeepChild(npc.transform, "Passenger_HairPivot");
        if (hairPivot != null && hairPivot.childCount > 0)
        {
            int selectedIndex = Random.Range(0, hairPivot.childCount);
            for (int i = 0; i < hairPivot.childCount; i++)
                hairPivot.GetChild(i).gameObject.SetActive(i == selectedIndex);
        }

        Transform head = FindDeepChild(npc.transform, "Passenger_Head");
        Transform body = FindDeepChild(npc.transform, "Passenger_Body");

        if (head != null) ApplyColorForce(head, headColors);
        if (body != null) ApplyColorForce(body, bodyColors);
    }

    void ApplyColorForce(Transform target, Color[] colors)
    {
        Renderer r = target.GetComponent<Renderer>();
        if (r == null || colors.Length == 0) return;
        Color randomColor = colors[Random.Range(0, colors.Length)];
        randomColor.a = 1.0f;
        Material newMat = new Material(r.sharedMaterial);
        if (newMat.HasProperty("_BaseColor")) newMat.SetColor("_BaseColor", randomColor);
        newMat.color = randomColor;
        r.sharedMaterial = newMat;
    }

    Vector3 GetRandomPointInSpawnArea()
    {
        Vector3 center = spawnArea.center;
        Vector3 size = spawnArea.size;
        float margin = passengerRadius; 
        float rangeX = (size.x * 0.5f) - margin;
        float rangeZ = (size.z * 0.5f) - margin;
        if (rangeX < 0) rangeX = 0;
        if (rangeZ < 0) rangeZ = 0;
        float randomX = center.x + Random.Range(-rangeX, rangeX);
        float randomZ = center.z + Random.Range(-rangeZ, rangeZ);
        float bottomY = center.y - (size.y * 0.5f);
        Vector3 localPos = new Vector3(randomX, bottomY, randomZ);
        return spawnArea.transform.TransformPoint(localPos);
    }

    private void CacheFootPivotOffset()
    {
        if (npcPrefab == null) return;
        Transform pivotTrans = FindDeepChild(npcPrefab.transform, footPivotName);
        if (pivotTrans != null)
            cachedPivotLocalPos = npcPrefab.transform.InverseTransformPoint(pivotTrans.position);
        hasCachedPivot = true;
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

    // === 検知・積み込み処理 ===

    void OnValidate() { UpdateColliderSize(); }
    void UpdateColliderSize() {
        if (triggerCollider == null) triggerCollider = GetComponent<SphereCollider>();
        triggerCollider.radius = detectionRange;
        if (lineRenderer != null) DrawCircle();
    }
    void DrawCircle() {
        float angle = 0f;
        for (int i = 0; i < 51; i++) {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * detectionRange;
            float z = Mathf.Cos(Mathf.Deg2Rad * angle) * detectionRange;
            lineRenderer.SetPosition(i, new Vector3(x, 0, z));
            angle += 360f / 50;
        }
    }

    private void OnTriggerEnter(Collider other) {
        Indian_Truck truck = other.GetComponentInParent<Indian_Truck>();
        if (truck != null) { detectedTruck = truck; isTruckNearby = true; }
    }

    private void OnTriggerExit(Collider other) {
        Indian_Truck truck = other.GetComponentInParent<Indian_Truck>();
        if (truck != null) { CancelLoading(); isTruckNearby = false; detectedTruck = null; }
    }

    void Update() {
        if (isLoading) lineRenderer.startColor = lineRenderer.endColor = Color.green;
        else if (isTruckNearby) lineRenderer.startColor = lineRenderer.endColor = Color.yellow;
        else lineRenderer.startColor = lineRenderer.endColor = Color.red;

        if (isTruckNearby && detectedTruck != null) {
            bool isStopped = detectedTruck.CurrentSpeed <= stopThreshold;
            if (isStopped && !isLoading) loadingCoroutine = StartCoroutine(LoadingRoutine());
            else if (!isStopped && isLoading) CancelLoading();
        }
    }

    // ▼▼▼ 変更点：1人ずつ乗せるコルーチン ▼▼▼
    IEnumerator LoadingRoutine()
    {
        isLoading = true;

        // 1. 最初の待機時間
        yield return new WaitForSeconds(rideTime);

        // 2. 1人ずつループ処理で乗せる
        // リストのコピーを取らずに処理しますが、要素を削除するわけではないのでインデックスで回します
        for (int i = 0; i < spawnedNPCs.Count; i++)
        {
            // 安全策：途中でトラックがいなくなったり、動き出したりしたら中断
            if (detectedTruck == null || detectedTruck.CurrentSpeed > stopThreshold)
            {
                CancelLoading();
                yield break; // コルーチン終了
            }

            GameObject npc = spawnedNPCs[i];
            
            if (npc != null)
            {
                // トラックに乗せる
                bool success = detectedTruck.LoadPassenger(npc);
                
                if (success)
                {
                    totalCargoLoaded++; // 乗った数をカウント
                }
            }

            // 次の人が乗るまで待つ（間隔）
            yield return new WaitForSeconds(boardingInterval);
        }

        // 全員乗り終わったら完了処理へ
        CompleteLoading();
    }

    void CancelLoading()
    {
        if (isLoading)
        {
            if (loadingCoroutine != null) StopCoroutine(loadingCoroutine);
            isLoading = false;
        }
    }

    void CompleteLoading()
    {
        // 処理が終わったのでリストをクリア
        spawnedNPCs.Clear();

        Debug.Log($"積み込み完了: 全員({totalCargoLoaded}人)がトラックに移送されました。");
        
        // 役目を終えたバス停を削除
        Destroy(gameObject);
    }
    
    private void OnDrawGizmos() {
        if (isTruckNearby) Gizmos.color = new Color(0, 1, 0, 0.3f);
        else Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawSphere(transform.position, detectionRange);
        if (detectedTruck != null) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, detectedTruck.transform.position);
        }
        if (spawnArea != null) {
            Gizmos.matrix = spawnArea.transform.localToWorldMatrix;
            Gizmos.color = new Color(0, 0, 1, 0.5f);
            Gizmos.DrawWireCube(spawnArea.center, spawnArea.size);
        }
    }
}