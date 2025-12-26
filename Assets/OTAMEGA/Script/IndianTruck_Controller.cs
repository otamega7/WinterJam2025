using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class IndianTruck_Controller : MonoBehaviour
{
    [Header("Kart Parts")]
    public Transform kartModel;
    public Transform kartNormal;
    public Rigidbody sphere;

    [Header("Movement Settings")]
    public float acceleration = 30f;   // 前進する力
    public float reversePower = 20f;   // バックする力（少し弱めにするのが一般的）
    public float brakePower = 60f;     // ブレーキの強さ（アクセルより強くすると止まりやすい）
    public float steering = 60f;
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

    [Header("Sound")]
    public AudioSource engineSound;
    public float minPitch = 1.0f;
    public float maxPitch = 2.0f;
    public float maxSpeedForPitch = 20f;

    Keyboard keyboard;

    void Start()
    {
        keyboard = Keyboard.current;
        if (sphere != null)
        {
            sphere.freezeRotation = true;
        }
    }

    void Update()
    {
        if (keyboard == null) return;

        float h = 0f;
        if (keyboard.aKey.isPressed) h = -1f;
        else if (keyboard.dKey.isPressed) h = 1f;

        // --- 車体の傾き ---
        float tilt = h * -5f;
        if (kartModel != null)
        {
            kartModel.localEulerAngles = new Vector3(0, 0, tilt);
        }

        // --- ステアリングホイール ---
        if (steeringWheel != null)
        {
            steeringWheel.localEulerAngles = new Vector3(25, 0, -h * 45);
        }

        // 速度取得
        float currentSpeed = sphere.linearVelocity.magnitude;

        // --- スピード表示 ---
        if (speedText != null)
        {
            speedText.text = $"Speed: {currentSpeed:0.0}";
        }

        // --- エンジン音のピッチ制御 ---
        if (engineSound != null)
        {
            engineSound.pitch = Mathf.Lerp(minPitch, maxPitch, currentSpeed / maxSpeedForPitch);
        }

        // --- カメラ追従 ---
        if (cameraTransform != null)
        {
            Vector3 targetPos =
                transform.position
                + transform.forward * cameraOffset.z
                + transform.up * cameraOffset.y;

            cameraTransform.position = Vector3.Lerp(
                cameraTransform.position,
                targetPos,
                Time.deltaTime * cameraSmooth
            );

            cameraTransform.rotation = Quaternion.Slerp(
                cameraTransform.rotation,
                Quaternion.LookRotation(transform.position - cameraTransform.position),
                Time.deltaTime * cameraSmooth
            );
        }

        // --- タイヤ回転 ---
        float dot = Vector3.Dot(sphere.linearVelocity, transform.forward);
        float moveDir = (dot > 0.1f) ? 1f : (dot < -0.1f) ? -1f : 0f;
        
        // 入力に合わせてタイヤを回す（見た目用）
        if (keyboard.wKey.isPressed) moveDir = 1f;
        else if (keyboard.sKey.isPressed) moveDir = -1f;

        if (moveDir != 0f)
        {
            // 速度に応じた回転（停止寸前はゆっくり）
            float wheelRotateSpeed = currentSpeed * 360f * Time.deltaTime * moveDir;
            if (currentSpeed < 0.5f) wheelRotateSpeed = 360f * Time.deltaTime * moveDir;

            Quaternion rot = Quaternion.Euler(wheelRotateSpeed, 0, 0);

            if (frontWheels != null) frontWheels.localRotation *= rot;
            if (backWheels != null)  backWheels.localRotation  *= rot;
        }
    }

    void FixedUpdate()
    {
        if (keyboard == null) return;

        float h = 0f;
        if (keyboard.aKey.isPressed) h = -1f;
        else if (keyboard.dKey.isPressed) h = 1f;

        // 現在の進行方向の速度成分を取得（+なら前進中、-なら後退中）
        float forwardSpeed = Vector3.Dot(sphere.linearVelocity, transform.forward);

        // --- アクセル（W） ---
        if (keyboard.wKey.isPressed)
        {
            // 前進力
            sphere.AddForce(transform.forward * acceleration, ForceMode.Acceleration);
        }

        // --- ブレーキ＆バック（S） ---
        if (keyboard.sKey.isPressed)
        {
            if (forwardSpeed > 1.0f) 
            {
                // 前に進んでいる時は「ブレーキ」として強く止める
                sphere.AddForce(-transform.forward * brakePower, ForceMode.Acceleration);
            }
            else
            {
                // 止まっている、または後ろに進んでいる時は「バック」として動かす
                sphere.AddForce(-transform.forward * reversePower, ForceMode.Acceleration);
            }
        }

        sphere.AddForce(Vector3.down * gravity, ForceMode.Acceleration);

        // --- 旋回 ---
        float steerAmount = steering * h;
        // バック中はハンドル操作を逆にする（リアル挙動）※お好みでコメントアウト可
        if (forwardSpeed < -1.0f) steerAmount *= -1f;

        transform.Rotate(0, steerAmount * Time.fixedDeltaTime, 0);

        // --- 接地 ---
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, 2f, layerMask))
        {
            if (kartNormal != null)
            {
                kartNormal.up = Vector3.Lerp(
                    kartNormal.up,
                    hit.normal,
                    Time.fixedDeltaTime * 8f
                );
                kartNormal.Rotate(0, transform.eulerAngles.y, 0);
            }
        }
    }
}