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
    public float driftSteering = 40f;
    public float gravity = 10f;
    public LayerMask layerMask;

    [Header("Model Parts")]
    public Transform frontWheelL;
    public Transform frontWheelR;
    public Transform backWheelL;
    public Transform backWheelR;
    public Transform steeringWheel;

    [Header("UI")]
    public TextMeshProUGUI speedText;

    [Header("Camera")]
    public Transform cameraTransform;
    public Vector3 cameraOffset = new Vector3(0, 5, -10);
    public float cameraSmooth = 5f;

    Keyboard keyboard;
    bool drifting = false;
    int driftDirection = 0;

    void Start()
    {
        keyboard = Keyboard.current;

        if (sphere != null)
        {
            sphere.freezeRotation = true; // 球体の回転は使わない
        }
    }

    void Update()
    {
        if (keyboard == null) return;

        // --- 左右入力 ---
        float h = 0f;
        if (keyboard.aKey.isPressed) h = -1f;
        else if (keyboard.dKey.isPressed) h = 1f;

        // --- ドリフト開始 ---
        if (keyboard.spaceKey.wasPressedThisFrame && h != 0)
        {
            drifting = true;
            driftDirection = (int)Mathf.Sign(h);
        }

        // --- ドリフト終了 ---
        if (keyboard.spaceKey.wasReleasedThisFrame)
        {
            drifting = false;
        }

        // --- カートモデルの傾き ---
        float tilt = drifting ? driftDirection * -5f : h * -10f;
        kartModel.localEulerAngles = new Vector3(0, 0, tilt);

        // --- ステアリングホイール ---
        if (steeringWheel != null)
        {
            steeringWheel.localEulerAngles = new Vector3(-25, 90, h * 45);
        }

        // --- スピード表示 ---
        if (speedText != null)
        {
            speedText.text = $"Speed: {sphere.linearVelocity.magnitude:0.0}";
        }

        // --- カメラ追従（※ここは変更なし） ---
        if (cameraTransform != null)
        {
            Vector3 targetPos =
                transform.position
                - transform.right * cameraOffset.z
                + Vector3.up * cameraOffset.y;

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

        // --- タイヤ回転（-X前進基準 / 回転軸Z） ---
        float speed = sphere.linearVelocity.magnitude;
        float wheelRotateSpeed = speed * 360f * Time.deltaTime;

        float wheelDir = 0f;
        if (keyboard.wKey.isPressed) wheelDir = 1f; // 前進
        else if (keyboard.sKey.isPressed) wheelDir = -1f; // 後退

        if (wheelDir != 0f)
        {
            Quaternion rot = Quaternion.Euler(0, 0, wheelRotateSpeed * wheelDir);

            if (frontWheelL != null) frontWheelL.localRotation *= rot;
            if (frontWheelR != null) frontWheelR.localRotation *= rot;
            if (backWheelL != null)  backWheelL.localRotation  *= rot;
            if (backWheelR != null)  backWheelR.localRotation  *= rot;
        }
    }

    void FixedUpdate()
    {
        if (keyboard == null) return;

        // --- 左右入力 ---
        float h = 0f;
        if (keyboard.aKey.isPressed) h = -1f;
        else if (keyboard.dKey.isPressed) h = 1f;

        // --- 前進（W）---
        if (keyboard.wKey.isPressed)
        {
            Vector3 forward = -transform.right; // -X が前
            sphere.AddForce(forward * acceleration, ForceMode.Acceleration);
        }

        // --- 後退（S）---
        if (keyboard.sKey.isPressed)
        {
            Vector3 back = transform.right;
            sphere.AddForce(back * acceleration * 0.7f, ForceMode.Acceleration);
        }

        // --- 重力 ---
        sphere.AddForce(Vector3.down * gravity, ForceMode.Acceleration);

        // --- 旋回 ---
        float steerAmount = drifting
            ? driftSteering * driftDirection
            : steering * h;

        transform.Rotate(0, steerAmount * Time.fixedDeltaTime, 0);

        // --- 地面に合わせる ---
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, 2f, layerMask))
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
