using UnityEngine;

public class NPCGenerator : MonoBehaviour
{
    [Header("Base Settings")]
    public GameObject npcBasePrefab;
    public Transform spawnPoint;

    [Header("Color Settings")]
    public Color[] headColors = new Color[3] { Color.white, Color.gray, Color.black };
    public Color[] bodyColors = new Color[5] { Color.red, Color.blue, Color.green, Color.yellow, Color.cyan };

    public void GenerateNPC()
    {
        if (npcBasePrefab == null || spawnPoint == null)
        {
            Debug.LogError("❌ PrefabまたはSpawnPointが設定されていません！");
            return;
        }

        // 1. 生成
        GameObject npc = Instantiate(npcBasePrefab, spawnPoint.position, spawnPoint.rotation);
        npc.name = "Passenger_" + Random.Range(100, 999);
        
        // --- 修正箇所：パスの変更 ---
        // 画像の階層に合わせて、親フォルダ(Passenger_Model)を含めたパスで探します
        Transform head = npc.transform.Find("Passenger_Model/Passenger_Head");
        Transform body = npc.transform.Find("Passenger_Model/Passenger_Body");
        
        // HairPivotは外にあるようなのでそのまま探します
        Transform hairPivot = npc.transform.Find("Passenger_HairPivot");
        // もし見つからなければ、念のためModelの中も探すように予備検索を入れます
        if (hairPivot == null) hairPivot = npc.transform.Find("Passenger_Model/Passenger_HairPivot");


        // 2. 髪の処理
        if (hairPivot != null && hairPivot.childCount > 0)
        {
            int selectedIndex = Random.Range(0, hairPivot.childCount);
            for (int i = 0; i < hairPivot.childCount; i++)
                hairPivot.GetChild(i).gameObject.SetActive(i == selectedIndex);
        }
        else
        {
            Debug.LogWarning("⚠️ HairPivotが見つかりません。階層を確認してください。");
        }

        // 3. 色の適用
        ApplyColorForce(head, headColors, "Head");
        ApplyColorForce(body, bodyColors, "Body");
    }

    // 強制的に色を適用する関数
    private void ApplyColorForce(Transform target, Color[] colors, string partName)
    {
        if (target == null)
        {
            Debug.LogError($"❌ {partName} が見つかりません！ 'Passenger_Model' の中にあるか確認してください。");
            return;
        }

        Renderer r = target.GetComponent<Renderer>();
        if (r == null)
        {
            Debug.LogError($"❌ {partName} にRendererがついていません！");
            return;
        }

        if (colors.Length == 0) return;

        // 色を抽選
        Color randomColor = colors[Random.Range(0, colors.Length)];
        randomColor.a = 1.0f; // 透明度を強制的に1にする

        // マテリアルを複製して適用
        Material newMat = new Material(r.sharedMaterial);
        newMat.color = randomColor; // .colorはURPでもStandardでも効きます

        // URP用の念押し設定
        if (newMat.HasProperty("_BaseColor")) newMat.SetColor("_BaseColor", randomColor);

        r.sharedMaterial = newMat;

        Debug.Log($"🎨 {partName} の色を {randomColor} に変更しました");
    }
}