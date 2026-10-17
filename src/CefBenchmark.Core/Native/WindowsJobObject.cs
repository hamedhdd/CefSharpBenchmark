using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CefBenchmark.Core.Native
{
    /// <summary>
    /// Manages a Windows Kernel Job Object to strictly bind child process lifecycles
    /// and optionally enforce CPU rate limits.
    /// Follows the Single Responsibility Principle (SRP).
    /// </summary>
    public sealed class WindowsJobObject : IDisposable
    {
        private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;
        private const uint JOB_OBJECT_CPU_RATE_CONTROL_ENABLE = 0x1;
        private const uint JOB_OBJECT_CPU_RATE_CONTROL_HARD_CAP = 0x4;

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryLimit;
            public UIntPtr PeakJobMemoryLimit;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_CPU_RATE_CONTROL_INFORMATION
        {
            public uint ControlFlags;
            public uint CpuRate;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetInformationJobObject(
            IntPtr hJob,
            int JobObjectInfoClass,
            IntPtr lpJobObjectInfo,
            uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private IntPtr _handle;
        private bool _disposed;

        public IntPtr Handle => _handle;

        public WindowsJobObject(string? jobName = null)
        {
            _handle = CreateJobObject(IntPtr.Zero, jobName);
            if (_handle == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to create Job Object. Win32 Error: {Marshal.GetLastWin32Error()}");
            }

            ConfigureKillOnClose();
        }

        /// <summary>
        /// Configures the job object to instantly kill all member processes if the job handle closes (e.g. parent exits or crashes).
        /// </summary>
        private void ConfigureKillOnClose()
        {
            var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
            {
                BasicLimitInformation = { LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE }
            };

            int length = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
            IntPtr ptr = Marshal.AllocHGlobal(length);
            try
            {
                Marshal.StructureToPtr(info, ptr, false);
                // 9 = JobObjectExtendedLimitInformation
                if (!SetInformationJobObject(_handle, 9, ptr, (uint)length))
                {
                    Debug.WriteLine($"Failed to set JobObjectExtendedLimitInformation: {Marshal.GetLastWin32Error()}");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        /// <summary>
        /// Enforces a hard CPU cap on the entire job across all member processes.
        /// </summary>
        /// <param name="maxCpuPercentage">Percentage between 1 and 100 (e.g. 50 = max 50% CPU total).</param>
        public bool SetCpuRateCap(uint maxCpuPercentage)
        {
            if (maxCpuPercentage < 1 || maxCpuPercentage > 100)
                throw new ArgumentOutOfRangeException(nameof(maxCpuPercentage), "Must be between 1 and 100.");

            var cpuInfo = new JOBOBJECT_CPU_RATE_CONTROL_INFORMATION
            {
                ControlFlags = JOB_OBJECT_CPU_RATE_CONTROL_ENABLE | JOB_OBJECT_CPU_RATE_CONTROL_HARD_CAP,
                CpuRate = maxCpuPercentage * 100 // Scale is 10000 = 100%
            };

            int length = Marshal.SizeOf(typeof(JOBOBJECT_CPU_RATE_CONTROL_INFORMATION));
            IntPtr ptr = Marshal.AllocHGlobal(length);
            try
            {
                Marshal.StructureToPtr(cpuInfo, ptr, false);
                // 15 = JobObjectCpuRateControlInformation
                return SetInformationJobObject(_handle, 15, ptr, (uint)length);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        /// <summary>
        /// Assigns a process to the job object.
        /// </summary>
        public bool AssignProcess(Process process)
        {
            if (process == null || process.HasExited) return false;
            return AssignProcessToJobObject(_handle, process.Handle);
        }

        /// <summary>
        /// Assigns the current host process to the job object. Any child processes created subsequently will inherit this job.
        /// </summary>
        public bool AssignCurrentProcess()
        {
            return AssignProcess(Process.GetCurrentProcess());
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_handle != IntPtr.Zero)
                {
                    CloseHandle(_handle);
                    _handle = IntPtr.Zero;
                }
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        ~WindowsJobObject()
        {
            Dispose();
        }
    }
}
