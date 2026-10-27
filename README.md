# CommChain

English | [简体中文](README_zh.md)

An interactive C# simulator of source coding, error control, routing and digital modulation.

Archived project retained for reference.

## Overview

The console application demonstrates Huffman coding, linked-list queues, CRC-8, Hamming (7,4) codes, shortest-path routing and ASK/FSK modulation. Individual demonstrations and an end-to-end mode expose how text passes through a simulated communication link, with configurable channel errors.

## Technology and Dependencies

C# and .NET 10 SDK. The application uses the .NET base class library. Tests reference xUnit 2.9.3, Microsoft.NET.Test.Sdk 17.14.1, the Visual Studio runner and Coverlet.

## Repository Structure

- `CommChain.sln` — solution
- `src/CommChain.App/` — console application and communication modules
- `src/CommChain.App/data/topology.txt` — weighted network topology
- `tests/CommChain.Tests/` — unit tests

## Quick Start

Install the .NET 10 SDK, then run from the repository root:

```bash
dotnet run --project src/CommChain.App
dotnet test
```

Select menu items 1–7 for individual modules or 8 for the complete chain; enter 0 to exit. In the end-to-end menu, compare simplified/full modes and the three error settings. Edit `src/CommChain.App/data/topology.txt` to change the graph; each edge is `u v weight`.
