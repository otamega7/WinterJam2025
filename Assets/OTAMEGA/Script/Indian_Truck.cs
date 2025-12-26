using UnityEngine;
using System.Collections.Generic;

public class Indian_Truck : MonoBehaviour
{
    [Header("車両設定")]
    private Rigidbody rb;
    // Unity 6以降は linearVelocity, それ以前は velocity
    public float CurrentSpeed => rb != null ? rb.linearVelocity.magnitude : 0f;

    [Header("荷台設定 (Cargo System)")]
    [Tooltip("客が乗る範囲（トラックの荷台に合わせて配置したBoxCollider）")]
    public BoxCollider cargoArea; 
    
    [Tooltip("客同士の距離（重なり防止）")]
    public float passengerRadius = 0.3f;
    
    [Tooltip("配置試行回数")]
    public int maxSpawnAttempts = 30;
    
    [Tooltip("足元の位置合わせ用オブジェクト名")]
    public string footPivotName = "Passenger_FootPivot";

    // 乗っている客のリスト
    private List<GameObject> loadedPassengers = new List<GameObject>();

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// NPCを受け入れて荷台に配置するメソッド
    /// </summary>
    public bool LoadPassenger(GameObject npc)
    {
        if (cargoArea == null)
        {
            Debug.LogError("トラックに CargoArea (BoxCollider) が設定されていません！");
            return false;
        }

        // 1. 荷台の中で空いている場所を探す
        Vector3 targetLocalPos = Vector3.zero;
        bool foundSpot = false;

        // トラックは動いている可能性があるため、計算はすべてローカル座標で行うのが無難ですが、
        // Physics.OverlapSphereはワールド座標を使うため、一時的にワールド座標候補を出して判定します。
        
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector3 candidateWorldPos = GetRandomPointInCargo(cargoArea);
            
            // 重なり判定
            Collider[] hitColliders = Physics.OverlapSphere(candidateWorldPos, passengerRadius);
            bool hitOtherPassenger = false;
            
            foreach (var col in hitColliders)
            {
                // 自分自身（トラック）や荷台の床には反応してもいいが、他の客には反応させない
                // ここでは簡易的に「Passenger」という名前やタグ、またはロード済みリストに含まれるかで判定できます
                // 今回は「ロード済みリストに入っているオブジェクトの近く」ならNGとします
                foreach (var loaded in loadedPassengers)
                {
                    if (loaded == null) continue;
                    if (Vector3.Distance(candidateWorldPos, loaded.transform.position) < passengerRadius * 2)
                    {
                        hitOtherPassenger = true;
                        break;
                    }
                }
            }

            if (!hitOtherPassenger)
            {
                targetLocalPos = cargoArea.transform.InverseTransformPoint(candidateWorldPos);
                foundSpot = true;
                break;
            }
        }

        // 場所が見つからなかった場合、適当な場所（エリアの中心）にする
        if (!foundSpot)
        {
            targetLocalPos = Vector3.zero; // BoxColliderの中心
        }

        // 2. NPCをトラックの子にする（これで一緒に動くようになる）
        npc.transform.SetParent(cargoArea.transform);

        // 3. 位置を適用（足元の補正を入れる）
        Vector3 pivotOffset = GetPivotOffset(npc);
        npc.transform.localPosition = targetLocalPos - pivotOffset;

        // 4. 向きをトラックに合わせる（＋ランダムな揺らぎ）
        // トラックの後ろ向き(180度)や横向きなど、好みに合わせて調整してください
        float randomY = Random.Range(-45f, 45f); 
        npc.transform.localRotation = Quaternion.Euler(0, randomY, 0);

        // 5. 物理演算の干渉を防ぐための処理（必要なら）
        // NPCにColliderがついているとトラックと衝突して荒ぶるので、TriggerにするかLayerを変える
        var npcCollider = npc.GetComponent<Collider>();
        if (npcCollider != null) npcCollider.isTrigger = true;

        // リストに追加
        loadedPassengers.Add(npc);
        
        return true;
    }

    // BoxCollider内のランダムなワールド座標を取得
    private Vector3 GetRandomPointInCargo(BoxCollider box)
    {
        Vector3 center = box.center;
        Vector3 size = box.size;

        float margin = passengerRadius;
        float rx = (size.x * 0.5f) - margin;
        float rz = (size.z * 0.5f) - margin;
        if (rx < 0) rx = 0;
        if (rz < 0) rz = 0;

        // ランダムなローカル座標
        Vector3 randomLocal = new Vector3(
            center.x + Random.Range(-rx, rx),
            center.y - (size.y * 0.5f), // 床面
            center.z + Random.Range(-rz, rz)
        );

        return box.transform.TransformPoint(randomLocal);
    }

    private Vector3 GetPivotOffset(GameObject npc)
    {
        Transform t = FindDeepChild(npc.transform, footPivotName);
        if (t != null)
        {
            return npc.transform.InverseTransformPoint(t.position);
        }
        return Vector3.zero;
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            var result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }

    // 開発用ギズモ
    private void OnDrawGizmos()
    {
        if (cargoArea != null)
        {
            Gizmos.matrix = cargoArea.transform.localToWorldMatrix;
            Gizmos.color = new Color(1, 0.5f, 0, 0.5f);
            Gizmos.DrawWireCube(cargoArea.center, cargoArea.size);
        }
    }
    
    /// <summary>
    /// 乗っている客の数を返す
    /// </summary>
    public int PassengerCount => loadedPassengers.Count;

    /// <summary>
    /// 客を1人降ろす（リストから削除し、GameObjectを返す）
    /// </summary>
    public GameObject UnloadOnePassenger()
    {
        if (loadedPassengers.Count == 0) return null;

        // リストの最後（最後に入った人）から降ろすか、最初（最初に乗った人）から降ろすか
        // ここでは「最後に乗った人が奥にいる」想定で、手前（リストの最後）から降ろします
        int lastIndex = loadedPassengers.Count - 1;
        GameObject npc = loadedPassengers[lastIndex];

        loadedPassengers.RemoveAt(lastIndex);

        // トラックとの親子関係を解除
        npc.transform.SetParent(null);

        return npc;
    }
}