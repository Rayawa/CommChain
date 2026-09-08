using CommChain.App.Common;
using CommChain.App.ErrorControl;
using CommChain.App.Modulation;
using CommChain.App.PacketLayer;
using CommChain.App.Queueing;
using CommChain.App.SourceCoding;
using CommChain.App.Topology;

namespace CommChain.App.EndToEnd;

/// <summary>信道错误档位（对应报告测试用例）。</summary>
public enum ErrorLevel
{
    None = 0,   // 无噪声无差错：验证链路基本正确性
    Low = 1,    // 少量单比特错误：汉明码可全部纠正
    High = 2,   // 高频错误：超出汉明纠错能力，演示 CRC 校验兜底
}

/// <summary>物理层调制方式。</summary>
public enum ModulationKind
{
    Ask,
    Fsk,
}

/// <summary>端到端链路模拟结果。</summary>
public sealed record EndToEndResult(
    string OriginalText,
    string RecoveredText,
    bool Success,
    int OriginalBitCount,
    int HuffmanBitCount,
    double CompressionRatio,
    int FrameCount,
    int ChannelErrorCount,
    int CorrectedHammingBlocks,
    int CrcFailedFrames,
    int NoisePacketsDropped,
    double BitErrorRate,
    List<int> Route);

/// <summary>
/// 端到端数字通信链路模拟器（核心主线）。
///
/// 以综合项目二「信号的调制与解调（文本到二进制）」的完整通信链路为骨架，
/// 综合运用指导书第一~第三阶段特性：
///   应用层   文本
///   表示层   霍夫曼信源编码（第一阶段：频率统计/优先队列/二叉树/贪心）
///   网络层   多信源数据包合并与分流 + Dijkstra 最短路径路由
///            （综合项目一：队列/字典；第三阶段：图论/Dijkstra/文件 I/O）
///   链路层   组帧 + CRC-8 校验（第二阶段：位操作/多项式；数据包封装题目）
///   物理层   汉明码(7,4) 前向纠错（第二阶段）+ FSK/ASK 调制解调
///            （第三阶段/综合项目二：信号处理）+ 信道加噪与比特错误模拟
/// </summary>
public sealed class EndToEndSimulator
{
    private readonly NetworkTopology _topology;
    private readonly byte _sourceNode;
    private readonly byte _destinationNode;

    /// <summary>背景噪声包数量（网络层演示多信源合并分流）。</summary>
    public int BackgroundPacketCount { get; init; } = 6;

    public EndToEndSimulator(NetworkTopology topology, int sourceNode, int destinationNode)
    {
        if (sourceNode < 0 || sourceNode >= topology.NodeCount ||
            destinationNode < 0 || destinationNode >= topology.NodeCount)
            throw new ArgumentException("源/目的节点编号超出拓扑范围。");
        _topology = topology;
        _sourceNode = (byte)sourceNode;
        _destinationNode = (byte)destinationNode;
    }

    /// <summary>
    /// 完整模式：9 阶段端到端链路模拟。
    /// </summary>
    public EndToEndResult Run(string text, ErrorLevel level, ModulationKind modulation, Random rng)
    {
        PrintSection($"阶段 1/9 · 信源：文本输入");
        Console.WriteLine($"  原始文本（{text.Length} 字符）：\"{text}\"");
        var originalBytes = System.Text.Encoding.UTF8.GetBytes(text);
        int originalBitCount = originalBytes.Length * 8;

        // ---------------------------------------------------------------
        PrintSection("阶段 2/9 · 信源编码：霍夫曼压缩（第一阶段）");
        var codeTable = HuffmanCoder.BuildCodeTable(text);
        Console.WriteLine($"  码表（{codeTable.Count} 个符号）：");
        foreach (var (symbol, code) in codeTable.OrderBy(kv => kv.Value.Count))
            Console.WriteLine($"    '{Printable(symbol)}' 频率编码 {code.ToBinaryString()}（{code.Count} bit）");

        var huffmanBits = HuffmanCoder.Encode(text, codeTable);
        int validDataLength = huffmanBits.Count;                  // 记录补零前的有效长度
        var paddedData = huffmanBits.PadToMultipleOf(8);          // 补零至整字节
        double compressionRatio = (double)huffmanBits.Count / originalBitCount;
        Console.WriteLine($"  定长编码（UTF-8）：{originalBitCount} bit → 霍夫曼编码：{huffmanBits.Count} bit（补零后 {paddedData.Count} bit）");
        Console.WriteLine($"  压缩率：{compressionRatio:P2}（节省 {(1 - compressionRatio):P2}）");

        // ---------------------------------------------------------------
        PrintSection("阶段 3/9 · 组帧：打包 + CRC-8 校验（链路层，第二阶段）");
        // META 帧载荷：[码表序列化][有效数据长度:16bit]，整体补零至整字节
        var metaPayload = HuffmanCoder.SerializeCodeTable(codeTable)
            .Concat(BitSequence.FromInt(validDataLength, 16))
            .PadToMultipleOf(8);
        var metaPacket = new DataPacket(PacketType.Meta, _sourceNode, _destinationNode, 0, metaPayload);

        // 数据帧：压缩比特流按每帧 ≤ 32 bit 载荷切分
        var dataPackets = new List<DataPacket>();
        byte seq = 1;
        foreach (var chunk in paddedData.SplitChunks(32))
            dataPackets.Add(new DataPacket(PacketType.Data, _sourceNode, _destinationNode, seq++, chunk));

        Console.WriteLine($"  META 帧 1 个（码表 {metaPayload.Count} bit）+ DATA 帧 {dataPackets.Count} 个（每帧载荷 ≤ 32 bit），均附 CRC-8");

        // ---------------------------------------------------------------
        PrintSection($"阶段 4/9 · 网络层：多信源合并与分流 + Dijkstra 路由（综合项目一/第三阶段）");
        // 生成背景流量（其他信源的包），与真实包合并进输入队列
        var inputQueue = new LinkedListQueue<DataPacket>();
        inputQueue.Enqueue(metaPacket);
        foreach (var packet in dataPackets)
            inputQueue.Enqueue(packet);

        for (int i = 0; i < BackgroundPacketCount; i++)
        {
            byte noiseSource = (byte)rng.Next(0, _topology.NodeCount);
            byte noiseDest = (byte)rng.Next(0, _topology.NodeCount);
            var noisePayload = BitSequence.FromByteArray(
                System.Text.Encoding.UTF8.GetBytes($"噪声_{rng.Next(1000, 9999)}"));
            inputQueue.Enqueue(new DataPacket(PacketType.Noise, noiseSource, noiseDest, 200, noisePayload));
        }
        Console.WriteLine($"  输入队列共 {inputQueue.Count} 个包（真实 {1 + dataPackets.Count} + 背景 {BackgroundPacketCount}），按目的地分流…");

        var outputQueues = PacketDispatcher.Dispatch(inputQueue);
        foreach (var (destId, queue) in outputQueues.OrderBy(kv => kv.Key))
            Console.WriteLine($"    目的地 {destId}：{queue.Count} 个包");

        // 取发往真实目的节点的队列，继续传输
        var transmitPackets = outputQueues.TryGetValue(_destinationNode, out var destQueue)
            ? destQueue.ToList()
            : new List<DataPacket>();
        Console.WriteLine($"  目的地 {_destinationNode} 队列中 {transmitPackets.Count} 个包进入传输通道（其余为发往其他节点的流量）");

        // Dijkstra 选路
        var route = _topology.ShortestPath(_sourceNode, _destinationNode);
        Console.WriteLine($"  Dijkstra 最短路径 {_sourceNode}→{_destinationNode}：{string.Join(" → ", route)}（代价 {_topology.Dijkstra(_sourceNode).Distances[_destinationNode]}）");

        // ---------------------------------------------------------------
        PrintSection($"阶段 5/9 · 物理层：汉明码(7,4) 前向纠错 + {ModulationName(modulation)} 调制（第二阶段/第三阶段）");
        // 每帧：组帧（CRC）→ 汉明编码
        var frames = new List<BitSequence>();
        foreach (var packet in transmitPackets)
            frames.Add(FrameCodec.ToFrame(packet));

        var hammingFrames = new List<BitSequence>();
        foreach (var frame in frames)
        {
            var (encoded, blocks) = HammingCodec.EncodeStream(frame);
            hammingFrames.Add(encoded);
        }
        int totalBits = hammingFrames.Sum(f => f.Count);
        Console.WriteLine($"  {frames.Count} 帧完成组帧（CRC-8）与汉明编码（每 4bit→7bit），信道待传比特 {totalBits} 位");

        // ---------------------------------------------------------------
        PrintSection("阶段 6/9 · 信道传输：噪声与比特错误模拟");
        // 档 1：每帧至多 1 个比特错误（汉明码保证可纠，链路无损）；
        // 档 2：高频错误且不限帧内错误数（超出汉明纠错能力，由 CRC 兜底）。
        // 30/totalBits 约引入 30 个错误：分布在约 200 个 7bit 块上，
        // 必然出现同块双错（单错均可纠，双错才会超出汉明能力触发 CRC 兜底）。
        double ber = level switch
        {
            ErrorLevel.None => 0,
            ErrorLevel.Low => 0.5 / totalBits,
            ErrorLevel.High => 30.0 / totalBits,
            _ => 0,
        };
        int? maxErrorsPerFrame = level switch
        {
            ErrorLevel.None => 0,
            ErrorLevel.Low => 1,
            ErrorLevel.High => null,
            _ => 0,
        };
        double noiseLevel = level == ErrorLevel.None ? 0 : 1.5;   // 模拟信号噪声（伏特）

        // 条件表达式需显式指定为抽象基类类型：var 推断时两个子类之间没有隐式转换
        Modulator modulator = modulation == ModulationKind.Fsk ? new FskModulator() : new AskModulator();
        int totalErrors = 0;
        var receivedFrames = new List<BitSequence>();
        for (int i = 0; i < hammingFrames.Count; i++)
        {
            // 1) 比特级错误（BER + 帧内错误上限控制）
            var (corrupted, errors) = Channel.IntroduceBitErrors(hammingFrames[i], rng, ber, maxErrorsPerFrame);
            totalErrors += errors;

            // 2) 调制 → 信道加噪 → 解调
            double[] signal = modulator.Modulate(corrupted);
            double[] noisySignal = Channel.AddNoise(signal, rng, noiseLevel);
            receivedFrames.Add(modulator.Demodulate(noisySignal));
        }
        Console.WriteLine($"  调制方式：{modulator.Name}（每比特 {modulator.SamplesPerBit} 采样）");
        Console.WriteLine($"  信道误比特率 BER = {ber:F5}，噪声电平 {noiseLevel} V");
        Console.WriteLine($"  信道共引入 {totalErrors} 个比特错误（档位：{LevelName(level)}）");

        // ---------------------------------------------------------------
        PrintSection("阶段 7/9 · 接收端：解调 → 汉明纠错 → 拆帧与 CRC 校验");
        int correctedBlocks = 0, crcFailed = 0, noiseDropped = 0;
        var metaTable = new Dictionary<char, BitSequence>();
        int recoveredValidLength = 0;
        var dataPayloads = new SortedDictionary<byte, BitSequence>();   // 按 seq 排序

        foreach (var receivedFrame in receivedFrames)
        {
            // 汉明解码纠错
            var (decoded, corrected, _) = HammingCodec.DecodeStream(receivedFrame);
            correctedBlocks += corrected;

            // 拆帧 + CRC 校验
            var parseResult = FrameCodec.TryParse(decoded);
            if (!parseResult.Success)
            {
                crcFailed++;
                Console.WriteLine($"  帧校验失败（丢弃）：{parseResult.ErrorMessage}");
                continue;
            }

            var packet = parseResult.Packet!;
            switch (packet.Type)
            {
                case PacketType.Meta:
                    var (table, bitsConsumed) = HuffmanCoder.DeserializeCodeTable(packet.Payload);
                    metaTable = table;
                    recoveredValidLength = packet.Payload.Slice(bitsConsumed, 16).ToInt();
                    Console.WriteLine($"  META 帧解析成功：码表 {table.Count} 个符号，数据有效长度 {recoveredValidLength} bit");
                    break;
                case PacketType.Data:
                    dataPayloads[packet.Seq] = packet.Payload;
                    break;
                case PacketType.Noise:
                    noiseDropped++;   // 背景噪声包被接收端丢弃
                    break;
            }
        }
        Console.WriteLine($"  汉明码纠错 {correctedBlocks} 个 7bit 块；CRC 校验失败 {crcFailed} 帧；丢弃背景噪声包 {noiseDropped} 个");

        // ---------------------------------------------------------------
        PrintSection("阶段 8/9 · 信源译码：霍夫曼解码（第一阶段）");
        string recoveredText;
        if (metaTable.Count == 0)
        {
            recoveredText = string.Empty;
        }
        else
        {
            var combined = new BitSequence();
            foreach (var payload in dataPayloads.Values)
                combined.AddRange(payload);
            recoveredText = HuffmanCoder.Decode(combined.Take(recoveredValidLength), metaTable);
        }
        Console.WriteLine($"  恢复文本：\"{recoveredText}\"");

        // ---------------------------------------------------------------
        PrintSection("阶段 9/9 · 信宿：一致性验证与统计");
        bool success = recoveredText == text;
        Console.WriteLine(success
            ? "  ✔ 恢复文本与原始文本完全一致，端到端传输成功！"
            : "  ✘ 恢复文本与原始文本不一致（高频错误超出纠错能力，CRC 兜底生效）。");

        double bitErrorRate = totalBits == 0 ? 0 : (double)totalErrors / totalBits;
        Console.WriteLine($"  ┌──────────────── 链路统计 ────────────────┐");
        Console.WriteLine($"  │ 原始文本比特数（UTF-8）      {originalBitCount,8} bit   │");
        Console.WriteLine($"  │ 霍夫曼压缩后比特数          {huffmanBits.Count,8} bit   │");
        Console.WriteLine($"  │ 压缩率                      {compressionRatio,8:P2}       │");
        Console.WriteLine($"  │ 数据帧数（META+DATA）        {1 + dataPackets.Count,8} 帧   │");
        Console.WriteLine($"  │ 信道比特错误数              {totalErrors,8} 个    │");
        Console.WriteLine($"  │ 汉明码纠正块数              {correctedBlocks,8} 块   │");
        Console.WriteLine($"  │ CRC 校验失败帧数            {crcFailed,8} 帧   │");
        Console.WriteLine($"  │ 丢弃背景噪声包数            {noiseDropped,8} 个    │");
        Console.WriteLine($"  │ 实际误比特率 BER            {bitErrorRate,8:P4}     │");
        Console.WriteLine($"  │ 最短路径 {_sourceNode}→{_destinationNode}            {string.Join("→", route),-10}     │");
        Console.WriteLine($"  └──────────────────────────────────────────┘");

        return new EndToEndResult(
            text, recoveredText, success,
            originalBitCount, huffmanBits.Count, compressionRatio,
            1 + dataPackets.Count, totalErrors, correctedBlocks,
            crcFailed, noiseDropped, bitErrorRate, route);
    }

    /// <summary>
    /// 简化模式：综合项目二的原始四函数链路（文本→8bit 二进制→ASK 电平调制→阈值解调→文本）。
    /// </summary>
    public static void RunSimplified(string text)
    {
        PrintSection("简化模式 · 综合项目二原始链路（文本 ↔ 二进制 ↔ 电平信号）");

        // 1. 文本 → 二进制（每字符 8 bit）
        var binary = BitSequence.FromByteArray(System.Text.Encoding.UTF8.GetBytes(text));
        Console.WriteLine($"  [1] 文本 → 二进制（{binary.Count} bit）：");
        Console.WriteLine($"      {binary.ToBinaryString(true)}");

        // 2. 调制：1→10V，0→0V
        var ask = new AskModulator();
        double[] signal = ask.Modulate(binary);
        Console.WriteLine($"  [2] ASK 调制（1→10V，0→0V）：{string.Join(" ", signal.Select(v => v.ToString("0")))}");

        // 3. 解调：阈值 5V
        var demodulated = ask.Demodulate(signal);
        Console.WriteLine($"  [3] 阈值解调（≥5V 判 1）：{demodulated.ToBinaryString(true)}");

        // 4. 二进制 → 文本
        var bytes = demodulated.ToByteArray();
        string recovered = System.Text.Encoding.UTF8.GetString(bytes);
        Console.WriteLine($"  [4] 二进制 → 文本：\"{recovered}\"");

        bool ok = recovered == text;
        Console.WriteLine(ok
            ? "  ✔ 恢复文本与原始文本一致，转换成功！"
            : "  ✘ 转换失败。");
    }

    private static string Printable(char c) =>
        c switch
        {
            '\n' => "\\n",
            '\r' => "\\r",
            '\t' => "\\t",
            _ => c.ToString(),
        };

    private static string LevelName(ErrorLevel level) => level switch
    {
        ErrorLevel.None => "档 0 无差错",
        ErrorLevel.Low => "档 1 少量差错（可纠）",
        ErrorLevel.High => "档 2 高频差错（超纠错能力）",
        _ => "未知",
    };

    private static string ModulationName(ModulationKind kind) => kind switch
    {
        ModulationKind.Ask => "ASK 幅移键控",
        ModulationKind.Fsk => "FSK 频移键控",
        _ => "未知",
    };

    private static void PrintSection(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"══ {title} ══");
    }
}
