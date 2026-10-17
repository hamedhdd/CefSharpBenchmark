using System;
using System.Collections.Generic;

namespace CefBenchmark.Core.Contracts
{
    public sealed record SubprocessDetail(int ProcessId, string Name, double CpuPercent, double MemoryMb);

    public sealed class TelemetrySnapshot
    {
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public int SubprocessCount { get; init; }
        public double SubprocessCpuPercent { get; init; }
        public double HostCpuPercent { get; init; }
        public double TotalCpuPercent => Math.Round(SubprocessCpuPercent + HostCpuPercent, 1);
        public double SubprocessMemoryMb { get; init; }
        public double HostMemoryMb { get; init; }
        public double TotalMemoryMb => Math.Round(SubprocessMemoryMb + HostMemoryMb, 1);
        public IReadOnlyList<SubprocessDetail> Subprocesses { get; init; } = Array.Empty<SubprocessDetail>();
    }
}
