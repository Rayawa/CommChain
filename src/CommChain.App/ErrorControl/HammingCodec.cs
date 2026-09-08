using CommChain.App.Common;

namespace CommChain.App.ErrorControl;

/// <summary>汉明码解码结果。</summary>
/// <param name="Data">纠错后提取出的 4 位数据。</param>
/// <param name="ErrorPosition">检测到的错误位置（1~7；0 表示无错）。</param>
/// <param name="WasCorrected">是否发生了纠错。</param>
public sealed record HammingDecodeResult(BitSequence Data, int ErrorPosition, bool WasCorrected);

/// <summary>
/// 汉明码（Hamming Code）(7,4) 前向纠错编解码器。
/// 考察点（第二阶段）：二进制运算、位操作、错误检测与纠正逻辑。
///
/// 采用教科书标准汉明(7,4)码：
///   码字布局  [p1, p2, d1, p3, d2, d3, d4]（位序 1~7，d1 为数据最高位）
///   校验位    p1 = d1⊕d2⊕d4（覆盖位 3,5,7）
///             p2 = d1⊕d3⊕d4（覆盖位 3,6,7）
///             p3 = d2⊕d3⊕d4（覆盖位 5,6,7）
///   校验子    s1 = 位1⊕3⊕5⊕7，s2 = 位2⊕3⊕6⊕7，s3 = 位4⊕5⊕6⊕7
///   错误位置  = s3s2s1（二进制，0 表示无错，可纠正任意单个比特错误）
/// </summary>
public static class HammingCodec
{
    public const int DataBits = 4;    // 每块数据比特数
    public const int CodeBits = 7;    // 编码后比特数

    /// <summary>把 4 位数据编码为 7 位汉明码。</summary>
    public static BitSequence Encode(BitSequence data4)
    {
        if (data4.Count != DataBits)
            throw new ArgumentException($"输入数据必须为 {DataBits} 位。", nameof(data4));

        // 数据位 d1~d4（按输入顺序，d1 为最高位）
        bool d1 = data4[0], d2 = data4[1], d3 = data4[2], d4 = data4[3];

        // 计算校验位（偶校验）
        bool p1 = d1 ^ d2 ^ d4;
        bool p2 = d1 ^ d3 ^ d4;
        bool p3 = d2 ^ d3 ^ d4;

        // 组装码字：p1, p2, d1, p3, d2, d3, d4
        return new BitSequence(new[] { p1, p2, d1, p3, d2, d3, d4 });
    }

    /// <summary>
    /// 对 7 位汉明码进行检错、纠错，并提取 4 位原始数据。
    /// </summary>
    public static HammingDecodeResult Decode(BitSequence code7)
    {
        if (code7.Count != CodeBits)
            throw new ArgumentException($"输入码字必须为 {CodeBits} 位。", nameof(code7));

        // 位序 1~7（下标 0~6）：p1,p2,d1,p3,d2,d3,d4
        bool c1 = code7[0], c2 = code7[1], c3 = code7[2], c4 = code7[3],
             c5 = code7[4], c6 = code7[5], c7 = code7[6];

        // 计算校验子 s1, s2, s3
        bool s1 = c1 ^ c3 ^ c5 ^ c7;
        bool s2 = c2 ^ c3 ^ c6 ^ c7;
        bool s3 = c4 ^ c5 ^ c6 ^ c7;

        int errorPosition = (s3 ? 4 : 0) + (s2 ? 2 : 0) + (s1 ? 1 : 0);

        var corrected = code7.Copy();
        if (errorPosition != 0)
            corrected.FlipAt(errorPosition - 1);   // 纠正出错的比特

        // 提取数据位：位 3,5,6,7（下标 2,4,5,6）
        var data = new BitSequence(new[] { corrected[2], corrected[4], corrected[5], corrected[6] });
        return new HammingDecodeResult(data, errorPosition, errorPosition != 0);
    }

    /// <summary>翻转码字中指定位置的比特（1-based），用于模拟信道错误。</summary>
    public static BitSequence FlipBit(BitSequence code7, int position)
    {
        if (position < 1 || position > CodeBits)
            throw new ArgumentOutOfRangeException(nameof(position), $"位置须在 1~{CodeBits} 之间。");
        var result = code7.Copy();
        result.FlipAt(position - 1);
        return result;
    }

    /// <summary>
    /// 对任意长度（4 的整数倍）的比特流整体做汉明编码：
    /// 按 4 比特分组逐块编码后拼接。返回值：编码后比特流 + 块数。
    /// </summary>
    public static (BitSequence Encoded, int BlockCount) EncodeStream(BitSequence data)
    {
        if (data.Count % DataBits != 0)
            throw new ArgumentException($"比特流长度 {data.Count} 须为 {DataBits} 的整数倍。", nameof(data));

        var result = new BitSequence();
        int blockCount = 0;
        foreach (var chunk in data.SplitChunks(DataBits))
        {
            result.AddRange(Encode(chunk));
            blockCount++;
        }
        return (result, blockCount);
    }

    /// <summary>
    /// 对汉明编码后的比特流整体解码：按 7 比特分组逐块纠错。
    /// 返回：解码后比特流、检测到错误并纠正的块数、总块数。
    /// </summary>
    public static (BitSequence Decoded, int CorrectedBlocks, int TotalBlocks) DecodeStream(BitSequence code)
    {
        if (code.Count % CodeBits != 0)
            throw new ArgumentException($"码流长度 {code.Count} 须为 {CodeBits} 的整数倍。", nameof(code));

        var result = new BitSequence();
        int corrected = 0, total = 0;
        foreach (var chunk in code.SplitChunks(CodeBits))
        {
            var decodeResult = Decode(chunk);
            result.AddRange(decodeResult.Data);
            if (decodeResult.WasCorrected)
                corrected++;
            total++;
        }
        return (result, corrected, total);
    }
}
