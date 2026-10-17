using CefSharp;
using CefSharp.WinForms;

namespace CefBenchmark.Core.Contracts
{
    /// <summary>
    /// Contract defining a configuration provider for Chromium Embedded Framework settings.
    /// Follows the Open/Closed Principle (OCP) and Dependency Inversion Principle (DIP).
    /// </summary>
    public interface IBrowserConfig
    {
        /// <summary>
        /// Display title for the benchmark mode (e.g. "Default Mode" vs "Optimized Mode").
        /// </summary>
        string ModeName { get; }

        /// <summary>
        /// Human-readable explanation of flags and parameters applied.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// True if VM optimization flags and job isolation are activated.
        /// </summary>
        bool IsOptimized { get; }

        /// <summary>
        /// Applies specific command-line arguments, frame rates, and options to the CefSettings instance.
        /// </summary>
        void Apply(CefSettings settings);
    }
}
