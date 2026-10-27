# CommChain

[English](README.md) | 简体中文

交互式 C# 数字通信模拟器，覆盖信源编码、差错控制、路由与数字调制。

归档项目，保留用于参考。

## 项目概述

控制台程序演示霍夫曼编码、链表队列、CRC-8、汉明码 (7,4)、最短路径及 ASK/FSK 调制，支持独立模块与端到端链路演示，可调整模拟信道差错条件。

## 技术栈与依赖

C#、.NET 10 SDK。主程序使用 .NET 基础类库；测试依赖 xUnit 2.9.3、Microsoft.NET.Test.Sdk 17.14.1、Visual Studio 测试运行器及 Coverlet。

## 文件结构

- `CommChain.sln` — 解决方案
- `src/CommChain.App/` — 控制台与通信模块
- `src/CommChain.App/data/topology.txt` — 带权网络拓扑
- `tests/CommChain.Tests/` — 单元测试

## 快速上手

安装 .NET 10 SDK，在仓库根目录执行：

```bash
dotnet run --project src/CommChain.App
dotnet test
```

菜单 1–7 演示单个模块，8 演示完整链路，0 退出。完整链路可比较简化和完整模式，以及三档差错条件。拓扑配置位于 `src/CommChain.App/data/topology.txt`，边的格式为 `u v weight`。
