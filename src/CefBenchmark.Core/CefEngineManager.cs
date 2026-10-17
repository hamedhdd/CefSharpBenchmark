using System;
using System.IO;
using CefBenchmark.Core.Contracts;
using CefBenchmark.Core.Telemetry;
using CefBenchmark.Core.UI;
using CefSharp;
using CefSharp.WinForms;

namespace CefBenchmark.Core
{
    /// <summary>
    /// Coordinates CEF lifecycle, settings injection, and launches the browser form.
    /// Follows the Single Responsibility Principle (SRP).
    /// </summary>
    public static class CefEngineManager
    {
        public static void Run(IBrowserConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            // Set up CefSettings
            var settings = new CefSettings();

            // Set isolated cache path per mode to prevent file locking conflicts
            string modeSlug = config.IsOptimized ? "optimized" : "default";
            string cachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache", modeSlug);
            Directory.CreateDirectory(cachePath);
            settings.RootCachePath = cachePath;
            settings.CachePath = Path.Combine(cachePath, "web");

            // Apply configuration-specific flags
            config.Apply(settings);

            // Initialize CEF
            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);

            // Instantiate telemetry service
            using var telemetryService = new SystemTelemetryService();

            // Run Form
            using var mainForm = new BrowserMainForm(config, telemetryService);
            System.Windows.Forms.Application.Run(mainForm);

            // Clean shutdown
            if (Cef.IsInitialized == true)
            {
                Cef.Shutdown();
            }
        }
    }
}
