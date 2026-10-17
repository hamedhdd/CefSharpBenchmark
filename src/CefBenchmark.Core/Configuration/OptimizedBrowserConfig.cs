using CefBenchmark.Core.Contracts;
using CefSharp;
using CefSharp.WinForms;

namespace CefBenchmark.Core.Configuration
{
    /// <summary>
    /// Highly optimized CefSharp configuration engineered specifically for VMs, VDI, RDP, and cloud instances.
    /// Disables GPU fallback loops, throttles background timers, caps renderer processes, and mutes audio pipelines.
    /// </summary>
    public sealed class OptimizedBrowserConfig : IBrowserConfig
    {
        public string ModeName => "Optimized Mode (VM Safe)";
        public string Description => "VM Hardened: GPU & Software rasterizers disabled, background timers throttled to 1Hz, renderer processes capped, audio muted, and OSR frame rate capped.";
        public bool IsOptimized => true;

        public void Apply(CefSettings settings)
        {
            // 1. Disable GPU hardware acceleration and software rasterization fallback
            settings.CefCommandLineArgs.Add("disable-gpu", "1");
            settings.CefCommandLineArgs.Add("disable-gpu-compositing", "1");
            settings.CefCommandLineArgs.Add("disable-software-rasterizer", "1");
            settings.CefCommandLineArgs.Add("disable-d3d11", "1");
            settings.CefCommandLineArgs.Add("disable-accelerated-2d-canvas", "1");
            settings.CefCommandLineArgs.Add("disable-gpu-vsync", "1");

            // 2. Cap subprocess count to prevent memory and thread explosion
            settings.CefCommandLineArgs.Add("renderer-process-limit", "2");
            settings.CefCommandLineArgs.Add("process-per-site", "1");

            // 3. Force background timer throttling (drops background JS timers to 1Hz)
            settings.CefCommandLineArgs.Add("enable-background-timer-throttling", "1");

            // 4. Mute background audio/speech threads
            settings.CefCommandLineArgs.Add("mute-audio", "1");
            settings.CefCommandLineArgs.Add("disable-speech-api", "1");

            // 5. Cap windowless/OSR frame rate
            settings.CefCommandLineArgs.Add("off-screen-rendering-frame-rate", "15");

            // 6. Logging
            settings.LogSeverity = LogSeverity.Disable;
        }
    }
}
