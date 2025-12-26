using UnityEngine;

public class HeadStabilizerTest : MonoBehaviour
{
    public Transform targetBody; // ここに胴体(Torso)をドラッグ＆ドロップ
    public float fixStrength = 5f; // 向きを戻す強さ

    public Vector3 force;

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

        Vector3 rbV = GetComponent<Rigidbody>().linearVelocity * Time.fixedDeltaTime;
        Quaternion addRot = Quaternion.Euler(rbV.z * fixStrength, 0, rbV.x * fixStrength);
        rb.MoveRotation(addRot);

        force = rbV;
    }
}