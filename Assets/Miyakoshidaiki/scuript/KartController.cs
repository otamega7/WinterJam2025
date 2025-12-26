using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class KartController : MonoBehaviour
{
    [Header("Kart Parts")]
    public Transform kartModel;    
    public Transform kartNormal;   
    public Rigidbody sphere;       

    [Header("Movement")]
    public float acceleration = 10f; 
    public float steering = 80f;     
    public float driftSteering = 40f; // ドリフト時の旋回
    public float gravity = 10f;      
    public LayerMask layerMask;      

    [Header("Model Parts")]
    public Transform frontWheels;
    public Transform backWheels;
    public Transform steeringWheel;

    [Header("UI")]
    public TextMeshProUGUI speedText;

    [Header("Camera")]
    public Transform cameraTransform;
    public Vector3 cameraOffset = new Vector3(0, 5, -10);
    public float cameraSmooth = 5f;

    Keyboard keyboard;
    float currentRotate;
    bool drifting = false;
    int driftDirection = 0;

    void Start()
    {
        keyboard = Keyboard.current;
        if (sphere != null)
        {
            sphere.freezeRotation = true; // 球体の自動回転を無効
        }
    }

    void Update()
    {
        if (keyboard == null) return;

        float h = 0f;
        if (keyboard.aKey.isPressed) h = -1f;
        else if (keyboard.dKey.isPressed) h = 1f;

        // --- ドリフト開始 ---
        if (keyboard.spaceKey.wasPressedThisFrame && h != 0)
        {
            drifting = true;
            driftDirection = (int)Mathf.Sign(h); // ← float → int に変換
        }

        // --- ドリフト終了 ---
        if (keyboard.spaceKey.wasReleasedThisFrame)
        {
            drifting = false;
        }

        // --- カートモデルの傾き（Z軸） ---
        float tilt = drifting ? driftDirection * -5f : h * -10f; // ドリフト中は傾き控えめ
        kartModel.localEulerAngles = new Vector3(0, 0, tilt);

        // --- ステアリングホイール ---
        steeringWheel.localEulerAngles = new Vector3(-25, 90, h * 45);

        // --- スピード表示 ---
        if (speedText != null)
            speedText.text = $"Speed: {sphere.linearVelocity.magnitude:0.0}";

        // --- カメラ追従 ---
        if (cameraTransform != null)
        {
            Vector3 targetPos = transform.position - transform.right * cameraOffset.z + Vector3.up * cameraOffset.y;
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, targetPos, Time.deltaTime * cameraSmooth);
            cameraTransform.rotation = Quaternion.Slerp(
                cameraTransform.rotation,
                Quaternion.LookRotation(transform.position - cameraTransform.position),
                Time.deltaTime * cameraSmooth
            );
        }
    }

    void FixedUpdate()
    {
        if (keyboard == null) return;

        float h = 0f;
        if (keyboard.aKey.isPressed) h = -1f;
        else if (keyboard.dKey.isPressed) h = 1f;

        // --- 前進（W） ---
        if (keyboard.wKey.isPressed)
        {
            Vector3 forward = -transform.right; // Xマイナスが前
            sphere.AddForce(forward * acceleration, ForceMode.Acceleration);
        }

        // --- 後退（S） ---
        if (keyboard.sKey.isPressed)
        {
            Vector3 back = transform.right; // 前の逆方向
            sphere.AddForce(back * acceleration * 0.7f, ForceMode.Acceleration);
        }


        // --- 重力 ---
        sphere.AddForce(Vector3.down * gravity, ForceMode.Acceleration);

        // --- 旋回 ---
        float steerAmount = drifting ? driftSteering * driftDirection : steering * h;
        transform.Rotate(0, steerAmount * Time.fixedDeltaTime, 0);

        // --- 地面に合わせる ---
        RaycastHit hitNear;
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hitNear, 2f, layerMask))
        {
            kartNormal.up = Vector3.Lerp(kartNormal.up, hitNear.normal, Time.fixedDeltaTime * 8f);
            kartNormal.Rotate(0, transform.eulerAngles.y, 0);
        }
    }
}
