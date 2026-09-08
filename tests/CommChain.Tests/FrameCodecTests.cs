using CommChain.App.Common;
using CommChain.App.PacketLayer;

namespace CommChain.Tests;

public class FrameCodecTests
{
    private static DataPacket CreateSamplePacket(byte seq = 1)
    {
        var payload = BitSequence.FromByteArray(
            System.Text.Encoding.UTF8.GetBytes("数据包内容-Data"));
        return new DataPacket(PacketType.Data, 0, 7, seq, payload);
    }

    [Fact]
    public void 组帧拆帧往返一致()
    {
        var packet = CreateSamplePacket();
        var frame = FrameCodec.ToFrame(packet);

        // 帧长 = 头 48 + 载荷 + CRC 8
        Assert.Equal(DataPacket.HeaderBitLength + packet.Payload.Count + 8, frame.Count);

        var result = FrameCodec.TryParse(frame);
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(packet.Type, result.Packet!.Type);
        Assert.Equal(packet.SourceId, result.Packet.SourceId);
        Assert.Equal(packet.DestinationId, result.Packet.DestinationId);
        Assert.Equal(packet.Seq, result.Packet.Seq);
        Assert.Equal(packet.Payload.ToBinaryString(), result.Packet.Payload.ToBinaryString());
    }

    [Fact]
    public void 载荷篡改被CRC检出()
    {
        var frame = FrameCodec.ToFrame(CreateSamplePacket());
        frame.FlipAt(DataPacket.HeaderBitLength + 3);   // 翻转载荷区第 4 个比特

        var result = FrameCodec.TryParse(frame);
        Assert.False(result.CrcValid);
        Assert.False(result.Success);
        Assert.Contains("CRC", result.ErrorMessage);
    }

    [Fact]
    public void 帧头篡改被CRC检出()
    {
        var frame = FrameCodec.ToFrame(CreateSamplePacket());
        frame.FlipAt(2);   // 翻转帧头中 type 字段的比特

        var result = FrameCodec.TryParse(frame);
        Assert.False(result.CrcValid);
    }

    [Fact]
    public void 过短帧解析失败()
    {
        var result = FrameCodec.TryParse(new BitSequence("10110010"));
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void 长度字段与实际不符时解析失败()
    {
        var frame = FrameCodec.ToFrame(CreateSamplePacket());
        var truncated = frame.Take(frame.Count - 8);   // 切掉 CRC 字节

        var result = FrameCodec.TryParse(truncated);
        Assert.False(result.Success);
        Assert.Contains("长度不符", result.ErrorMessage);
    }

    [Fact]
    public void 载荷非8对齐构造抛异常()
    {
        Assert.Throws<ArgumentException>(() =>
            new DataPacket(PacketType.Data, 0, 7, 1, new BitSequence("101")));
    }

    [Fact]
    public void 元信息帧与数据帧可区分()
    {
        var metaPacket = new DataPacket(PacketType.Meta, 0, 7, 0,
            new BitSequence("00000000"));
        var result = FrameCodec.TryParse(FrameCodec.ToFrame(metaPacket));
        Assert.True(result.Success);
        Assert.Equal(PacketType.Meta, result.Packet!.Type);
    }
}
