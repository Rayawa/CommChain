using CommChain.App.Common;

namespace CommChain.App.SourceCoding;

/// <summary>
/// 霍夫曼（Huffman）信源编码器。
/// 考察点（第一阶段）：字符频率统计、优先队列（小顶堆）、二叉树、贪心算法。
/// </summary>
public sealed class HuffmanCoder
{
    /// <summary>霍夫曼树的节点。</summary>
    private sealed class HuffmanNode : IComparable<HuffmanNode>
    {
        public char? Symbol;          // 叶子节点携带字符；内部节点为 null
        public int Weight;            // 权重（子树中字符频率之和）
        public HuffmanNode? Left;
        public HuffmanNode? Right;

        public bool IsLeaf => Symbol.HasValue;

        /// <summary>按权重升序比较，供优先队列（小顶堆）使用。</summary>
        public int CompareTo(HuffmanNode? other) =>
            other is null ? 1 : Weight.CompareTo(other.Weight);
    }

    /// <summary>
    /// 统计文本中每个字符的出现频率，并按霍夫曼贪心算法构建码表。
    /// 算法：频率统计 → 每个字符独立成节点入优先队列 → 反复合并权重最小的两棵树 → 从根 DFS 生成前缀码。
    /// </summary>
    public static Dictionary<char, BitSequence> BuildCodeTable(string text)
    {
        // 1. 频率统计
        var frequencies = new Dictionary<char, int>();
        foreach (char c in text)
            frequencies[c] = frequencies.GetValueOrDefault(c) + 1;

        if (frequencies.Count == 0)
            throw new ArgumentException("文本为空，无法构建霍夫曼码表。", nameof(text));

        // 2. 贪心建树：小顶堆每次弹出权重最小的两个节点合并
        var heap = new PriorityQueue<HuffmanNode, HuffmanNode>();
        foreach (var (symbol, weight) in frequencies)
        {
            var leaf = new HuffmanNode { Symbol = symbol, Weight = weight };
            heap.Enqueue(leaf, leaf);
        }

        while (heap.Count > 1)
        {
            var left = heap.Dequeue();
            var right = heap.Dequeue();
            var parent = new HuffmanNode { Weight = left.Weight + right.Weight, Left = left, Right = right };
            heap.Enqueue(parent, parent);
        }

        var root = heap.Dequeue();

        // 3. 深度优先遍历生成码表（左 0 右 1）
        var codeTable = new Dictionary<char, BitSequence>();
        BuildCodeTable(root, new BitSequence(), codeTable);
        return codeTable;
    }

    private static void BuildCodeTable(HuffmanNode node, BitSequence prefix, Dictionary<char, BitSequence> codeTable)
    {
        if (node.IsLeaf)
        {
            // 仅一个字符的特殊情况（不合并时无前缀路径）
            codeTable[node.Symbol!.Value] = prefix.Count == 0 ? new BitSequence("0") : prefix;
            return;
        }

        var leftPrefix = prefix.Copy();
        leftPrefix.Add(false);
        BuildCodeTable(node.Left!, leftPrefix, codeTable);

        var rightPrefix = prefix.Copy();
        rightPrefix.Add(true);
        BuildCodeTable(node.Right!, rightPrefix, codeTable);
    }

    /// <summary>用给定码表把文本编码为比特流。</summary>
    public static BitSequence Encode(string text, Dictionary<char, BitSequence> codeTable)
    {
        var result = new BitSequence();
        foreach (char c in text)
        {
            if (!codeTable.TryGetValue(c, out var code))
                throw new ArgumentException($"字符 '{c}' 不在码表中。", nameof(text));
            result.AddRange(code);
        }
        return result;
    }

    /// <summary>用给定码表把比特流解码为文本（按码表逐位匹配前缀码）。</summary>
    public static string Decode(BitSequence bits, Dictionary<char, BitSequence> codeTable)
    {
        // 反转码表：码字 → 字符
        var reverseTable = new Dictionary<string, char>();
        foreach (var (symbol, code) in codeTable)
            reverseTable[code.ToBinaryString()] = symbol;

        var sb = new System.Text.StringBuilder();
        var buffer = new System.Text.StringBuilder();
        for (int i = 0; i < bits.Count; i++)
        {
            buffer.Append(bits[i] ? '1' : '0');
            if (reverseTable.TryGetValue(buffer.ToString(), out char symbol))
            {
                sb.Append(symbol);
                buffer.Clear();
            }
        }

        if (buffer.Length > 0)
            throw new InvalidOperationException("比特流末尾存在无法匹配的残码，解码失败。");
        return sb.ToString();
    }

    // ---- 码表序列化 / 反序列化（端到端链路中随 META 帧传输） ----

    /// <summary>
    /// 码表序列化格式（定长传输，接收端可自描述解析）：
    /// [条目数:8bit] + 每个条目 [字符:16bit][码长:8bit][码字:码长bit]。
    /// </summary>
    public static BitSequence SerializeCodeTable(Dictionary<char, BitSequence> codeTable)
    {
        var bits = new BitSequence();
        bits.AddInt(codeTable.Count, 8);
        foreach (var (symbol, code) in codeTable)
        {
            bits.AddInt(symbol, 16);
            bits.AddInt(code.Count, 8);
            bits.AddRange(code);
        }
        return bits;
    }

    /// <summary>
    /// 从比特流反序列化码表。返回（码表, 消耗的比特数），供调用方继续解析后续字段。
    /// </summary>
    public static (Dictionary<char, BitSequence> Table, int BitsConsumed) DeserializeCodeTable(BitSequence bits)
    {
        int pos = 0;
        int ReadInt(int bitCount)
        {
            int value = 0;
            for (int i = 0; i < bitCount; i++)
                value = (value << 1) | (bits[pos + i] ? 1 : 0);
            pos += bitCount;
            return value;
        }

        int entryCount = ReadInt(8);
        var table = new Dictionary<char, BitSequence>();
        for (int i = 0; i < entryCount; i++)
        {
            char symbol = (char)ReadInt(16);
            int codeLength = ReadInt(8);
            var code = new BitSequence();
            for (int j = 0; j < codeLength; j++)
            {
                code.Add(bits[pos]);
                pos++;
            }
            table[symbol] = code;
        }
        return (table, pos);
    }
}
