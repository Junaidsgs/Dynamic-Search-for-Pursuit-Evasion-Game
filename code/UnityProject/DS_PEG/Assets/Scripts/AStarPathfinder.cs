using System.Collections.Generic;
using UnityEngine;

// Simple A* pathfinder and movement script over RoadGraphNodes
public class AStarPathfinder : MonoBehaviour
{
    public WFCCityGenerator city;
    public Transform target;
    public float moveSpeed = 3.5f;
    public float repathRate = 0.5f;

    public List<RoadGraphNode> currentPath = new List<RoadGraphNode>();

    private int pathIndex = 0;
    private float timer = 0f;

    void Start()
    {
        if (city == null)
        {
            city = FindFirstObjectByType<WFCCityGenerator>();
        }
    }

    void Update()
    {
        if (city == null || city.allRoadNodes.Count == 0 || target == null)
            return;

        timer += Time.deltaTime;
        if (timer >= repathRate)
        {
            timer = 0f;
            RoadGraphNode startNode = city.GetClosestNode(transform.position);
            RoadGraphNode goalNode = city.GetClosestNode(target.position);
            currentPath = FindPath(startNode, goalNode);
            pathIndex = 0;
        }

        // Move toward current waypoint node in path
        if (currentPath != null && pathIndex < currentPath.Count)
        {
            RoadGraphNode targetNode = currentPath[pathIndex];
            if (targetNode == null) return;

            Vector3 waypoint = targetNode.WorldPosition;
            waypoint.y = transform.position.y;

            transform.position = Vector3.MoveTowards(transform.position, waypoint, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, waypoint) < 0.15f)
            {
                pathIndex++;
            }
        }
    }

    public List<RoadGraphNode> FindPath(RoadGraphNode start, RoadGraphNode goal)
    {
        if (start == null || goal == null) return new List<RoadGraphNode>();

        List<RoadGraphNode> openList = new List<RoadGraphNode> { start };
        HashSet<RoadGraphNode> closedList = new HashSet<RoadGraphNode>();

        Dictionary<RoadGraphNode, RoadGraphNode> cameFrom = new Dictionary<RoadGraphNode, RoadGraphNode>();
        Dictionary<RoadGraphNode, float> gCost = new Dictionary<RoadGraphNode, float>();
        Dictionary<RoadGraphNode, float> fCost = new Dictionary<RoadGraphNode, float>();

        gCost[start] = 0f;
        fCost[start] = GetHeuristic(start, goal);

        while (openList.Count > 0)
        {
            // Sort openList by fCost so index 0 is always the lowest fCost node
            openList.Sort((a, b) => fCost[a].CompareTo(fCost[b]));
            RoadGraphNode current = openList[0];

            if (current == goal)
            {
                return ReconstructPath(cameFrom, current);
            }

            openList.RemoveAt(0);
            closedList.Add(current);

            // Traverse connected road graph neighbors
            foreach (RoadGraphNode neighbor in current.neighbors)
            {
                if (neighbor == null || closedList.Contains(neighbor))
                    continue;

                float newG = gCost[current] + Vector3.Distance(current.WorldPosition, neighbor.WorldPosition);
                if (!gCost.ContainsKey(neighbor) || newG < gCost[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gCost[neighbor] = newG;
                    fCost[neighbor] = newG + GetHeuristic(neighbor, goal);

                    if (!openList.Contains(neighbor))
                    {
                        openList.Add(neighbor);
                    }
                }
            }
        }

        // TODO: Handle case when start and goal are on disconnected road pieces
        return new List<RoadGraphNode>();
    }

    float GetHeuristic(RoadGraphNode a, RoadGraphNode b)
    {
        return Mathf.Abs(a.WorldPosition.x - b.WorldPosition.x) + Mathf.Abs(a.WorldPosition.z - b.WorldPosition.z);
    }

    List<RoadGraphNode> ReconstructPath(Dictionary<RoadGraphNode, RoadGraphNode> cameFrom, RoadGraphNode current)
    {
        List<RoadGraphNode> path = new List<RoadGraphNode> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }
        path.Reverse();
        return path;
    }

    void OnDrawGizmos()
    {
        if (currentPath == null) return;
        Gizmos.color = Color.red;
        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            if (currentPath[i] != null && currentPath[i + 1] != null)
            {
                Vector3 a = currentPath[i].WorldPosition + Vector3.up * 0.5f;
                Vector3 b = currentPath[i + 1].WorldPosition + Vector3.up * 0.5f;
                Gizmos.DrawLine(a, b);
            }
        }
    }
}
