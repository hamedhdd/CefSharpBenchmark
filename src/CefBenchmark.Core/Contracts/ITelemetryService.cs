using System;

namespace CefBenchmark.Core.Contracts
{
    public interface ITelemetryService : IDisposable
    {
        event EventHandler<TelemetrySnapshot>? TelemetryUpdated;
        void Start(int intervalMs = 500);
        void Stop();
        TelemetrySnapshot GetCurrentSnapshot();
    }
}
