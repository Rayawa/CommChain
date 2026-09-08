namespace CommChain.App.Topology;

/// <summary>
/// 网络拓扑图管理：以邻接矩阵表示小型网络，支持从文件加载、
/// Dijkstra 单源最短路径与 Floyd-Warshall 全源最短路径。
/// 考察点（第三阶段）：图论、Dijkstra 算法、面向对象编程、文件 I/O。
/// </summary>
public sealed class NetworkTopology
{
    public const int Infinity = int.MaxValue / 4;   // 表示无边（避免加法溢出）

    private int[,] _adjacency = null!;

    /// <summary>节点数。</summary>
    public int NodeCount { get; private set; }

    /// <summary>邻接矩阵只读视图。</summary>
    public int[,] Adjacency => (int[,])_adjacency.Clone();

    /// <summary>
    /// 从文本文件加载拓扑。
    /// 文件格式：第一行为节点数 N；之后每行一条边 "u v weight"（节点编号 0~N-1，空格分隔，# 开头为注释）。
    /// </summary>
    public static NetworkTopology LoadFromFile(string path)
    {
        var lines = File.ReadAllLines(path)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith('#'))
            .ToList();

        if (lines.Count == 0)
            throw new InvalidDataException($"拓扑文件 {path} 为空。");

        if (!int.TryParse(lines[0], out int nodeCount) || nodeCount <= 0)
            throw new InvalidDataException($"拓扑文件 {path} 第一行节点数非法。");

        var topology = new NetworkTopology { NodeCount = nodeCount };
        topology._adjacency = new int[nodeCount, nodeCount];
        for (int i = 0; i < nodeCount; i++)
            for (int j = 0; j < nodeCount; j++)
                topology._adjacency[i, j] = i == j ? 0 : Infinity;

        for (int lineIndex = 1; lineIndex < lines.Count; lineIndex++)
        {
            var parts = lines[lineIndex].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3 ||
                !int.TryParse(parts[0], out int u) ||
                !int.TryParse(parts[1], out int v) ||
                !int.TryParse(parts[2], out int weight))
                throw new InvalidDataException($"拓扑文件第 {lineIndex + 1} 行格式非法：{lines[lineIndex]}");

            if (u < 0 || u >= nodeCount || v < 0 || v >= nodeCount)
                throw new InvalidDataException($"拓扑文件第 {lineIndex + 1} 行节点编号越界：{lines[lineIndex]}");
            if (weight <= 0)
                throw new InvalidDataException($"拓扑文件第 {lineIndex + 1} 行边权必须为正：{lines[lineIndex]}");

            topology._adjacency[u, v] = weight;
            topology._adjacency[v, u] = weight;   // 无向图
        }
        return topology;
    }

    /// <summary>
    /// Dijkstra 单源最短路径（贪心 + 松弛）。
    /// 返回（各节点到源的最短距离, 前驱节点数组）。
    /// </summary>
    public (int[] Distances, int[] Previous) Dijkstra(int source)
    {
        if (source < 0 || source >= NodeCount)
            throw new ArgumentOutOfRangeException(nameof(source));

        var dist = new int[NodeCount];
        var prev = new int[NodeCount];
        var visited = new bool[NodeCount];
        for (int i = 0; i < NodeCount; i++)
        {
            dist[i] = Infinity;
            prev[i] = -1;
        }
        dist[source] = 0;

        for (int round = 0; round < NodeCount; round++)
        {
            // 每次从未访问节点中选出距离最小的（朴素 O(N²) 实现，便于教学演示）
            int u = -1;
            for (int i = 0; i < NodeCount; i++)
                if (!visited[i] && (u == -1 || dist[i] < dist[u]))
                    u = i;

            if (u == -1 || dist[u] == Infinity)
                break;   // 剩余节点不可达

            visited[u] = true;

            // 松弛 u 的所有邻边
            for (int v = 0; v < NodeCount; v++)
            {
                if (!visited[v] && _adjacency[u, v] != Infinity &&
                    dist[u] + _adjacency[u, v] < dist[v])
                {
                    dist[v] = dist[u] + _adjacency[u, v];
                    prev[v] = u;
                }
            }
        }
        return (dist, prev);
    }

    /// <summary>
    /// 求 src → dst 的最短路径（节点序列，含两端）。
    /// 不可达时返回空列表。
    /// </summary>
    public List<int> ShortestPath(int src, int dst)
    {
        var (dist, prev) = Dijkstra(src);
        if (dist[dst] == Infinity)
            return new List<int>();

        var path = new List<int>();
        for (int node = dst; node != -1; node = prev[node])
            path.Add(node);
        path.Reverse();
        return path;
    }

    /// <summary>
    /// Floyd-Warshall 全源最短路径（动态规划，三重循环）。
    /// 返回任意两点间最短距离矩阵，可与 Dijkstra 结果互证。
    /// </summary>
    public int[,] FloydWarshall()
    {
        var dist = (int[,])_adjacency.Clone();
        for (int k = 0; k < NodeCount; k++)
            for (int i = 0; i < NodeCount; i++)
                for (int j = 0; j < NodeCount; j++)
                    if (dist[i, k] != Infinity && dist[k, j] != Infinity &&
                        dist[i, k] + dist[k, j] < dist[i, j])
                        dist[i, j] = dist[i, k] + dist[k, j];
        return dist;
    }
}
