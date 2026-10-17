using CefBenchmark.Core.Contracts;
using CefSharp;
using CefSharp.WinForms;

namespace CefBenchmark.Core.Configuration
{
    /// <summary>
    /// Default CefSharp configuration.
    /// Represents standard out-of-the-box settings without VM/VDI optimizations.
    /// In a VM environment lacking discrete GPU hardware, this triggers CPU-based software
    /// rasterization (SwiftShader / D3D11 WARP), unbounded renderer processes, and full-speed background timers.
    /// </summary>
    public sealed class DefaultBrowserConfig : IBrowserConfig
    {
        public string ModeName => "Default Mode (Unthrottled)";
        public string Description => "Standard Chromium configuration: GPU acceleration active (falls back to SwiftShader on VMs), unconstrained renderer processes, full-speed background timers.";
        public bool IsOptimized => false;

        public void Apply(CefSettings settings)
        {
            // Standard CefSharp behavior: default settings without custom VM overrides.
            // Hardware acceleration remains enabled by default.
            settings.LogSeverity = LogSeverity.Disable;
        }
    }
}
