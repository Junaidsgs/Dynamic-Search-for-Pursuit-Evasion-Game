using System.Collections.Generic;
using UnityEngine;

// Attached to each spawned road tile so A* only walks along connected roads
public class RoadGraphNode : MonoBehaviour
{
    public Vector2Int gridPos;
    public List<RoadGraphNode> neighbors = new List<RoadGraphNode>();

    public Vector3 WorldPosition => transform.position;
}
