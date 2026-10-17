# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-10-17

### Added
- **Core Architecture (`CefBenchmark.Core`)**:
  - `IBrowserConfig` interface adhering to Strategy and Open/Closed Principles (OCP).
  - `DefaultBrowserConfig` providing standard out-of-the-box unthrottled CEF configuration.
  - `OptimizedBrowserConfig` providing VM/VDI hardened configuration (`disable-gpu`, `disable-software-rasterizer`, `disable-d3d11`, `renderer-process-limit=2`, `enable-background-timer-throttling`, `mute-audio`, and OSR rate cap).
  - `WindowsJobObject` native wrapper enforcing `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE` to completely eradicate zombie subprocess leaks on VM host shutdowns or crashes.
  - `SystemTelemetryService` high-frequency, non-admin delta CPU and memory monitor for host and child CEF subprocesses.
  - Built-in offline stress testing suites: Canvas 2D physics with 2,500 particles and shadow blur, 50 high-frequency timers, and 3,000 element DOM layout thrashing.
  - `BrowserMainForm` WinForms browser view with real-time HUD telemetry, navigation controls, DevTools, and a "Simulate Tab Hidden" visibility toggle.
- **Default Application (`CefBenchmark.Default`)**:
  - Compiles to `CefDefaultBrowser.exe` for baseline CPU and memory measurement.
- **Optimized Application (`CefBenchmark.Optimized`)**:
  - Compiles to `CefOptimizedBrowser.exe` with Job Object confinement and `BelowNormal` process priority.
- **Automation Scripts**:
  - `Run_Default_Mode.bat`, `Run_Optimized_Mode.bat`, and `Run_Both_Comparison.bat` for one-click launching and side-by-side benchmarking.
- **Documentation**:
  - `PROJECT_MAP.md` living codebase and telemetry architecture map.
  - Persistent operational runbook stored at `C:\Users\Hamed\Documents\Antigravity_Docs\CefSharp_High_CPU_and_VM_Optimization_Runbook.md`.
