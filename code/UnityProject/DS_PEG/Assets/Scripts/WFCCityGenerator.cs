using System.Collections.Generic;
using UnityEngine;

public class WFCCityGenerator : MonoBehaviour
{
    // 3 Socket types so straight roads / corners force actual Pavement tiles on their curb sides:
    // - Pavement: edge of a Pavement tile (can touch Pavement or RoadCurb)
    // - RoadCurb: non-road side of a road tile (MUST touch Pavement, so two roads can't spawn side-by-side)
    // - RoadOpen: active road end (MUST touch another RoadOpen)
    public enum Socket
    {
        Pavement = 0,
        RoadCurb = 1,
        RoadOpen = 2
    }

    [System.Serializable]
    public class TileData
    {
        public string name;
        public GameObject prefab;
        public float yRotation;
        public bool isRoad;
        // Sockets: [0]=North(+Z), [1]=East(+X), [2]=South(-Z), [3]=West(-X)
        public Socket[] sockets = new Socket[4];
    }

    public int gridWidth = 10;
    public int gridLength = 10;
    public float tileSize = 2.0f;

    [Header("Tile Prefabs")]
    public GameObject pavementPrefab;
    public GameObject straightRoadPrefab;
    public GameObject cornerRoadPrefab;
    public GameObject tJunctionPrefab;
    public GameObject xJunctionPrefab;
    public GameObject[] buildingPrefabs;

    [Header("Agents & Goal (Placed on random road tiles)")]
    public Transform pursuer;
    public Transform evader;
    public Transform goal;

    [Header("Generated Road Graph")]
    public List<RoadGraphNode> allRoadNodes = new List<RoadGraphNode>();
    public bool showGraphGizmos = true;

    private List<TileData> tileList = new List<TileData>();
    private RoadGraphNode[,] nodeGrid;

    // Directions: 0=North(+Z), 1=East(+X), 2=South(-Z), 3=West(-X)
    private int[] dx = { 0, 1, 0, -1 };
    private int[] dz = { 1, 0, -1, 0 };
    private int[] opposite = { 2, 3, 0, 1 };

    void Start()
    {
        SetupTiles();
        GenerateCity();
    }

    void SetupTiles()
    {
        tileList.Clear();

        Socket P = Socket.Pavement;
        Socket C = Socket.RoadCurb;
        Socket R = Socket.RoadOpen;

        // Pavement tile (all 4 sides are Pavement)
        AddTile("Pavement", pavementPrefab, 0, false, P, P, P, P);

        // Straight roads (RoadOpen on front/back, RoadCurb on left/right so pavements must spawn beside them)
        AddTile("Straight_NS", straightRoadPrefab, 0, true, R, C, R, C);
        AddTile("Straight_EW", straightRoadPrefab, 90, true, C, R, C, R);

        // Corners (KayKit default corner connects North and West at 0 deg)
        AddTile("Corner_NW", cornerRoadPrefab, 0, true, R, C, C, R);
        AddTile("Corner_NE", cornerRoadPrefab, 90, true, R, R, C, C);
        AddTile("Corner_SE", cornerRoadPrefab, 180, true, C, R, R, C);
        AddTile("Corner_SW", cornerRoadPrefab, 270, true, C, C, R, R);

        // T-Junctions (KayKit default tsplit connects North, South, West at 0 deg)
        AddTile("TJunc_NSW", tJunctionPrefab, 0, true, R, C, R, R);
        AddTile("TJunc_NEW", tJunctionPrefab, 90, true, R, R, C, R);
        AddTile("TJunc_NES", tJunctionPrefab, 180, true, R, R, R, C);
        AddTile("TJunc_ESW", tJunctionPrefab, 270, true, C, R, R, R);

        // 4-way X-Junction
        AddTile("XJunc", xJunctionPrefab, 0, true, R, R, R, R);
    }

    void AddTile(string name, GameObject prefab, float rot, bool isRoad, Socket n, Socket e, Socket s, Socket w)
    {
        if (prefab == null) return;

        TileData t = new TileData();
        t.name = name;
        t.prefab = prefab;
        t.yRotation = rot;
        t.isRoad = isRoad;
        t.sockets = new Socket[] { n, e, s, w };
        tileList.Add(t);
    }

    // Checks if two touching edges are valid
    bool AreSocketsCompatible(Socket a, Socket b)
    {
        // A road opening must connect to another road opening
        if (a == Socket.RoadOpen || b == Socket.RoadOpen)
            return a == Socket.RoadOpen && b == Socket.RoadOpen;

        // Two road curbs cannot touch each other (forces a Pavement tile on the left/right of straight roads)
        if (a == Socket.RoadCurb && b == Socket.RoadCurb)
            return false;

        return true;
    }

    [ContextMenu("Generate City")]
    public void GenerateCity()
    {
        if (SearchStatsDebugger.Instance != null)
            SearchStatsDebugger.Instance.StartWFCTimer();

        SetupTiles();
        if (tileList.Count == 0) return;

        // Clear old tiles if regenerating
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
        allRoadNodes.Clear();
        nodeGrid = new RoadGraphNode[gridWidth, gridLength];

        List<int>[,] possible = new List<int>[gridWidth, gridLength];
        int[,] chosenGrid = new int[gridWidth, gridLength];

        // 1. Initialize every cell with all possible tiles
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                chosenGrid[x, z] = -1;
                possible[x, z] = new List<int>();
                for (int i = 0; i < tileList.Count; i++)
                {
                    possible[x, z].Add(i);
                }
            }
        }

        // 2. WFC loop: pick cell with fewest options and collapse it
        for (int step = 0; step < gridWidth * gridLength; step++)
        {
            int bestX = -1, bestZ = -1;
            int minCount = 999;

            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridLength; z++)
                {
                    if (chosenGrid[x, z] == -1 && possible[x, z].Count < minCount)
                    {
                        minCount = possible[x, z].Count;
                        bestX = x;
                        bestZ = z;
                    }
                }
            }

            if (bestX == -1) break;

            List<int> options = possible[bestX, bestZ];
            int chosenIndex = (options.Count > 0) ? options[Random.Range(0, options.Count)] : 0;

            chosenGrid[bestX, bestZ] = chosenIndex;
            possible[bestX, bestZ].Clear();
            possible[bestX, bestZ].Add(chosenIndex);

            ConstrainNeighbors(possible, bestX, bestZ, chosenIndex);
        }

        // 3. Spawn tiles and attach RoadGraphNode to road pieces
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                int idx = chosenGrid[x, z] >= 0 ? chosenGrid[x, z] : 0;
                TileData tile = tileList[idx];

                Vector3 pos = transform.position + new Vector3(x * tileSize, 0f, z * tileSize);
                Quaternion rot = Quaternion.Euler(0f, tile.yRotation, 0f);
                GameObject obj = Instantiate(tile.prefab, pos, rot, transform);
                obj.name = $"{tile.name}_({x},{z})";

                if (tile.isRoad)
                {
                    RoadGraphNode node = obj.AddComponent<RoadGraphNode>();
                    node.gridPos = new Vector2Int(x, z);
                    nodeGrid[x, z] = node;
                    allRoadNodes.Add(node);
                }
                else if (buildingPrefabs != null && buildingPrefabs.Length > 0 && Random.value < 0.6f)
                {
                    GameObject b = buildingPrefabs[Random.Range(0, buildingPrefabs.Length)];
                    if (b != null) Instantiate(b, pos, Quaternion.identity, obj.transform);
                }
            }
        }

        // 4. Connect road graph nodes along valid RoadOpen sockets
        BuildRoadGraph(chosenGrid);

        if (SearchStatsDebugger.Instance != null)
            SearchStatsDebugger.Instance.StopWFCTimer(allRoadNodes.Count);

        // 5. Place Pursuer, Evader, and Goal on random road nodes
        SpawnObjectsOnRoads();
    }

    void SpawnObjectsOnRoads()
    {
        if (allRoadNodes.Count == 0) return;

        // Auto-find by name if not dragged into Inspector
        if (pursuer == null)
        {
            GameObject p = GameObject.Find("Pursuer");
            if (p != null) pursuer = p.transform;
        }
        if (evader == null)
        {
            GameObject e = GameObject.Find("Evader");
            if (e != null) evader = e.transform;
        }
        if (goal == null)
        {
            GameObject g = GameObject.Find("Goal");
            if (g != null) goal = g.transform;
        }

        // Find the largest connected group of road nodes so Pursuer, Evader, and Goal are always on the same connected road network
        List<RoadGraphNode> mainRoadGroup = GetLargestConnectedRoadGroup();
        if (mainRoadGroup.Count == 0) return;

        RoadGraphNode pNode = mainRoadGroup[Random.Range(0, mainRoadGroup.Count)];
        RoadGraphNode eNode = mainRoadGroup[Random.Range(0, mainRoadGroup.Count)];
        RoadGraphNode gNode = mainRoadGroup[Random.Range(0, mainRoadGroup.Count)];

        // Make sure they don't pick the exact same tile if there are enough road tiles
        if (mainRoadGroup.Count >= 3)
        {
            while (eNode == pNode)
                eNode = mainRoadGroup[Random.Range(0, mainRoadGroup.Count)];

            while (gNode == pNode || gNode == eNode)
                gNode = mainRoadGroup[Random.Range(0, mainRoadGroup.Count)];
        }

        if (pursuer != null)
            pursuer.position = new Vector3(pNode.WorldPosition.x, pursuer.position.y, pNode.WorldPosition.z);

        if (evader != null)
            evader.position = new Vector3(eNode.WorldPosition.x, evader.position.y, eNode.WorldPosition.z);

        if (goal != null)
            goal.position = new Vector3(gNode.WorldPosition.x, goal.position.y, gNode.WorldPosition.z);
    }

    // Simple BFS to find the largest connected road component
    List<RoadGraphNode> GetLargestConnectedRoadGroup()
    {
        List<RoadGraphNode> largestGroup = new List<RoadGraphNode>();
        HashSet<RoadGraphNode> visited = new HashSet<RoadGraphNode>();

        foreach (RoadGraphNode startNode in allRoadNodes)
        {
            if (startNode == null || visited.Contains(startNode))
                continue;

            List<RoadGraphNode> group = new List<RoadGraphNode>();
            Queue<RoadGraphNode> queue = new Queue<RoadGraphNode>();

            visited.Add(startNode);
            queue.Enqueue(startNode);

            while (queue.Count > 0)
            {
                RoadGraphNode curr = queue.Dequeue();
                group.Add(curr);

                foreach (RoadGraphNode neighbor in curr.neighbors)
                {
                    if (neighbor != null && !visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            if (group.Count > largestGroup.Count)
            {
                largestGroup = group;
            }
        }

        return largestGroup;
    }

    void ConstrainNeighbors(List<int>[,] possible, int cx, int cz, int chosenIndex)
    {
        TileData chosenTile = tileList[chosenIndex];

        for (int dir = 0; dir < 4; dir++)
        {
            int nx = cx + dx[dir];
            int nz = cz + dz[dir];

            if (nx < 0 || nx >= gridWidth || nz < 0 || nz >= gridLength)
                continue;

            Socket mySocket = chosenTile.sockets[dir];
            int oppDir = opposite[dir];

            List<int> neighborOpts = possible[nx, nz];
            for (int i = neighborOpts.Count - 1; i >= 0; i--)
            {
                TileData neighborTile = tileList[neighborOpts[i]];
                if (!AreSocketsCompatible(mySocket, neighborTile.sockets[oppDir]))
                {
                    neighborOpts.RemoveAt(i);
                }
            }
        }
    }

    void BuildRoadGraph(int[,] chosenGrid)
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                RoadGraphNode node = nodeGrid[x, z];
                if (node == null) continue;

                TileData tile = tileList[chosenGrid[x, z]];

                for (int dir = 0; dir < 4; dir++)
                {
                    // Only connect to neighbor if our tile has a RoadOpen socket in this direction
                    if (tile.sockets[dir] != Socket.RoadOpen) continue;

                    int nx = x + dx[dir];
                    int nz = z + dz[dir];
                    if (nx < 0 || nx >= gridWidth || nz < 0 || nz >= gridLength) continue;

                    RoadGraphNode neighbor = nodeGrid[nx, nz];
                    TileData neighborTile = tileList[chosenGrid[nx, nz]];

                    if (neighbor != null && neighborTile.sockets[opposite[dir]] == Socket.RoadOpen)
                    {
                        node.neighbors.Add(neighbor);
                    }
                }
            }
        }
    }

    public RoadGraphNode GetClosestNode(Vector3 worldPos)
    {
        RoadGraphNode best = null;
        float bestDist = float.MaxValue;

        foreach (RoadGraphNode node in allRoadNodes)
        {
            if (node == null) continue;
            float d = Vector3.Distance(worldPos, node.WorldPosition);
            if (d < bestDist)
            {
                bestDist = d;
                best = node;
            }
        }
        return best;
    }

    void OnDrawGizmos()
    {
        if (!showGraphGizmos || allRoadNodes == null) return;

        Vector3 offset = Vector3.up * 0.3f;
        foreach (RoadGraphNode node in allRoadNodes)
        {
            if (node == null) continue;

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(node.WorldPosition + offset, 0.2f);

            Gizmos.color = Color.white;
            foreach (RoadGraphNode n in node.neighbors)
            {
                if (n != null)
                {
                    Gizmos.DrawLine(node.WorldPosition + offset, n.WorldPosition + offset);
                }
            }
        }
    }
}
