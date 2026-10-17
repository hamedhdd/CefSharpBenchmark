using System;
using System.Windows.Forms;
using CefBenchmark.Core;
using CefBenchmark.Core.Configuration;

namespace CefBenchmark.Default
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Run with standard unthrottled CefSharp settings
            var config = new DefaultBrowserConfig();
            CefEngineManager.Run(config);
        }
    }
}