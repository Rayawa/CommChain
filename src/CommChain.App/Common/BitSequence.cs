using System.Collections;

namespace CommChain.App.Common;

/// <summary>
/// 全链路统一的比特序列容器。
/// 所有模块之间传递二进制数据时均使用本类型，避免 string / byte[] / bool[] 混用带来的歧义。
/// 内部以 List&lt;bool&gt; 存储，索引 0 为最高有效位（MSB first）。
/// </summary>
public sealed class BitSequence : IEnumerable<bool>
{
    private readonly List<bool> _bits = new();

    /// <summary>比特总数。</summary>
    public int Count => _bits.Count;

    /// <summary>按索引访问比特（0 为最高位）。</summary>
    public bool this[int index] => _bits[index];

    /// <summary>按 MSB first 顺序枚举全部比特。</summary>
    public IEnumerator<bool> GetEnumerator() => _bits.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public BitSequence()
    {
    }

    /// <summary>由布尔序列构造。</summary>
    public BitSequence(IEnumerable<bool> bits)
    {
        _bits.AddRange(bits);
    }

    /// <summary>由 "0"/"1" 字符串构造（如 "1011"）。</summary>
    public BitSequence(string bits)
    {
        foreach (char c in bits)
        {
            if (c != '0' && c != '1')
                throw new ArgumentException($"非法比特字符 '{c}'，只允许 0 或 1。", nameof(bits));
            _bits.Add(c == '1');
        }
    }

    /// <summary>按整数值与指定位数构造（不足位数高位补零，超出则截断高位）。</summary>
    public static BitSequence FromInt(int value, int bitCount)
    {
        var result = new BitSequence();
        for (int i = bitCount - 1; i >= 0; i--)
            result._bits.Add(((value >> i) & 1) == 1);
        return result;
    }

    /// <summary>由字节数组构造（每个字节按 MSB first 展开为 8 比特）。</summary>
    public static BitSequence FromByteArray(byte[] bytes)
    {
        var result = new BitSequence();
        foreach (byte b in bytes)
            result.AddInt(b, 8);
        return result;
    }

    /// <summary>末尾追加一个比特。</summary>
    public void Add(bool bit) => _bits.Add(bit);

    /// <summary>末尾追加一段布尔序列。</summary>
    public void AddRange(IEnumerable<bool> bits) => _bits.AddRange(bits);

    /// <summary>把整数值的低 bitCount 位按 MSB first 追加到末尾。</summary>
    public void AddInt(int value, int bitCount)
    {
        for (int i = bitCount - 1; i >= 0; i--)
            _bits.Add(((value >> i) & 1) == 1);
    }

    /// <summary>拼接另一个比特序列，返回新序列（原序列不变）。</summary>
    public BitSequence Concat(BitSequence other)
    {
        var result = new BitSequence(_bits);
        result._bits.AddRange(other._bits);
        return result;
    }

    /// <summary>按定长分组，返回若干子序列（长度不足整组时返回余下部分）。</summary>
    public List<BitSequence> SplitChunks(int chunkSize)
    {
        var chunks = new List<BitSequence>();
        for (int i = 0; i < _bits.Count; i += chunkSize)
        {
            int take = Math.Min(chunkSize, _bits.Count - i);
            chunks.Add(new BitSequence(_bits.GetRange(i, take)));
        }
        return chunks;
    }

    /// <summary>翻转指定位置的比特（信道错误模拟用）。</summary>
    public void FlipAt(int index) => _bits[index] = !_bits[index];

    /// <summary>按 8 比特一组转换为字节数组（要求长度是 8 的整数倍）。</summary>
    public byte[] ToByteArray()
    {
        if (_bits.Count % 8 != 0)
            throw new InvalidOperationException($"比特流长度 {_bits.Count} 不是 8 的整数倍，无法转为字节数组。");
        var bytes = new byte[_bits.Count / 8];
        for (int i = 0; i < bytes.Length; i++)
        {
            int value = 0;
            for (int j = 0; j < 8; j++)
                value = (value << 1) | (_bits[i * 8 + j] ? 1 : 0);
            bytes[i] = (byte)value;
        }
        return bytes;
    }

    /// <summary>把整段比特流按大端序解析为整数（长度须 ≤ 32）。</summary>
    public int ToInt()
    {
        if (_bits.Count > 32)
            throw new InvalidOperationException($"比特流长度 {_bits.Count} 超过 32，无法转为 int。");
        int value = 0;
        foreach (bool bit in _bits)
            value = (value << 1) | (bit ? 1 : 0);
        return value;
    }

    /// <summary>末尾补零至长度是 n 的整数倍，返回补齐后的新序列。</summary>
    public BitSequence PadToMultipleOf(int n)
    {
        var result = new BitSequence(_bits);
        int pad = (n - result._bits.Count % n) % n;
        for (int i = 0; i < pad; i++)
            result._bits.Add(false);
        return result;
    }

    /// <summary>截取前 length 个比特，返回新序列。</summary>
    public BitSequence Take(int length)
    {
        if (length < 0 || length > _bits.Count)
            throw new ArgumentOutOfRangeException(nameof(length));
        return new BitSequence(_bits.GetRange(0, length));
    }

    /// <summary>截取从 start 开始、长度为 length 的子序列。</summary>
    public BitSequence Slice(int start, int length)
    {
        if (start < 0 || length < 0 || start + length > _bits.Count)
            throw new ArgumentOutOfRangeException(nameof(start), "切片范围越界。");
        return new BitSequence(_bits.GetRange(start, length));
    }

    /// <summary>深拷贝。</summary>
    public BitSequence Copy() => new(_bits);

    /// <summary>转为 "0101..." 字符串；可指定每 8 位加一个空格便于阅读。</summary>
    public string ToBinaryString(bool groupByByte = false)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < _bits.Count; i++)
        {
            sb.Append(_bits[i] ? '1' : '0');
            if (groupByByte && (i + 1) % 8 == 0 && i + 1 < _bits.Count)
                sb.Append(' ');
        }
        return sb.ToString();
    }

    public override string ToString() => ToBinaryString();
}
