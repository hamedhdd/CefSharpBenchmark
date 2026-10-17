using System;
using System.Diagnostics;
using System.Windows.Forms;
using CefBenchmark.Core;
using CefBenchmark.Core.Configuration;
using CefBenchmark.Core.Native;

namespace CefBenchmark.Optimized
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // 1. Attach Windows Job Object before spawning any child processes.
            // Guarantees all CefSharp.BrowserSubprocess.exe instances are instantly terminated
            // when this parent application closes or crashes (zero zombie leaks).
            using var jobObject = new WindowsJobObject();
            jobObject.AssignCurrentProcess();

            // Optional: Limit maximum aggregated CPU for this job to 60% of total VM capacity
            // jobObject.SetCpuRateCap(60);

            // 2. Set priority to BelowNormal so that any browser spikes do not lock up the OS or VM
            try
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
            }
            catch { }

            // 3. Launch CEF with VM-hardened configuration
            var config = new OptimizedBrowserConfig();
            CefEngineManager.Run(config);
        }
    }
}