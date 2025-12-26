using UnityEngine;

public class AICarController : MonoBehaviour
{
    public CourseManager course;
    public Transform playerCar;

    [Header("Movement")]
    public float speed = 5f;
    public float turnSpeed = 5f;
    public float reachDistance = 1.5f;

    [Header("Deactivate")]
    public float deactivateDistance = 50f;

    private int currentIndex;
    private int direction; // 1: forward, -1: backward
    private bool initialized = false;

    void Start()
    {
        InitializeDirection();
    }

    void Update()
    {
        if (!initialized)
            return;

        CheckDeactivate();
        MoveAlongCourse();
    }

    /// <summary>
    /// 最初に一度だけ前後判定を行う
    /// </summary>
    void InitializeDirection()
    {
        currentIndex = course.GetNearestIndex(transform.position);
        int playerIndex = course.GetNearestIndex(playerCar.position);

        if (playerIndex > currentIndex)
            direction = 1;
        else
            direction = -1;

        initialized = true;
    }

    void MoveAlongCourse()
    {
        Transform target = course.Waypoints[currentIndex];
        float dist = Vector3.Distance(transform.position, target.position);

        if (dist < reachDistance)
        {
            currentIndex += direction;

            if (currentIndex < 0)
                currentIndex = course.Waypoints.Count - 1;
            else if (currentIndex >= course.Waypoints.Count)
                currentIndex = 0;
        }

        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        if (dir != Vector3.zero)
        {
            Quaternion rot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                rot,
                turnSpeed * Time.deltaTime
            );
        }
    }

    /// <summary>
    /// プレイヤーから一定距離離れたら非アクティブ化
    /// </summary>
    void CheckDeactivate()
    {
        float dist = Vector3.Distance(transform.position, playerCar.position);
        if (dist > deactivateDistance)
        {
            gameObject.SetActive(false);
        }
    }
}
