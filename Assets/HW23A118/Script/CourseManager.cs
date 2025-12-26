using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class CourseManager : MonoBehaviour
{

    [Header("Waypoint Settings")]
    public Transform waypointRoot;

    private List<Transform> waypoints = new List<Transform>();
    public IReadOnlyList<Transform> Waypoints => waypoints;

    // WP_000 の 000 部分を取得
    private static readonly Regex indexRegex = new Regex(@"\d+");

    void Awake()
    {
        BuildWaypointList();
    }

    void BuildWaypointList()
    {
        waypoints.Clear();

        Dictionary<int, List<Transform>> indexMap = new Dictionary<int, List<Transform>>();

        foreach (Transform child in waypointRoot)
        {
            int index = ExtractIndex(child.name);

            if (index < 0)
            {
                Debug.LogError(
                    $"[CourseManager] Waypoint name has no index: {child.name}",
                    child
                );
                continue;
            }

            if (!indexMap.ContainsKey(index))
                indexMap[index] = new List<Transform>();

            indexMap[index].Add(child);
        }

        // 番号被りチェック
        foreach (var pair in indexMap)
        {
            if (pair.Value.Count > 1)
            {
                string names = string.Join(", ", pair.Value.ConvertAll(t => t.name));
                Debug.LogError(
                    $"[CourseManager] Duplicate waypoint index {pair.Key}: {names}",
                    waypointRoot
                );
            }
        }

        // ソートして List 化
        List<int> keys = new List<int>(indexMap.Keys);
        keys.Sort();

        foreach (int key in keys)
        {
            // 被っていても先頭のみ使う（動作は継続）
            waypoints.Add(indexMap[key][0]);
        }

        if (waypoints.Count == 0)
        {
            Debug.LogError("[CourseManager] No valid waypoints found.");
        }
    }

    int ExtractIndex(string name)
    {
        Match match = indexRegex.Match(name);
        if (!match.Success)
            return -1;

        return int.Parse(match.Value);
    }

    /// <summary>
    /// position が属する「最も近い線分 index」を返す
    /// （index と index+1 の間）
    /// </summary>
    public int GetNearestSegmentIndex(Vector3 position)
    {
        float minDist = float.MaxValue;
        int nearestIndex = 0;

        for (int i = 0; i < Waypoints.Count; i++)
        {
            int next = (i + 1) % Waypoints.Count;

            Vector3 a = Waypoints[i].position;
            Vector3 b = Waypoints[next].position;

            Vector3 closest = GetClosestPointOnLineSegment(a, b, position);
            float dist = Vector3.Distance(position, closest);

            if (dist < minDist)
            {
                minDist = dist;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    /// <summary>
    /// 線分 AB 上の position に最も近い点を返す
    /// </summary>
    Vector3 GetClosestPointOnLineSegment(Vector3 a, Vector3 b, Vector3 position)
    {
        Vector3 ab = b - a;
        float t = Vector3.Dot(position - a, ab) / ab.sqrMagnitude;
        t = Mathf.Clamp01(t);
        return a + ab * t;
    }

    /// <summary>
    /// index 番目の線分の進行方向ベクトル
    /// </summary>
    public Vector3 GetSegmentDirection(int index)
    {
        int next = (index + 1) % Waypoints.Count;
        return (Waypoints[next].position - Waypoints[index].position).normalized;
    }
}
