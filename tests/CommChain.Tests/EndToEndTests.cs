using CommChain.App.EndToEnd;
using CommChain.App.Topology;

namespace CommChain.Tests;

public class EndToEndTests
{
    private const string TopologySource =
        """
        8
        0 1 4
        0 2 2
        1 2 1
        1 3 5
        2 3 8
        2 4 10
        3 4 2
        3 5 6
        4 5 3
        5 6 5
        5 7 2
        6 7 4
        """;

    private static EndToEndSimulator CreateSimulator()
    {
        string path = Path.Combine(Path.GetTempPath(), $"topology-e2e-{Guid.NewGuid()}.txt");
        File.WriteAllText(path, TopologySource);
        var topology = NetworkTopology.LoadFromFile(path);
        File.Delete(path);
        return new EndToEndSimulator(topology, 0, 7) { BackgroundPacketCount = 4 };
    }

    /// <summary>执行一次链路模拟并静默控制台输出，避免测试日志噪音。</summary>
    private static EndToEndResult RunQuiet(ErrorLevel level, string text = "数字通信系统端到端模拟测试文本")
    {
        var simulator = CreateSimulator();
        var rng = new Random(42);   // 固定种子，结果可复现

        using var writer = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(writer);
        try
        {
            return simulator.Run(text, level, ModulationKind.Fsk, rng);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void 档0无差错_链路无损恢复()
    {
        var result = RunQuiet(ErrorLevel.None);

        Assert.True(result.Success);
        Assert.Equal("数字通信系统端到端模拟测试文本", result.RecoveredText);
        Assert.Equal(0, result.ChannelErrorCount);
        Assert.Equal(0, result.CrcFailedFrames);
        Assert.Equal(0, result.CorrectedHammingBlocks);
        Assert.Equal(new List<int> { 0, 2, 1, 3, 4, 5, 7 }, result.Route);
        Assert.True(result.FrameCount >= 2, "至少包含 1 个 META 帧与 1 个 DATA 帧");
        Assert.True(result.NoisePacketsDropped >= 0);
    }

    [Fact]
    public void 档1少量差错_汉明码全部纠正()
    {
        var result = RunQuiet(ErrorLevel.Low);

        // 每帧至多 1 错：汉明码必能纠正，链路无损
        Assert.True(result.Success);
        Assert.Equal("数字通信系统端到端模拟测试文本", result.RecoveredText);
        Assert.Equal(0, result.CrcFailedFrames);
        Assert.True(result.ChannelErrorCount <= result.FrameCount,
            "档 1 每帧至多 1 个错误");
        Assert.Equal(result.ChannelErrorCount, result.CorrectedHammingBlocks);
    }

    [Fact]
    public void 档2高频差错_CRC校验兜底()
    {
        var result = RunQuiet(ErrorLevel.High);

        // 高频错误超出汉明纠错能力：部分帧 CRC 失败被丢弃
        Assert.True(result.ChannelErrorCount > result.CorrectedHammingBlocks,
            "档 2 错误数应明显大于可纠正块数（含同块双错）");
        Assert.True(result.CrcFailedFrames > 0, "档 2 应至少检出 1 帧 CRC 失败");
        Assert.True(result.CorrectedHammingBlocks > 0, "档 2 仍应展示汉明纠错过程");
    }

    [Fact]
    public void 三档错误数递增且结果可复现()
    {
        var result0 = RunQuiet(ErrorLevel.None);
        var result1 = RunQuiet(ErrorLevel.Low);
        var result2 = RunQuiet(ErrorLevel.High);
        var repeat2 = RunQuiet(ErrorLevel.High);

        Assert.True(result0.ChannelErrorCount <= result1.ChannelErrorCount);
        Assert.True(result1.ChannelErrorCount < result2.ChannelErrorCount);
        // 固定种子 → 完全相同的统计结果（截图与报告可复现）
        Assert.Equal(result2.ChannelErrorCount, repeat2.ChannelErrorCount);
        Assert.Equal(result2.CrcFailedFrames, repeat2.CrcFailedFrames);
    }

    [Fact]
    public void 简化模式往返成功()
    {
        var simulator = CreateSimulator();
        using var writer = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(writer);
        try
        {
            EndToEndSimulator.RunSimplified("综合项目二");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
        // 简化模式无返回值：不抛异常且输出包含成功标记
        Assert.Contains("✔", writer.ToString());
    }
}
