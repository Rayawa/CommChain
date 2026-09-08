using CommChain.App.Common;
using CommChain.App.ErrorControl;

namespace CommChain.App.PacketLayer;

/// <summary>数据包类型。</summary>
public enum PacketType : byte
{
    Meta = 0x01,   // 元信息帧（霍夫曼码表 + 有效数据长度）
    Data = 0x02,   // 数据帧（霍夫曼编码后的比特流分片）
    Noise = 0x03,  // 背景噪声包（网络层模拟其他信源的流量，接收端丢弃）
}

/// <summary>
/// 链路层数据包：包头（类型/源/目的/序号/载荷长度）+ 变长载荷 + CRC-8 校验。
/// 对应指导书数据包封装题目（header/payload/checksum），并扩展为完整帧格式：
///   帧位布局 [type:8][sourceId:8][destId:8][seq:8][payloadBytes:16][payload:变长][crc8:8]
/// </summary>
public sealed record DataPacket
{
    public const int HeaderBitLength = 48;   // 类型+源+目的+序号+长度字段共 6 字节

    public PacketType Type { get; init; }
    public byte SourceId { get; init; }
    public byte DestinationId { get; init; }
    public byte Seq { get; init; }

    /// <summary>载荷（比特流，长度须为 8 的整数倍）。</summary>
    public BitSequence Payload { get; init; } = new();

    /// <summary>载荷字节数（帧头中的长度字段值）。</summary>
    public int PayloadByteCount => Payload.Count / 8;

    public DataPacket()
    {
    }

    public DataPacket(PacketType type, byte sourceId, byte destinationId, byte seq, BitSequence payload)
    {
        if (payload.Count % 8 != 0)
            throw new ArgumentException("载荷比特数须为 8 的整数倍。", nameof(payload));
        if (payload.Count / 8 > ushort.MaxValue)
            throw new ArgumentException("载荷过大，长度字段溢出。", nameof(payload));

        Type = type;
        SourceId = sourceId;
        DestinationId = destinationId;
        Seq = seq;
        Payload = payload;
    }

    public override string ToString() =>
        $"[类型={Type} 源={SourceId} 目的={DestinationId} 序号={Seq} 载荷={PayloadByteCount}字节]";
}

/// <summary>组帧（封装）/ 拆帧（解封装）结果。</summary>
/// <param name="Packet">解析出的数据包（失败时为 null）。</param>
/// <param name="CrcValid">CRC 校验是否通过。</param>
/// <param name="ErrorMessage">失败原因（成功时为 null）。</param>
public sealed record FrameParseResult(DataPacket? Packet, bool CrcValid, string? ErrorMessage)
{
    public bool Success => Packet is not null && CrcValid;
}

/// <summary>
/// 帧编解码器：把 DataPacket 组装为传输用比特帧，或从比特帧拆解并校验。
/// 考察点（数据包封装题目 + 第二阶段）：结构化数据封装、CRC 校验、错误处理。
/// </summary>
public static class FrameCodec
{
    /// <summary>组装帧：帧头 + 载荷 + CRC-8（校验覆盖帧头与载荷全部字节）。</summary>
    public static BitSequence ToFrame(DataPacket packet)
    {
        var frame = new BitSequence();
        frame.AddInt((byte)packet.Type, 8);
        frame.AddInt(packet.SourceId, 8);
        frame.AddInt(packet.DestinationId, 8);
        frame.AddInt(packet.Seq, 8);
        frame.AddInt(packet.PayloadByteCount, 16);
        frame.AddRange(packet.Payload);

        // CRC 覆盖帧头与载荷（不含 CRC 字段本身）
        byte crc = CrcCalculator.ComputeCrc8(frame.ToByteArray());
        frame.AddInt(crc, 8);
        return frame;
    }

    /// <summary>
    /// 拆帧：解析帧头 → 读取载荷 → 重算 CRC 校验完整性。
    /// </summary>
    public static FrameParseResult TryParse(BitSequence frame)
    {
        const int headerBits = DataPacket.HeaderBitLength;
        const int crcBits = 8;

        if (frame.Count < headerBits + crcBits)
            return new FrameParseResult(null, false, "帧长度不足，无法解析帧头。");

        // 按 MSB first 从帧头读取各字段
        var bytes = frame.ToByteArray();
        var type = (PacketType)bytes[0];
        byte sourceId = bytes[1];
        byte destinationId = bytes[2];
        byte seq = bytes[3];
        int payloadBytes = (bytes[4] << 8) | bytes[5];

        int totalBytes = headerBits / 8 + payloadBytes + crcBits / 8;
        if (bytes.Length != totalBytes)
            return new FrameParseResult(null, false,
                $"帧长度不符：头指示 {totalBytes} 字节，实际 {bytes.Length} 字节。");

        // 载荷与 CRC
        var payloadBytesArray = new byte[payloadBytes];
        Array.Copy(bytes, headerBits / 8, payloadBytesArray, 0, payloadBytes);
        byte expectedCrc = bytes[bytes.Length - 1];

        var packet = new DataPacket(type, sourceId, destinationId, seq,
            BitSequence.FromByteArray(payloadBytesArray));

        // CRC 校验：覆盖帧头 + 载荷
        var covered = new byte[bytes.Length - 1];
        Array.Copy(bytes, covered, covered.Length);
        bool crcValid = CrcCalculator.Verify(covered, expectedCrc);

        return crcValid
            ? new FrameParseResult(packet, true, null)
            : new FrameParseResult(packet, false, $"CRC 校验失败（期望 0x{expectedCrc:X2}）。");
    }
}
