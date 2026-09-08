namespace CommChain.App.ErrorControl;

/// <summary>
/// 循环冗余校验（CRC）计算器。
/// 考察点（第二阶段）：位操作（移位、异或）、多项式运算的模拟。
/// </summary>
public static class CrcCalculator
{
    /// <summary>CRC-8 生成多项式：x^8 + x^2 + x + 1（0x07，CRC-8/ATM 同款多项式）。</summary>
    public const byte Polynomial = 0x07;

    /// <summary>
    /// 逐字节移位异或实现 CRC-8：
    /// 对每个字节，先与寄存器高位异或，再左移 8 次；每次若最高位为 1 则与多项式异或。
    /// </summary>
    public static byte ComputeCrc8(byte[] data)
    {
        byte crc = 0x00;   // 初始寄存器
        foreach (byte b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
            {
                if ((crc & 0x80) != 0)
                    crc = (byte)((crc << 1) ^ Polynomial);
                else
                    crc <<= 1;
            }
        }
        return crc;
    }

    /// <summary>校验数据完整性：重算 CRC 并与期望值比对。</summary>
    public static bool Verify(byte[] data, byte expectedCrc) => ComputeCrc8(data) == expectedCrc;

    /// <summary>
    /// 指导书数据包题目中的简易校验和：ASCII 码累加后对 255 取模。
    /// 与 CRC-8 并存，供演示时对比两种校验方法。
    /// </summary>
    public static int SimpleChecksum(string data)
    {
        int sum = 0;
        foreach (char c in data)
            sum += c;
        return sum % 255;
    }
}
