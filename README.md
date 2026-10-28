# CommChain — Digital Communication System Simulator

English | [简体中文](README_zh.md)


A comprehensive programming-practice project implemented in C#. It uses **the complete communication chain from Comprehensive Project 2, “Signal Modulation and Demodulation (Text to Binary),” in the course guide as its framework**, integrating features from Stages 1–3 and Comprehensive Project 1 into an interactive end-to-end digital communication simulator.

- Language/framework: C# / .NET (implemented entirely with the BCL, with **no third-party dependencies**)
- Interaction: console menus, with individual module demonstrations and a one-command end-to-end simulation
- Tests: xUnit unit tests, including reproducible end-to-end assertions with a fixed random seed

## Usage

### 1. Requirements

- Any machine with the .NET SDK installed (macOS / Windows / Linux); target framework: .NET 10
- No third-party NuGet dependencies in the application; only the test project references xUnit. Build and run directly.

### 2. Install the .NET SDK

```bash
# macOS（推荐，Homebrew）
brew install dotnet

# 或从官网下载安装包：https://dotnet.microsoft.com/download
# Windows / Linux 均可使用官方安装器，安装完成后验证：
dotnet --version
```

### 3. Run the Application

```bash
# 在项目根目录（CommChain/）下执行
dotnet run --project src/CommChain.App
```

The main menu appears after startup. Enter a number (1–8) to select a demonstration, or 0 to exit.

### 4. Run All Unit Tests

```bash
# 在项目根目录（CommChain/）下执行
dotnet test
```

All 50+ test cases are expected to pass. Tests use a fixed random seed (42) for consistent, reproducible results across machines.

### 5. Main Menu Guide

| Number | Demonstration | Input | Output to Examine |
|------|--------|----------|----------------|
| 1 | Huffman coding | A text string | Character frequencies → prefix-code table → compression ratio (fixed 8bit/character versus variable-length codes) → decoding and reconstruction check |
| 2 | Linked-list queue | None | Enqueue/dequeue operations, changes in Count, and a caught empty-queue exception without terminating the program |
| 3 | CRC checking | A string to transmit | Sender-side CRC-8 (0x07) and simple checksum → simulated modification → receiver-side recomputation and error detection |
| 4 | Hamming error correction | 4 data bits (for example, 1011) and a bit position to flip (1–7; 0 means no flip) | Generation of a 7-bit codeword → syndrome-based error location → corrected data |
| 5 | Network topology | Source/destination node numbers (0–7) | Adjacency matrix → Dijkstra shortest path → cross-check against Floyd all-pairs results |
| 6 | ASK/FSK modulation | 8 bits (for example, 10110010) | ASCII waveforms for both modulation methods → added noise → demodulation without bit errors |
| 7 | Packet merging and dispatch | Number of packets (1–100) | Random packets from multiple sources → merged queue → dispatch to output queues by destination ID |
| 8 | Complete end-to-end chain | Text, mode (1 simplified / 2 complete), and error setting (0/1/2) | All nine stages and link statistics: compression ratio, BER, corrected errors, CRC failures and route |

### 6. Suggested End-to-End Demonstration Sequence (Menu 8)

1. Start with **simplified mode** (mode 1), corresponding to the original four-function chain in Comprehensive Project 2: text → binary → ASK levels → threshold demodulation → text. Verify the basic chain.
2. Then run **complete mode** (mode 2) under all three error settings and compare the link behavior:

| Setting | Input | Expected Behavior |
|------|------|----------|
| 0: no errors | Any text | Lossless recovery; error counts, correction counts and CRC failures are all 0 |
| 1: a small number of correctable errors | Any text | The channel introduces a few single-bit errors, all corrected by Hamming coding; text is recovered losslessly |
| 2: errors beyond correction capacity | Any text | Some 7bit blocks have two errors; Hamming correction fails and CRC rejects frames, so recovered text differs from the original, demonstrating the final integrity check |

### 7. Frequently Asked Questions

- **`dotnet` is not found**: install the .NET SDK or add it to PATH; see Step 2. On macOS, reopen the terminal after Homebrew installation.
- **`topology.txt` is not found at runtime**: run `dotnet run --project src/CommChain.App` from the repository root. The csproj copies the topology file into the build output, so running CommChain.App.dll directly from that directory also works.
- **Chinese text is garbled**: the entry point sets `Console.OutputEncoding = UTF-8`. If necessary, set the terminal encoding to UTF-8; Windows Terminal and the VS Code terminal usually use it by default.
- **Topology changes have no effect**: menus 5 and 8 load `data/topology.txt`. Restart after editing it. The first line specifies the number of nodes; subsequent lines use `u v weight`, with `#` comments supported.

## Main Menu

| Number | Function | Course-Guide Stage | Concepts |
|------|------|----------------|--------|
| 1 | Huffman coding | Stage 1 | Frequency counting, priority queues, binary trees and greedy algorithms |
| 2 | Linked-list queue | Stage 1 | Linked lists, queues, dynamic memory and exception handling |
| 3 | CRC checking | Stage 2 | Bit operations (shifts/XOR) and polynomial arithmetic |
| 4 | Hamming error correction | Stage 2 | Binary operations, error detection and correction |
| 5 | Network topology and shortest paths | Stage 3 | Graph theory, Dijkstra, Floyd-Warshall, file I/O and OOP |
| 6 | ASK/FSK modulation and demodulation | Stage 3 | Signal processing, mathematical functions, channel noise, inheritance and polymorphism |
| 7 | Packet merging and dispatch | Comprehensive Project 1 | Queues, dictionaries (maps), random generation and destination-based dispatch |
| 8 | Complete end-to-end chain | **Comprehensive Project 2 (main framework)** | Integration of all the features above |

## Complete End-to-End Chain (Menu 8, Complete Mode, 9 Stages)

```
应用层    文本
   │ 阶段2  霍夫曼信源编码（第一阶段：频率统计→优先队列建树→前缀码）
表示层    压缩比特流 + META 帧（码表序列化 + 有效长度）
   │ 阶段3  组帧：帧头[type/source/dest/seq/长度] + 载荷 + CRC-8（第二阶段）
链路层    META 帧 + 若干 DATA 帧
   │ 阶段4  网络层：真实包与背景噪声包合并入队 → 按目的地分流（综合项目一）
网络层    → Dijkstra 最短路径路由（第三阶段：图 + 文件 I/O 拓扑）
   │ 阶段5  信道编码：汉明码(7,4)，每 4bit → 7bit（第二阶段）
物理层    帧比特流
   │ 阶段6  FSK/ASK 调制 → 信道加噪 + 按 BER 比特翻转（第三阶段/综合项目二）
信道      三档错误：0 无差错 / 1 少量可纠 / 2 高频超限
   │ 阶段7  解调 → 汉明纠错（定位并翻转错误位）→ 拆帧 + CRC 校验兜底
接收端    丢弃 CRC 失败帧与背景噪声包
   │ 阶段8  霍夫曼解码（按 META 帧恢复码表与有效长度）
信宿      恢复文本
   │ 阶段9  一致性验证 + 链路统计表（压缩率/BER/纠错数/CRC 失败数/路由）
```

### Three Error Settings (Corresponding to the Report's Test Cases)

| Setting | Channel Behavior | Expected Result |
|------|----------|----------|
| 0: no errors | BER=0, noise=0 | Complete lossless recovery |
| 1: a few correctable errors | At most 1 erroneous bit per frame | Hamming coding corrects 100% of errors; lossless recovery |
| 2: errors beyond capacity | Frequent errors without a per-frame limit | Some 7bit blocks have double errors → errors remain after Hamming correction → CRC rejects the frames, demonstrating the final integrity check |

## Repository Structure

```
CommChain/
├── CommChain.sln
├── src/CommChain.App/                  # 主程序（控制台）
│   ├── Program.cs                      # 入口 + 主菜单循环
│   ├── MenuDemos.cs                    # 8 个菜单项演示入口
│   ├── Common/BitSequence.cs           # 全链路统一比特容器（MSB first）
│   ├── SourceCoding/HuffmanCoder.cs    # 霍夫曼编码（含码表序列化）
│   ├── Queueing/LinkedListQueue.cs     # 手写链表队列
│   ├── Queueing/PacketDispatcher.cs    # 多信源包生成与分流
│   ├── ErrorControl/CrcCalculator.cs   # CRC-8 + 简易校验和
│   ├── ErrorControl/HammingCodec.cs    # 汉明码(7,4) 编解码纠错
│   ├── PacketLayer/DataPacket.cs       # 帧结构 + FrameCodec 组帧/拆帧
│   ├── Topology/NetworkTopology.cs     # 邻接矩阵 + Dijkstra + Floyd + 文件 I/O
│   ├── Modulation/Modulator.cs         # ASK/FSK 调制器（多态）+ 信道模型
│   ├── EndToEnd/EndToEndSimulator.cs   # 9 阶段链路编排 + 简化模式
│   └── data/topology.txt               # 网络拓扑配置文件
└── tests/CommChain.Tests/              # xUnit 单元测试（6 个测试类、50+ 用例）
```

## Key Design Notes

1. **Unified BitSequence contract**: all modules exchange bit sequences, eliminating ambiguity from mixing string/byte[]/bool[] representations.
2. **Huffman tables travel with the frames**: the table is serialized into the fixed self-describing format `[条目数][字符:16][码长:8][码字]` and sent first in a META frame. Data frames carry only the compressed bitstream, zero-padded to whole bytes; the valid length is included in the META frame.
3. **Standard Hamming (7,4) code**: codewords use `[p1 p2 d1 p3 d2 d3 d4]`; syndrome s3s2s1 directly encodes the error position (0=no error, 1–7=error position). The guide's reference parity/syndrome formulas contain typographical errors, such as s3 always evaluating to 0. This project uses the standard textbook implementation, with unit tests covering all 16 data values and 7 single-error positions.
4. **Deterministic demonstrations**: the random seed is fixed (42). Identical input yields identical output, making report screenshots and test results reproducible.
5. **Separated layers**: sender-side application → presentation → link → network → physical stages are decoupled from the reverse receiver path. Each layer has one responsibility and can be extended, for example with another modulation method or interleaving.

## Tests

`tests/CommChain.Tests` contains 6 test classes: `BitSequenceTests`, `HuffmanCoderTests` (prefix-code properties), `CrcCalculatorTests` (including manually calculated known values), `HammingCodecTests` (all 16 data values × 7 single-error positions), `FrameCodecTests` (framing, deframing and CRC modification detection), `NetworkTopologyTests` (Dijkstra/Floyd cross-checks and invalid-file handling), and `EndToEndTests` (end-to-end assertions for three error settings and reproducibility assertions).

```bash
dotnet test
```
