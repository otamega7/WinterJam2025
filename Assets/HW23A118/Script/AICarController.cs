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

    float segmentT; // 0〜1 の進行度

    void Start()
    {
        currentIndex = course.GetNearestSegmentIndex(transform.position);
        segmentT = 0f;

        int playerIndex = course.GetNearestSegmentIndex(playerCar.position);
        direction = (playerIndex >= currentIndex) ? 1 : -1;
    }

    void Update()
    {
        MoveAlongSegment();
        CheckDeactivate();
    }

    void MoveAlongSegment()
    {
        int next = (currentIndex + direction + course.Waypoints.Count) % course.Waypoints.Count;

        Vector3 a = course.Waypoints[currentIndex].position;
        Vector3 b = course.Waypoints[next].position;

        Vector3 segment = b - a;
        float length = segment.magnitude;
        Vector3 dir = segment.normalized;

        segmentT += (speed * Time.deltaTime) / length;

        if (segmentT >= 1f)
        {
            segmentT = 0f;
            currentIndex = next;
        }

        Vector3 targetPos = a + segment * segmentT;
        transform.position = targetPos;

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
