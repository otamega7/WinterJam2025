using UnityEngine;

public class HeadStabilizer : MonoBehaviour
{
    public Transform targetBody; // ここに胴体(Torso)をドラッグ＆ドロップ
    public float fixStrength = 5f; // 向きを戻す強さ

    void FixedUpdate()
    {
        if (targetBody == null) return;

        // 胴体の向き（回転）を取得
        Quaternion targetRotation = targetBody.rotation;

        // 現在の向きから、胴体の向きへ、少しずつ回転させる（Lerp）
        Rigidbody rb = GetComponent<Rigidbody>();
        
        // 物理演算に逆らわないようにMoveRotationを使う
        Quaternion nextRot = Quaternion.Lerp(rb.rotation, targetRotation, Time.fixedDeltaTime * fixStrength);
        rb.MoveRotation(nextRot);
    }
}