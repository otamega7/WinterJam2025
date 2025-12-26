using UnityEngine;
// System.Collections.Generic は今回使っていないので削除してもOK

public class NPCGenerator : MonoBehaviour
{
    [Header("Base Settings")]
    [Tooltip("PassengerのルートPrefabを指定")]
    public GameObject npcBasePrefab;
    [Tooltip("生成する場所")]
    public Transform spawnPoint;

    [Header("Random Elements")]
    [Tooltip("髪の毛のPrefabリスト (Hair01～03)")]
    public GameObject[] hairPrefabs;
    
    [Tooltip("頭の色リスト (3色)")]
    public Color[] headColors = new Color[3] { Color.white, Color.grey, Color.black };
    
    [Tooltip("胴体の色リスト (5色)")]
    public Color[] bodyColors = new Color[5] { Color.red, Color.blue, Color.green, Color.yellow, Color.cyan };

    // URPの場合は "_BaseColor" 、Standardパイプラインの場合は "_Color" が一般的です
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color"); 
    // ※もし色が反映されない場合は、上の "_Color" を "_BaseColor" に書き換えてください

    public void GenerateNPC()
    {
        if (npcBasePrefab == null || spawnPoint == null)
        {
            Debug.LogError("PrefabまたはSpawnPointが設定されていません。");
            return;
        }

        // 1. ベースのNPCを生成
        GameObject npc = Instantiate(npcBasePrefab, spawnPoint.position, spawnPoint.rotation);
        npc.name = "Passenger_" + Random.Range(100, 999);

        // 2. 各パーツのTransformを取得
        Transform hairPivot = npc.transform.Find("Passenger_HairPivot");
        Transform head = npc.transform.Find("Passenger_Head");
        Transform body = npc.transform.Find("Passenger_Body");

        // 3. 髪の毛の抽選と生成
        if (hairPivot != null && hairPrefabs.Length > 0)
        {
            GameObject selectedHair = hairPrefabs[Random.Range(0, hairPrefabs.Length)];
            if (selectedHair != null)
            {
                GameObject hair = Instantiate(selectedHair, hairPivot);
                hair.transform.localPosition = Vector3.zero;
                hair.transform.localRotation = Quaternion.identity;
            }
        }

        // PropertyBlockの準備（これを使って色を上書きします）
        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

        // 4. 頭の色の抽選
        if (head != null && headColors.Length > 0)
        {
            Renderer headRenderer = head.GetComponent<Renderer>();
            if (headRenderer != null)
            {
                // 現在のプロパティを取得
                headRenderer.GetPropertyBlock(propBlock);
                // 色をセット
                propBlock.SetColor(ColorPropertyId, headColors[Random.Range(0, headColors.Length)]);
                // Rendererに適用
                headRenderer.SetPropertyBlock(propBlock);
            }
        }

        // 5. 胴体の色の抽選
        if (body != null && bodyColors.Length > 0)
        {
            Renderer bodyRenderer = body.GetComponent<Renderer>();
            if (bodyRenderer != null)
            {
                bodyRenderer.GetPropertyBlock(propBlock);
                propBlock.SetColor(ColorPropertyId, bodyColors[Random.Range(0, bodyColors.Length)]);
                bodyRenderer.SetPropertyBlock(propBlock);
            }
        }
        
        Debug.Log("NPCを生成しました（MaterialPropertyBlock使用）");
    }
}