using CommChain.App.Common;

namespace CommChain.App.Modulation;

/// <summary>
/// 调制方式抽象基类：把二进制比特流调制成模拟信号（采样电压序列），
/// 以及把（可能带噪的）模拟信号解调回比特流。
/// 考察点（第三阶段）：信号处理、数学函数、面向对象设计（继承与多态）。
/// </summary>
public abstract class Modulator
{
    /// <summary>调制方式名称。</summary>
    public abstract string Name { get; }

    /// <summary>每比特采样点数。</summary>
    public abstract int SamplesPerBit { get; }

    /// <summary>调制：比特流 → 采样信号。</summary>
    public abstract double[] Modulate(BitSequence bits);

    /// <summary>解调：采样信号 → 比特流。</summary>
    public abstract BitSequence Demodulate(double[] signal);
}

/// <summary>
/// ASK（幅移键控）调制：1 → 高电平 10V，0 → 低电平 0V，每比特 1 个采样。
/// 解调：阈值 5V 判决。
/// 即综合项目二的原始电平调制方式。
/// </summary>
public sealed class AskModulator : Modulator
{
    public const double HighVoltage = 10.0;
    public const double LowVoltage = 0.0;
    public const double Threshold = 5.0;

    public override string Name => "ASK 幅移键控";
    public override int SamplesPerBit => 1;

    public override double[] Modulate(BitSequence bits)
    {
        var signal = new double[bits.Count];
        for (int i = 0; i < bits.Count; i++)
            signal[i] = bits[i] ? HighVoltage : LowVoltage;
        return signal;
    }

    public override BitSequence Demodulate(double[] signal)
    {
        var bits = new BitSequence();
        foreach (double sample in signal)
            bits.Add(sample >= Threshold);
        return bits;
    }
}

/// <summary>
/// FSK（频移键控）调制：1 → 频率 f1 = 2000Hz，0 → 频率 f2 = 1000Hz，
/// 每比特采样 N = 8 点正弦波。
/// 解调：对每个比特窗口分别与 f1/f2 参考正弦做内积（相关检测），
/// 相关值大者判定为对应比特——对高斯噪声有较强的抗扰能力。
/// </summary>
public sealed class FskModulator : Modulator
{
    public const double Frequency1 = 2000.0;   // 比特 1 的频率
    public const double Frequency2 = 1000.0;   // 比特 0 的频率
    public const double Amplitude = 10.0;      // 幅度 10V（与 ASK 一致）
    public const double SampleRate = 8000.0;   // 采样率 8kHz

    public override string Name => "FSK 频移键控";
    public override int SamplesPerBit => 8;

    public override double[] Modulate(BitSequence bits)
    {
        var signal = new double[bits.Count * SamplesPerBit];
        for (int i = 0; i < bits.Count; i++)
        {
            double frequency = bits[i] ? Frequency1 : Frequency2;
            for (int s = 0; s < SamplesPerBit; s++)
            {
                double t = (i * SamplesPerBit + s) / SampleRate;
                signal[i * SamplesPerBit + s] = Amplitude * Math.Sin(2 * Math.PI * frequency * t);
            }
        }
        return signal;
    }

    public override BitSequence Demodulate(double[] signal)
    {
        if (signal.Length % SamplesPerBit != 0)
            throw new ArgumentException($"信号长度 {signal.Length} 须为每比特采样数 {SamplesPerBit} 的整数倍。", nameof(signal));

        var bits = new BitSequence();
        for (int i = 0; i < signal.Length; i += SamplesPerBit)
        {
            // 相关检测：窗口内信号与两个参考频率的内积，幅值大者胜出
            double correlation1 = 0, correlation2 = 0;
            for (int s = 0; s < SamplesPerBit; s++)
            {
                double t = (i + s) / SampleRate;
                double sample = signal[i + s];
                correlation1 += sample * Math.Sin(2 * Math.PI * Frequency1 * t);
                correlation2 += sample * Math.Sin(2 * Math.PI * Frequency2 * t);
            }
            bits.Add(correlation1 >= correlation2);
        }
        return bits;
    }
}

/// <summary>
/// 信道模型：模拟传输过程中的噪声与比特错误。
/// 考察点（第二阶段/第三阶段）：随机过程模拟、错误引入。
/// </summary>
public static class Channel
{
    /// <summary>
    /// 对模拟信号叠加高斯白噪声（Box-Muller 变换生成标准正态分布）。
    /// </summary>
    /// <param name="signal">原始信号。</param>
    /// <param name="rng">随机数发生器（固定种子保证可复现）。</param>
    /// <param name="noiseLevel">噪声标准差（伏特）。</param>
    public static double[] AddNoise(double[] signal, Random rng, double noiseLevel)
    {
        if (noiseLevel <= 0)
            return (double[])signal.Clone();

        var noisy = new double[signal.Length];
        for (int i = 0; i < signal.Length; i++)
        {
            // Box-Muller：两个均匀随机数 → 一个标准正态样本
            double u1 = 1.0 - rng.NextDouble();
            double u2 = rng.NextDouble();
            double gaussian = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            noisy[i] = signal[i] + noiseLevel * gaussian;
        }
        return noisy;
    }

    /// <summary>
    /// 按给定误比特率（BER）对比特流随机翻转。
    /// </summary>
    /// <param name="bits">原始比特流（不改动）。</param>
    /// <param name="rng">随机数发生器。</param>
    /// <param name="ber">每个比特被翻转的概率（0~1）。</param>
    /// <param name="maxErrors">翻转比特数上限（null 表示不限）；用于"每帧至多 1 错"等可控场景。</param>
    /// <returns>（翻转后的比特流, 实际翻转的比特数）</returns>
    public static (BitSequence Corrupted, int ErrorCount) IntroduceBitErrors(
        BitSequence bits, Random rng, double ber, int? maxErrors = null)
    {
        var corrupted = bits.Copy();
        int errorCount = 0;
        for (int i = 0; i < corrupted.Count; i++)
        {
            if (maxErrors.HasValue && errorCount >= maxErrors.Value)
                break;   // 达到该帧错误数上限，不再引入更多错误

            if (rng.NextDouble() < ber)
            {
                corrupted.FlipAt(i);
                errorCount++;
            }
        }
        return (corrupted, errorCount);
    }
}
