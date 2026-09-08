using CommChain.App.Common;
using CommChain.App.ErrorControl;

namespace CommChain.Tests;

public class CrcCalculatorTests
{
    [Fact]
    public void 手算已知值验证()
    {
        // 手算：0x31（字符 '1'）经多项式 0x07 逐位移位异或 → 0x97
        byte crc = CrcCalculator.ComputeCrc8(new byte[] { 0x31 });
        Assert.Equal(0x97, crc);
    }

    [Fact]
    public void 校验成功与篡改检测()
    {
        byte[] data = System.Text.Encoding.UTF8.GetBytes("Hello, CommChain!");
        byte crc = CrcCalculator.ComputeCrc8(data);
        Assert.True(CrcCalculator.Verify(data, crc));

        var corrupted = (byte[])data.Clone();
        corrupted[corrupted.Length / 2] ^= 0x01;   // 篡改一个比特
        Assert.False(CrcCalculator.Verify(corrupted, crc));
    }

    [Fact]
    public void 空数据CRC为零()
    {
        Assert.Equal(0x00, CrcCalculator.ComputeCrc8(Array.Empty<byte>()));
    }

    [Fact]
    public void 简易校验和按ASCII累加取模()
    {
        // 'H'=72, 'e'=101, 'l'=108, 'l'=108, 'o'=111 → 500 % 255 = 245
        Assert.Equal(245, CrcCalculator.SimpleChecksum("Hello"));
        Assert.Equal(0, CrcCalculator.SimpleChecksum(""));
    }
}

public class HammingCodecTests
{
    [Theory]
    [InlineData("0000", "0000000")]
    [InlineData("1111", "1111111")]
    [InlineData("1011", "0110011")]   // 标准汉明(7,4)手算结果
    [InlineData("0101", "0100101")]
    public void 编码已知向量(string data, string expectedCode)
    {
        var encoded = HammingCodec.Encode(new BitSequence(data));
        Assert.Equal(expectedCode, encoded.ToBinaryString());
    }

    [Fact]
    public void 全部16种数据无错往返()
    {
        for (int value = 0; value < 16; value++)
        {
            var data = BitSequence.FromInt(value, 4);
            var encoded = HammingCodec.Encode(data);
            var result = HammingCodec.Decode(encoded);

            Assert.Equal(0, result.ErrorPosition);
            Assert.False(result.WasCorrected);
            Assert.Equal(data.ToBinaryString(), result.Data.ToBinaryString());
        }
    }

    [Fact]
    public void 任意单比特错误均可定位并纠正()
    {
        var data = new BitSequence("1011");
        var encoded = HammingCodec.Encode(data);

        for (int position = 1; position <= HammingCodec.CodeBits; position++)
        {
            var corrupted = HammingCodec.FlipBit(encoded, position);
            var result = HammingCodec.Decode(corrupted);

            Assert.Equal(position, result.ErrorPosition);
            Assert.True(result.WasCorrected);
            Assert.Equal(data.ToBinaryString(), result.Data.ToBinaryString());
        }
    }

    [Fact]
    public void 双比特错误无法正确定位但能检出()
    {
        // 双错时校验子非零（检出），但纠正结果不再等于原数据
        var data = new BitSequence("1011");
        var corrupted = HammingCodec.FlipBit(HammingCodec.Encode(data), 1);
        corrupted = HammingCodec.FlipBit(corrupted, 2);
        var result = HammingCodec.Decode(corrupted);

        Assert.NotEqual(0, result.ErrorPosition);
        Assert.NotEqual(data.ToBinaryString(), result.Data.ToBinaryString());
    }

    [Fact]
    public void 比特流整体编解码往返()
    {
        var data = new BitSequence("1011001010110010");
        var (encoded, blockCount) = HammingCodec.EncodeStream(data);
        Assert.Equal(4, blockCount);
        Assert.Equal(4 * 7, encoded.Count);

        var (decoded, corrected, total) = HammingCodec.DecodeStream(encoded);
        Assert.Equal(data.ToBinaryString(), decoded.ToBinaryString());
        Assert.Equal(0, corrected);
        Assert.Equal(4, total);
    }

    [Fact]
    public void 非法长度输入抛异常()
    {
        Assert.Throws<ArgumentException>(() => HammingCodec.Encode(new BitSequence("101")));
        Assert.Throws<ArgumentException>(() => HammingCodec.Decode(new BitSequence("10110")));
        Assert.Throws<ArgumentException>(() => HammingCodec.EncodeStream(new BitSequence("1011" + "10")));
        Assert.Throws<ArgumentOutOfRangeException>(() => HammingCodec.FlipBit(new BitSequence("1011001"), 8));
    }
}
