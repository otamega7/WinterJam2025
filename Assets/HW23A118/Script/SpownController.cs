using UnityEngine;

public class SpownController : MonoBehaviour
{
    [SerializeField] GameObject prefab_Enemy;
    public CourseManager course;
    Vector2 center = new Vector2(10, 5); // 中心座標
    float radius = 3.0f; // 半径
    private Vector2 randomPos;
    private int currentIndex;
    void Start()
    {
        randomPos = center + Random.insideUnitCircle * radius;
        currentIndex = course.GetNearestSegmentIndex(transform.position);
    }
    // Update is called once per frame
    void Update()
    {
        Instantiate(prefab_Enemy);
    }
}
