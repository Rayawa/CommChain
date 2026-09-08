using System.Text;
using CommChain.App;

// 保证中文界面在任何终端编码下正常显示
Console.OutputEncoding = Encoding.UTF8;

while (true)
{
    Console.WriteLine();
    Console.WriteLine("══════════════════════════════════════════════════════════");
    Console.WriteLine("         数字通信系统模拟器 CommChain —— 程序设计实践综合项目");
    Console.WriteLine("══════════════════════════════════════════════════════════");
    Console.WriteLine("  1. 霍夫曼编码演示          第一阶段：频率统计/优先队列/二叉树/贪心");
    Console.WriteLine("  2. 链表队列演示            第一阶段：手写链表/队列/异常处理");
    Console.WriteLine("  3. CRC 校验演示            第二阶段：位操作/多项式运算");
    Console.WriteLine("  4. 汉明码纠错演示          第二阶段：前向纠错/错误定位");
    Console.WriteLine("  5. 网络拓扑与最短路径演示  第三阶段：图/Dijkstra/Floyd/文件 I/O");
    Console.WriteLine("  6. ASK/FSK 调制解调演示    第三阶段：信号处理/信道噪声/多态");
    Console.WriteLine("  7. 数据包合并与分流演示    综合项目一：队列/字典/随机生成");
    Console.WriteLine("  8. 端到端完整链路模拟      ★核心：综合项目二主线 + 全部阶段特性");
    Console.WriteLine("  0. 退出");
    Console.WriteLine("══════════════════════════════════════════════════════════");

    Console.Write("请输入菜单编号：");
    string? choice = Console.ReadLine()?.Trim();

    switch (choice)
    {
        case "1":
            MenuDemos.HuffmanDemo();
            break;
        case "2":
            MenuDemos.QueueDemo();
            break;
        case "3":
            MenuDemos.CrcDemo();
            break;
        case "4":
            MenuDemos.HammingDemo();
            break;
        case "5":
            MenuDemos.TopologyDemo();
            break;
        case "6":
            MenuDemos.ModulationDemo();
            break;
        case "7":
            MenuDemos.DispatcherDemo();
            break;
        case "8":
            MenuDemos.EndToEndDemo();
            break;
        case "0":
            Console.WriteLine("再见！");
            return;
        default:
            Console.WriteLine("✘ 无效的菜单编号，请重新输入。");
            break;
    }
}
