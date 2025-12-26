using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class CourseManager : MonoBehaviour
{
    [Header("Waypoint Settings")]
    public Transform waypointRoot;

    public List<Transform> waypoints = new List<Transform>();
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

    public int GetNearestIndex(Vector3 position)
    {
        int nearest = 0;
        float minDist = float.MaxValue;

        for (int i = 0; i < waypoints.Count; i++)
        {
            float dist = Vector3.Distance(position, waypoints[i].position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = i;
            }
        }
        return nearest;
    }
}
