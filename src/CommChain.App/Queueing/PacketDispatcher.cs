using CommChain.App.Common;
using CommChain.App.PacketLayer;

namespace CommChain.App.Queueing;

/// <summary>
/// 多信源数据包合并与分流处理器。
/// 考察点（综合项目一）：队列（LinkedListQueue）、字典（Dictionary）、
/// 随机数据包生成、按目的地 ID 分流。
/// </summary>
public static class PacketDispatcher
{
    /// <summary>
    /// 随机生成指定数量的数据包（模拟多个信源汇聚的流量）。
    /// </summary>
    /// <param name="count">包数量。</param>
    /// <param name="rng">随机数发生器。</param>
    /// <param name="sourceIdMin">源 ID 范围下限（含）。</param>
    /// <param name="sourceIdMax">源 ID 范围上限（含）。</param>
    /// <param name="destIdMin">目的 ID 范围下限（含）。</param>
    /// <param name="destIdMax">目的 ID 范围上限（含）。</param>
    public static LinkedListQueue<DataPacket> GeneratePackets(
        int count, Random rng,
        int sourceIdMin = 101, int sourceIdMax = 103,
        int destIdMin = 201, int destIdMax = 204)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        var queue = new LinkedListQueue<DataPacket>();
        for (int i = 0; i < count; i++)
        {
            byte sourceId = (byte)rng.Next(sourceIdMin, sourceIdMax + 1);
            byte destId = (byte)rng.Next(destIdMin, destIdMax + 1);
            string payloadText = $"数据_{rng.Next(1, 1000)}";
            // 文本按 UTF-8 编码转字节，再展开为比特流（长度自动 8 对齐）
            var payload = BitSequence.FromByteArray(System.Text.Encoding.UTF8.GetBytes(payloadText));

            var packet = new DataPacket(PacketType.Data, sourceId, destId, (byte)(i + 1), payload);
            queue.Enqueue(packet);
        }
        return queue;
    }

    /// <summary>
    /// 从输入队列依次取出数据包，按 destinationId 分流到不同的输出队列。
    /// </summary>
    /// <returns>目的 ID → 输出队列 的映射。</returns>
    public static Dictionary<int, LinkedListQueue<DataPacket>> Dispatch(LinkedListQueue<DataPacket> inputQueue)
    {
        var outputQueues = new Dictionary<int, LinkedListQueue<DataPacket>>();
        while (!inputQueue.IsEmpty)
        {
            var packet = inputQueue.Dequeue();
            if (!outputQueues.TryGetValue(packet.DestinationId, out var queue))
            {
                queue = new LinkedListQueue<DataPacket>();
                outputQueues[packet.DestinationId] = queue;
            }
            queue.Enqueue(packet);
        }
        return outputQueues;
    }

    /// <summary>打印各目的输出队列的内容（按键 ID 升序）。</summary>
    public static void PrintOutputQueues(Dictionary<int, LinkedListQueue<DataPacket>> outputQueues)
    {
        foreach (var (destId, queue) in outputQueues.OrderBy(kv => kv.Key))
        {
            Console.WriteLine($"  [目的地 ID {destId}] 队列中共 {queue.Count} 个包：");
            foreach (var packet in queue.ToList())
                Console.WriteLine($"    {packet}");
        }
    }
}
