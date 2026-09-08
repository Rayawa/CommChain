using CommChain.App.Common;
using CommChain.App.SourceCoding;

namespace CommChain.Tests;

public class HuffmanCoderTests
{
    [Theory]
    [InlineData("hello world")]
    [InlineData("程序设计实践")]
    [InlineData("aaaaaaaaaa")]                 // 单字符文本（特殊分支）
    [InlineData("The quick brown fox jumps over the lazy dog")]
    [InlineData("通信原理与信道编码课程设计")]
    public void 编码解码往返一致(string text)
    {
        var codeTable = HuffmanCoder.BuildCodeTable(text);
        var encoded = HuffmanCoder.Encode(text, codeTable);
        string decoded = HuffmanCoder.Decode(encoded, codeTable);
        Assert.Equal(text, decoded);
    }

    [Fact]
    public void 码表频率统计正确()
    {
        // "aab"：a 出现 2 次，b 出现 1 次 → a 的码字不长于 b
        var codeTable = HuffmanCoder.BuildCodeTable("aab");
        Assert.Equal(2, codeTable.Count);
        Assert.True(codeTable['a'].Count <= codeTable['b'].Count);
    }

    [Fact]
    public void 码字满足前缀码性质()
    {
        // 任意码字都不能是另一码字的前缀
        var codeTable = HuffmanCoder.BuildCodeTable("this is a prefix code property test");
        foreach (var (symbol1, code1) in codeTable)
            foreach (var (symbol2, code2) in codeTable)
                if (!ReferenceEquals(code1, code2) && code1.Count <= code2.Count)
                    Assert.False(code2.ToBinaryString().StartsWith(code1.ToBinaryString()),
                        $"码字 {code2.ToBinaryString()} 以 {code1.ToBinaryString()} 为前缀，违反前缀码性质");
    }

    [Fact]
    public void 高重复文本具备压缩效果()
    {
        string text = new string('A', 200) + "BCDE";
        var codeTable = HuffmanCoder.BuildCodeTable(text);
        var encoded = HuffmanCoder.Encode(text, codeTable);
        Assert.True(encoded.Count < text.Length * 8, "重复度高的文本应被压缩到 8bit/字符以下");
    }

    [Fact]
    public void 码表序列化往返一致()
    {
        var codeTable = HuffmanCoder.BuildCodeTable("通信网络路由与差错控制");
        var serialized = HuffmanCoder.SerializeCodeTable(codeTable);
        var (table, bitsConsumed) = HuffmanCoder.DeserializeCodeTable(serialized);

        Assert.Equal(serialized.Count, bitsConsumed);
        Assert.Equal(codeTable.Count, table.Count);
        foreach (var (symbol, code) in codeTable)
        {
            Assert.True(table.TryGetValue(symbol, out var recovered));
            Assert.Equal(code.ToBinaryString(), recovered.ToBinaryString());
        }
    }

    [Fact]
    public void 空文本抛异常()
    {
        Assert.Throws<ArgumentException>(() => HuffmanCoder.BuildCodeTable(""));
    }

    [Fact]
    public void 解码残码抛异常()
    {
        var codeTable = HuffmanCoder.BuildCodeTable("abc");
        var bits = new BitSequence("111111111");   // 尾随码无对应字符
        Assert.Throws<InvalidOperationException>(() => HuffmanCoder.Decode(bits, codeTable));
    }
}
