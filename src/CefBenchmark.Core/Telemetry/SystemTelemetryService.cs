using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using CefBenchmark.Core.Contracts;

namespace CefBenchmark.Core.Telemetry
{
    /// <summary>
    /// Thread-safe real-time telemetry service that measures CPU and memory consumption
    /// for both the host application and all child CefSharp.BrowserSubprocess instances.
    /// Follows the Single Responsibility Principle (SRP).
    /// </summary>
    public sealed class SystemTelemetryService : ITelemetryService
    {
        private readonly int _processorCount = Math.Max(1, Environment.ProcessorCount);
        private readonly Dictionary<int, (TimeSpan CpuTime, DateTime SampleTime)> _processCpuHistory = new();
        private System.Threading.Timer? _timer;
        private readonly object _syncLock = new();
        private TelemetrySnapshot _latestSnapshot = new();
        private bool _disposed;

        public event EventHandler<TelemetrySnapshot>? TelemetryUpdated;

        public void Start(int intervalMs = 500)
        {
            lock (_syncLock)
            {
                _timer?.Dispose();
                _timer = new System.Threading.Timer(OnTimerTick, null, 0, intervalMs);
            }
        }

        public void Stop()
        {
            lock (_syncLock)
            {
                _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        public TelemetrySnapshot GetCurrentSnapshot()
        {
            lock (_syncLock)
            {
                return _latestSnapshot;
            }
        }

        private void OnTimerTick(object? state)
        {
            if (_disposed) return;

            try
            {
                var snapshot = CollectSnapshot();
                lock (_syncLock)
                {
                    _latestSnapshot = snapshot;
                }
                TelemetryUpdated?.Invoke(this, snapshot);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Telemetry collection error: {ex.Message}");
            }
        }

        private TelemetrySnapshot CollectSnapshot()
        {
            DateTime now = DateTime.UtcNow;

            // 1. Host Process Metrics
            using var hostProcess = Process.GetCurrentProcess();
            double hostCpu = CalculateCpuPercent(hostProcess.Id, hostProcess.TotalProcessorTime, now);
            double hostMemoryMb = Math.Round(hostProcess.WorkingSet64 / (1024.0 * 1024.0), 1);

            // 2. CefSharp Subprocesses Metrics
            Process[] subProcesses = Process.GetProcessesByName("CefSharp.BrowserSubprocess");
            double totalSubCpu = 0;
            double totalSubMem = 0;
            var details = new List<SubprocessDetail>(subProcesses.Length);

            var activePids = new HashSet<int> { hostProcess.Id };

            foreach (var sub in subProcesses)
            {
                try
                {
                    activePids.Add(sub.Id);
                    double cpu = CalculateCpuPercent(sub.Id, sub.TotalProcessorTime, now);
                    double mem = Math.Round(sub.WorkingSet64 / (1024.0 * 1024.0), 1);

                    totalSubCpu += cpu;
                    totalSubMem += mem;
                    details.Add(new SubprocessDetail(sub.Id, sub.ProcessName, cpu, mem));
                }
                catch
                {
                    // Process may have exited between GetProcessesByName and query
                }
                finally
                {
                    sub.Dispose();
                }
            }

            // Prune dead processes from CPU tracking dictionary
            var deadPids = new List<int>();
            foreach (var pid in _processCpuHistory.Keys)
            {
                if (!activePids.Contains(pid))
                {
                    deadPids.Add(pid);
                }
            }
            foreach (var pid in deadPids)
            {
                _processCpuHistory.Remove(pid);
            }

            return new TelemetrySnapshot
            {
                Timestamp = now,
                SubprocessCount = details.Count,
                HostCpuPercent = Math.Round(hostCpu, 1),
                HostMemoryMb = hostMemoryMb,
                SubprocessCpuPercent = Math.Round(totalSubCpu, 1),
                SubprocessMemoryMb = Math.Round(totalSubMem, 1),
                Subprocesses = details
            };
        }

        private double CalculateCpuPercent(int pid, TimeSpan totalProcessorTime, DateTime now)
        {
            if (_processCpuHistory.TryGetValue(pid, out var last))
            {
                double elapsedRealMs = (now - last.SampleTime).TotalMilliseconds;
                double elapsedCpuMs = (totalProcessorTime - last.CpuTime).TotalMilliseconds;

                _processCpuHistory[pid] = (totalProcessorTime, now);

                if (elapsedRealMs <= 0 || elapsedCpuMs < 0) return 0.0;

                // CPU % across all cores
                double cpu = (elapsedCpuMs / (elapsedRealMs * _processorCount)) * 100.0;
                return Math.Clamp(cpu, 0.0, 100.0);
            }

            _processCpuHistory[pid] = (totalProcessorTime, now);
            return 0.0;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _timer?.Dispose();
                _timer = null;
            }
        }
    }
}
