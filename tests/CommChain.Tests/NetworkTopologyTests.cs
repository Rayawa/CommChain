using CommChain.App.Topology;

namespace CommChain.Tests;

public class NetworkTopologyTests
{
    /// <summary>与 data/topology.txt 内容一致的临时拓扑文件。</summary>
    private static string CreateTempTopologyFile()
    {
        string path = Path.Combine(Path.GetTempPath(), $"topology-test-{Guid.NewGuid()}.txt");
        File.WriteAllLines(path, new[]
        {
            "# 测试拓扑",
            "8",
            "0 1 4",
            "0 2 2",
            "1 2 1",
            "1 3 5",
            "2 3 8",
            "2 4 10",
            "3 4 2",
            "3 5 6",
            "4 5 3",
            "5 6 5",
            "5 7 2",
            "6 7 4",
        });
        return path;
    }

    [Fact]
    public void 从文件加载拓扑成功()
    {
        string path = CreateTempTopologyFile();
        try
        {
            var topology = NetworkTopology.LoadFromFile(path);
            Assert.Equal(8, topology.NodeCount);
            Assert.Equal(4, topology.Adjacency[0, 1]);   // 无向边
            Assert.Equal(4, topology.Adjacency[1, 0]);
            Assert.Equal(NetworkTopology.Infinity, topology.Adjacency[0, 5]);   // 无边
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Dijkstra最短路径正确()
    {
        string path = CreateTempTopologyFile();
        try
        {
            var topology = NetworkTopology.LoadFromFile(path);
            var (distances, previous) = topology.Dijkstra(0);

            // 手算验证：0→7 最短为 0-2-1-3-4-5-7 = 2+1+5+2+3+2 = 15
            // （0→1 直连 4 > 0→2→1 的 3，故 dist[1] = 3）
            Assert.Equal(0, distances[0]);
            Assert.Equal(3, distances[1]);
            Assert.Equal(2, distances[2]);
            Assert.Equal(15, distances[7]);

            var route = topology.ShortestPath(0, 7);
            Assert.Equal(new List<int> { 0, 2, 1, 3, 4, 5, 7 }, route);
            _ = previous;
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Floyd与Dijkstra全源结果一致()
    {
        string path = CreateTempTopologyFile();
        try
        {
            var topology = NetworkTopology.LoadFromFile(path);
            var floyd = topology.FloydWarshall();

            for (int src = 0; src < topology.NodeCount; src++)
            {
                var (dijkstra, _) = topology.Dijkstra(src);
                for (int dst = 0; dst < topology.NodeCount; dst++)
                    Assert.Equal(dijkstra[dst], floyd[src, dst]);
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void 不可达节点距离为无穷()
    {
        string path = Path.Combine(Path.GetTempPath(), $"topology-isolated-{Guid.NewGuid()}.txt");
        File.WriteAllLines(path, new[] { "3", "0 1 5" });   // 节点 2 孤立
        try
        {
            var topology = NetworkTopology.LoadFromFile(path);
            var (distances, _) = topology.Dijkstra(0);
            Assert.Equal(NetworkTopology.Infinity, distances[2]);
            Assert.Empty(topology.ShortestPath(0, 2));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("abc")]                    // 节点数非法
    [InlineData("2", "0 1")]               // 边格式缺权
    [InlineData("2", "0 5 3")]             // 节点编号越界
    [InlineData("2", "0 1 -1")]            // 边权非正
    public void 非法文件内容抛异常(string firstLine, params string[] edges)
    {
        string path = Path.Combine(Path.GetTempPath(), $"topology-bad-{Guid.NewGuid()}.txt");
        File.WriteAllLines(path, new[] { firstLine }.Concat(edges));
        try
        {
            Assert.Throws<InvalidDataException>(() => NetworkTopology.LoadFromFile(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void 越界源节点抛异常()
    {
        string path = CreateTempTopologyFile();
        try
        {
            var topology = NetworkTopology.LoadFromFile(path);
            Assert.Throws<ArgumentOutOfRangeException>(() => topology.Dijkstra(8));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
