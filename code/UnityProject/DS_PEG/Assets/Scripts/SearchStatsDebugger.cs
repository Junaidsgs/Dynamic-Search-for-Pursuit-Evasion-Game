using System.Diagnostics;
using UnityEngine;

// Tracks generation and pathfinding performance stats so we can compare A* with other search algorithms later
public class SearchStatsDebugger : MonoBehaviour
{
    public static SearchStatsDebugger Instance;

    [Header("WFC City Stats")]
    public float wfcGenerateTimeMs;
    public int totalRoadNodes;

    [Header("Latest Search / Pathfinding Stats")]
    public string lastAlgorithmName = "A*";
    public float lastSearchTimeMs;
    public int lastNodesExpanded;
    public int lastPathLength;

    [Header("Averages Across Searches")]
    public int totalSearchesRun;
    public float averageSearchTimeMs;
    public float averageNodesExpanded;

    [Header("Display Settings")]
    public bool showOnScreenGUI = true;
    public bool logToConsole = false;

    private Stopwatch _wfcWatch = new Stopwatch();
    private Stopwatch _searchWatch = new Stopwatch();
    private float _cumulativeSearchTimeMs = 0f;
    private int _cumulativeNodesExpanded = 0;

    void Awake()
    {
        Instance = this;
    }

    // --- WFC Timing ---

    public void StartWFCTimer()
    {
        _wfcWatch.Restart();
    }

    public void StopWFCTimer(int roadNodeCount)
    {
        _wfcWatch.Stop();
        wfcGenerateTimeMs = (float)_wfcWatch.Elapsed.TotalMilliseconds;
        totalRoadNodes = roadNodeCount;

        if (logToConsole)
        {
            UnityEngine.Debug.Log($"[WFC Stats] Generated in {wfcGenerateTimeMs:F2} ms | Road Nodes: {totalRoadNodes}");
        }
    }

    // --- Pathfinding / Search Timing ---

    public void StartSearchTimer()
    {
        _searchWatch.Restart();
    }

    public void StopSearchTimer(string algorithmName, int nodesExpanded, int pathLength)
    {
        _searchWatch.Stop();
        lastAlgorithmName = algorithmName;
        lastSearchTimeMs = (float)_searchWatch.Elapsed.TotalMilliseconds;
        lastNodesExpanded = nodesExpanded;
        lastPathLength = pathLength;

        totalSearchesRun++;
        _cumulativeSearchTimeMs += lastSearchTimeMs;
        _cumulativeNodesExpanded += nodesExpanded;

        averageSearchTimeMs = _cumulativeSearchTimeMs / totalSearchesRun;
        averageNodesExpanded = (float)_cumulativeNodesExpanded / totalSearchesRun;

        if (logToConsole)
        {
            UnityEngine.Debug.Log($"[{algorithmName}] Time: {lastSearchTimeMs:F3} ms | Expanded: {nodesExpanded} | Path Len: {pathLength}");
        }
    }

    [ContextMenu("Reset Search Stats")]
    public void ResetSearchStats()
    {
        totalSearchesRun = 0;
        _cumulativeSearchTimeMs = 0f;
        _cumulativeNodesExpanded = 0;
        averageSearchTimeMs = 0f;
        averageNodesExpanded = 0f;
        lastSearchTimeMs = 0f;
        lastNodesExpanded = 0;
        lastPathLength = 0;
    }

    void OnGUI()
    {
        if (!showOnScreenGUI) return;

        GUI.Box(new Rect(10, 10, 290, 165), "WFC & Search Benchmark Stats");

        int y = 35;
        GUI.Label(new Rect(20, y, 270, 20), $"WFC Solve Time: {wfcGenerateTimeMs:F2} ms ({totalRoadNodes} road nodes)");
        y += 22;
        GUI.Label(new Rect(20, y, 270, 20), $"Algorithm: {lastAlgorithmName}");
        y += 20;
        GUI.Label(new Rect(20, y, 270, 20), $"Last Search Time: {lastSearchTimeMs:F4} ms");
        y += 20;
        GUI.Label(new Rect(20, y, 270, 20), $"Nodes Expanded: {lastNodesExpanded}  |  Path Len: {lastPathLength}");
        y += 20;
        GUI.Label(new Rect(20, y, 270, 20), $"Avg Search Time: {averageSearchTimeMs:F4} ms ({totalSearchesRun} runs)");
        y += 20;
        GUI.Label(new Rect(20, y, 270, 20), $"Avg Nodes Expanded: {averageNodesExpanded:F1}");
    }
}
