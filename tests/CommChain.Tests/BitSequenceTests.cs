using CommChain.App.Common;

namespace CommChain.Tests;

public class BitSequenceTests
{
    [Fact]
    public void 字符串构造与输出往返()
    {
        var bits = new BitSequence("10110010");
        Assert.Equal(8, bits.Count);
        Assert.Equal("10110010", bits.ToBinaryString());
        Assert.True(bits[0]);    // 最高位为 1
        Assert.False(bits[1]);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1011")]
    [InlineData("1111000011110000")]
    public void 字符串构造往返_多组长度(string input)
    {
        var bits = new BitSequence(input);
        Assert.Equal(input, bits.ToBinaryString());
    }

    [Fact]
    public void 非法字符抛异常()
    {
        Assert.Throws<ArgumentException>(() => new BitSequence("10x1"));
    }

    [Fact]
    public void 整数与比特流互转()
    {
        var bits = BitSequence.FromInt(0xB5, 8);
        Assert.Equal("10110101", bits.ToBinaryString());
        Assert.Equal(0xB5, bits.ToInt());
    }

    [Fact]
    public void 字节数组与比特流互转()
    {
        byte[] bytes = { 0x00, 0xFF, 0xA5, 0x5A };
        var bits = BitSequence.FromByteArray(bytes);
        Assert.Equal(32, bits.Count);
        Assert.Equal("00000000 11111111 10100101 01011010", bits.ToBinaryString(true));
        Assert.Equal(bytes, bits.ToByteArray());
    }

    [Fact]
    public void 非8倍数转字节数组抛异常()
    {
        var bits = new BitSequence("101");
        Assert.Throws<InvalidOperationException>(() => bits.ToByteArray());
    }

    [Fact]
    public void 分组切分()
    {
        var bits = new BitSequence("101100101011");
        var chunks = bits.SplitChunks(4);
        Assert.Equal(3, chunks.Count);
        Assert.Equal("1011", chunks[0].ToBinaryString());
        Assert.Equal("0010", chunks[1].ToBinaryString());
        Assert.Equal("1011", chunks[2].ToBinaryString());
    }

    [Fact]
    public void 分组切分_长度不整除时最后一块变短()
    {
        var bits = new BitSequence("101100101");
        var chunks = bits.SplitChunks(4);
        Assert.Equal(3, chunks.Count);
        Assert.Equal(1, chunks[2].Count);
    }

    [Fact]
    public void 切片与截取()
    {
        var bits = new BitSequence("10110010");
        Assert.Equal("1011", bits.Take(4).ToBinaryString());
        Assert.Equal("0010", bits.Slice(4, 4).ToBinaryString());
        Assert.Throws<ArgumentOutOfRangeException>(() => bits.Slice(6, 4));
    }

    [Fact]
    public void 补零对齐()
    {
        var bits = new BitSequence("10110");
        var padded = bits.PadToMultipleOf(8);
        Assert.Equal(8, padded.Count);
        Assert.Equal("10110000", padded.ToBinaryString());
        Assert.Equal(5, bits.Count);   // 原序列不变
    }

    [Fact]
    public void 拼接()
    {
        var a = new BitSequence("101");
        var b = new BitSequence("011");
        Assert.Equal("101011", a.Concat(b).ToBinaryString());
        Assert.Equal(3, a.Count);   // 原序列不变
    }

    [Fact]
    public void 翻转指定比特()
    {
        var bits = new BitSequence("1010");
        bits.FlipAt(0);
        bits.FlipAt(3);
        Assert.Equal("0011", bits.ToBinaryString());
    }

    [Fact]
    public void 深拷贝互不影响()
    {
        var bits = new BitSequence("1010");
        var copy = bits.Copy();
        copy.FlipAt(0);
        Assert.Equal("1010", bits.ToBinaryString());
        Assert.Equal("0010", copy.ToBinaryString());
    }
}
