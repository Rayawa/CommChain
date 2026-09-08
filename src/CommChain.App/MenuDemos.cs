using CommChain.App.Common;
using CommChain.App.EndToEnd;
using CommChain.App.ErrorControl;
using CommChain.App.Modulation;
using CommChain.App.Queueing;
using CommChain.App.SourceCoding;
using CommChain.App.Topology;

namespace CommChain.App;

/// <summary>
/// 主菜单各演示项的入口。每个方法对应一个独立可演示（可截图）的功能模块。
/// </summary>
public static class MenuDemos
{
    /// <summary>演示用固定随机种子（保证每次运行输出一致，便于报告截图与测试复现）。</summary>
    public const int DemoSeed = 42;

    // ---------------------------------------------------------------
    // 1. 霍夫曼编码演示（第一阶段：频率统计/优先队列/二叉树/贪心）
    // ---------------------------------------------------------------
    public static void HuffmanDemo(string? input = null)
    {
        string text = input ?? ReadLine("请输入一段文本：");
        if (text.Length == 0)
        {
            Console.WriteLine("文本为空，返回主菜单。");
            return;
        }

        Console.WriteLine("\n--- 步骤1：字符频率统计 ---");
        var frequencies = text.GroupBy(c => c).OrderByDescending(g => g.Count());
        foreach (var group in frequencies)
            Console.WriteLine($"  字符 '{Printable(group.Key)}'：{group.Count()} 次");

        Console.WriteLine("\n--- 步骤2：贪心构建霍夫曼树并生成前缀码 ---");
        var codeTable = HuffmanCoder.BuildCodeTable(text);
        foreach (var (symbol, code) in codeTable.OrderBy(kv => kv.Value.Count))
            Console.WriteLine($"  '{Printable(symbol)}' → {code.ToBinaryString()}（{code.Count} bit）");

        Console.WriteLine("\n--- 步骤3：编码 ---");
        var encoded = HuffmanCoder.Encode(text, codeTable);
        int fixedLength = text.Length * 8;
        Console.WriteLine($"  定长编码（每字符 8 bit）：{fixedLength} bit");
        Console.WriteLine($"  霍夫曼编码：{encoded.Count} bit");
        Console.WriteLine($"  压缩率：{(double)encoded.Count / fixedLength:P2}");
        Console.WriteLine($"  编码结果：{encoded.ToBinaryString()}");

        Console.WriteLine("\n--- 步骤4：解码还原 ---");
        string decoded = HuffmanCoder.Decode(encoded, codeTable);
        Console.WriteLine($"  解码结果：\"{decoded}\"");
        Console.WriteLine(decoded == text ? "  ✔ 解码与原文一致。" : "  ✘ 解码失败！");
    }

    // ---------------------------------------------------------------
    // 2. 链表队列演示（第一阶段：链表/动态内存）
    // ---------------------------------------------------------------
    public static void QueueDemo()
    {
        Console.WriteLine("\n--- 手写链表队列 LinkedListQueue<T> 演示 ---");
        var queue = new LinkedListQueue<string>();
        Console.WriteLine($"  初始：IsEmpty={queue.IsEmpty}, Count={queue.Count}");

        foreach (string item in new[] { "数据包A", "数据包B", "数据包C" })
        {
            queue.Enqueue(item);
            Console.WriteLine($"  入队 \"{item}\" → Count={queue.Count}");
        }

        Console.WriteLine($"  查看队头 Peek：\"{queue.Peek()}\"（不出队）");
        while (!queue.IsEmpty)
            Console.WriteLine($"  出队 Dequeue：\"{queue.Dequeue()}\" → 剩余 Count={queue.Count}");

        Console.WriteLine($"  全部出队后：IsEmpty={queue.IsEmpty}");

        Console.WriteLine("\n  异常处理演示：空队列出队 →");
        try
        {
            queue.Dequeue();
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"  ✘ 捕获异常：{ex.Message}（程序未崩溃，容错正常）");
        }
    }

    // ---------------------------------------------------------------
    // 3. CRC 校验演示（第二阶段：位操作/多项式运算）
    // ---------------------------------------------------------------
    public static void CrcDemo(string? input = null)
    {
        string text = input ?? ReadLine("请输入要传输的数据（字符串）：");
        byte[] data = System.Text.Encoding.UTF8.GetBytes(text);

        Console.WriteLine($"\n--- 发送端：计算校验码 ---");
        byte crc = CrcCalculator.ComputeCrc8(data);
        int simple = CrcCalculator.SimpleChecksum(text);
        Console.WriteLine($"  数据字节（HEX）：{BitConverter.ToString(data)}");
        Console.WriteLine($"  CRC-8（多项式 0x07）：0x{crc:X2}");
        Console.WriteLine($"  简易校验和（ASCII 累加 % 255）：{simple}");

        Console.WriteLine("\n--- 传输（篡改模拟）---");
        var corrupted = (byte[])data.Clone();
        corrupted[0] ^= 0x41;   // 翻转首字节部分位，模拟传输错误
        Console.WriteLine($"  篡改后字节（HEX）：{BitConverter.ToString(corrupted)}");

        Console.WriteLine("\n--- 接收端：重新校验 ---");
        byte recomputed = CrcCalculator.ComputeCrc8(corrupted);
        Console.WriteLine($"  重算 CRC-8：0x{recomputed:X2}，与发送值 0x{crc:X2} " +
                          (CrcCalculator.Verify(corrupted, crc) ? "一致 → 数据完整 ✔" : "不一致 → 检测到传输错误 ✘"));
        Console.WriteLine($"  完整链路演示：{CrcCalculator.Verify(data, crc)} 应输出 True；篡改后 {CrcCalculator.Verify(corrupted, crc)} 应输出 False");
    }

    // ---------------------------------------------------------------
    // 4. 汉明码纠错演示（第二阶段：位操作/错误检测与纠正）
    // ---------------------------------------------------------------
    public static void HammingDemo(string? dataInput = null, int? flipPosition = null)
    {
        string input = dataInput ?? ReadLine("请输入 4 位数据比特（如 1011）：");
        var data = new BitSequence(input);
        if (data.Count != 4)
        {
            Console.WriteLine("✘ 输入必须恰好 4 位。");
            return;
        }

        Console.WriteLine("\n--- 编码器 ---");
        var encoded = HammingCodec.Encode(data);
        Console.WriteLine($"  数据：{data.ToBinaryString()}");
        Console.WriteLine($"  校验位 p1=d1⊕d2⊕d4, p2=d1⊕d3⊕d4, p3=d2⊕d3⊕d4");
        Console.WriteLine($"  汉明码（p1 p2 d1 p3 d2 d3 d4）：{encoded.ToBinaryString()}");

        Console.WriteLine("\n--- 信道错误模拟 ---");
        int position = flipPosition ?? ReadInt("输入要翻转的比特位置（1~7，0 表示不翻转）：", 0, 7);
        var corrupted = encoded;
        if (position >= 1)
        {
            corrupted = HammingCodec.FlipBit(encoded, position);
            Console.WriteLine($"  翻转第 {position} 位后：{corrupted.ToBinaryString()}");
        }

        Console.WriteLine("\n--- 解码器 ---");
        var result = HammingCodec.Decode(corrupted);
        if (result.ErrorPosition == 0)
            Console.WriteLine($"  校验子 s3s2s1 = 000 → 未检测到错误");
        else
        {
            Console.WriteLine($"  校验子 s3s2s1 = {result.ErrorPosition:B3} → 第 {result.ErrorPosition} 位出错");
            Console.WriteLine($"  纠正后数据：{result.Data.ToBinaryString()}");
        }
        Console.WriteLine($"  恢复的数据比特：{result.Data.ToBinaryString()}");
        Console.WriteLine(result.Data.ToBinaryString() == data.ToBinaryString()
            ? "  ✔ 数据完整恢复。"
            : "  ✘ 恢复失败（发生了多位错误）。");
    }

    // ---------------------------------------------------------------
    // 5. 网络拓扑与最短路径演示（第三阶段：图/Dijkstra/Floyd/文件 I/O）
    // ---------------------------------------------------------------
    public static void TopologyDemo(string? topologyFile = null, int? src = null, int? dst = null)
    {
        string path = topologyFile ?? "data/topology.txt";
        Console.WriteLine($"\n--- 从文件加载拓扑：{path} ---");
        var topology = NetworkTopology.LoadFromFile(path);
        Console.WriteLine($"  节点数：{topology.NodeCount}");

        Console.WriteLine("\n--- 邻接矩阵（∞ 表示无边）---");
        var adjacency = topology.Adjacency;
        Console.Write("      ");
        for (int j = 0; j < topology.NodeCount; j++)
            Console.Write($"{j,5}");
        Console.WriteLine();
        for (int i = 0; i < topology.NodeCount; i++)
        {
            Console.Write($"  {i,3} ");
            for (int j = 0; j < topology.NodeCount; j++)
                Console.Write(adjacency[i, j] == NetworkTopology.Infinity ? $"{"∞",5}" : $"{adjacency[i, j],5}");
            Console.WriteLine();
        }

        int source = src ?? ReadInt("输入源节点编号：", 0, topology.NodeCount - 1);
        int destination = dst ?? ReadInt("输入目的节点编号：", 0, topology.NodeCount - 1);

        Console.WriteLine("\n--- Dijkstra 单源最短路径 ---");
        var (distances, _) = topology.Dijkstra(source);
        Console.WriteLine($"  从节点 {source} 出发到各节点的最短距离：{string.Join(", ", distances.Select(d => d == NetworkTopology.Infinity ? "∞" : d.ToString()))}");
        var route = topology.ShortestPath(source, destination);
        if (route.Count == 0)
        {
            Console.WriteLine($"  ✘ 节点 {source} 到 {destination} 不可达。");
        }
        else
        {
            Console.WriteLine($"  最短路径 {source}→{destination}：{string.Join(" → ", route)}");
            Console.WriteLine($"  路径总代价：{distances[destination]}");
        }

        Console.WriteLine("\n--- Floyd-Warshall 全源最短路径（与 Dijkstra 互证）---");
        var floyd = topology.FloydWarshall();
        bool consistent = true;
        for (int i = 0; i < topology.NodeCount; i++)
        {
            var (dijkstraDist, _) = topology.Dijkstra(i);
            for (int j = 0; j < topology.NodeCount; j++)
                if (dijkstraDist[j] != floyd[i, j])
                    consistent = false;
        }
        Console.WriteLine($"  两算法对全部 {topology.NodeCount}×{topology.NodeCount} 组节点对结果一致：{(consistent ? "✔ 是" : "✘ 否")}");
    }

    // ---------------------------------------------------------------
    // 6. ASK/FSK 调制解调演示（第三阶段：信号处理/多态）
    // ---------------------------------------------------------------
    public static void ModulationDemo()
    {
        string input = ReadLine("请输入 8 位比特（如 10110010）：");
        var bits = new BitSequence(input);
        if (bits.Count != 8)
        {
            Console.WriteLine("✘ 输入必须恰好 8 位。");
            return;
        }

        // 抽象基类引用子类实例，演示多态
        var rng = new Random(DemoSeed);
        foreach (Modulator modulator in new Modulator[] { new AskModulator(), new FskModulator() })
        {
            Console.WriteLine($"\n--- {modulator.Name}（多态调用 {nameof(Modulator.Modulate)}/{nameof(Modulator.Demodulate)}）---");
            double[] signal = modulator.Modulate(bits);
            DrawWaveform(modulator, signal);

            // 信道加噪
            double[] noisy = Channel.AddNoise(signal, rng, 1.2);
            var demodulated = modulator.Demodulate(noisy);

            Console.WriteLine($"  原始比特：{bits.ToBinaryString()}");
            Console.WriteLine($"  解调比特：{demodulated.ToBinaryString()}");
            Console.WriteLine(demodulated.ToBinaryString() == bits.ToBinaryString()
                ? "  ✔ 加噪后解调无误。"
                : "  ✘ 解调出现误码。");
        }
    }

    /// <summary>把调制信号前 24 个采样绘制为 ASCII 波形图（ASK 每比特 1 采样，FSK 每比特 8 采样）。</summary>
    private static void DrawWaveform(Modulator modulator, double[] signal)
    {
        int showSamples = Math.Min(24, signal.Length);
        double min = -12, max = 12;
        int rows = 9;   // 波形图行数

        Console.WriteLine($"  波形（前 {showSamples} 个采样，幅度 -12~+12V）：");
        for (int r = rows - 1; r >= 0; r--)
        {
            double level = min + (max - min) * (r + 0.5) / rows;
            Console.Write($"    {level,5:0.0}V |");
            for (int s = 0; s < showSamples; s++)
            {
                double sample = signal[s];
                char mark = sample >= level + (max - min) / rows ? '#' :
                            sample <= level - (max - min) / rows ? ' ' : '·';
                Console.Write(mark);
            }
            Console.WriteLine();
        }
        Console.WriteLine($"           +{new string('-', showSamples)}> 时间");
    }

    // ---------------------------------------------------------------
    // 7. 数据包合并与分流演示（综合项目一：队列/字典/随机生成）
    // ---------------------------------------------------------------
    public static void DispatcherDemo(int? count = null)
    {
        int packetCount = count ?? ReadInt("请输入要生成的数据包数量：", 1, 100);
        var rng = new Random(DemoSeed);

        Console.WriteLine($"\n--- 生成 {packetCount} 个数据包（多信源随机流量）---");
        var inputQueue = PacketDispatcher.GeneratePackets(packetCount, rng);
        foreach (var packet in inputQueue.ToList())
            Console.WriteLine($"  生成：{packet}");

        Console.WriteLine("\n--- 合并入输入队列，按目的地 ID 分流 ---");
        var outputQueues = PacketDispatcher.Dispatch(inputQueue);

        Console.WriteLine("\n--- 分流结果（各目的地输出队列）---");
        PacketDispatcher.PrintOutputQueues(outputQueues);
        Console.WriteLine("  ✔ 分流完成：每个包恰好出现在其目的地的队列中。");
    }

    // ---------------------------------------------------------------
    // 8. 端到端完整链路模拟（核心主线）
    // ---------------------------------------------------------------
    public static void EndToEndDemo()
    {
        string text = ReadLine("请输入要传输的文本：");
        if (text.Length == 0)
        {
            Console.WriteLine("文本为空，返回主菜单。");
            return;
        }

        Console.WriteLine("\n请选择模式：");
        Console.WriteLine("  1. 简化模式 —— 综合项目二原始四函数链路（文本→二进制→ASK 电平→解调→文本）");
        Console.WriteLine("  2. 完整模式 —— 9 阶段端到端链路（霍夫曼+组帧CRC+汉明+队列分流+Dijkstra+FSK+信道）");
        int mode = ReadInt("请输入选择：", 1, 2);

        if (mode == 1)
        {
            EndToEndSimulator.RunSimplified(text);
            return;
        }

        int level = ReadInt("选择信道错误档位（0=无差错 1=少量可纠差错 2=高频超限差错）：", 0, 2);
        var rng = new Random(DemoSeed);
        var topology = NetworkTopology.LoadFromFile("data/topology.txt");
        var simulator = new EndToEndSimulator(topology, sourceNode: 0, destinationNode: 7);

        var result = simulator.Run(text, (ErrorLevel)level, ModulationKind.Fsk, rng);
        _ = result;   // 结果已由 Run 内部打印
    }

    // ---------------------------------------------------------------
    // 输入辅助
    // ---------------------------------------------------------------
    private static string ReadLine(string prompt)
    {
        Console.Write($"{prompt} ");
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }

    private static int ReadInt(string prompt, int min, int max)
    {
        while (true)
        {
            Console.Write($"{prompt} ");
            if (int.TryParse(Console.ReadLine(), out int value) && value >= min && value <= max)
                return value;
            Console.WriteLine($"✘ 输入无效，请输入 {min}~{max} 之间的整数。");
        }
    }

    private static string Printable(char c) =>
        c switch
        {
            '\n' => "\\n",
            '\r' => "\\r",
            '\t' => "\\t",
            _ => c.ToString(),
        };
}
