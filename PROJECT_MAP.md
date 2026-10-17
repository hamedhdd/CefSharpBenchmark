# CefSharp Performance Benchmark — Living Codebase Map (`PROJECT_MAP.md`)

## 1. Directory Tree & Component Index

```
CefSharpBenchmark/
├── .gitignore                          # Excludes bin/, obj/, native CEF binaries, and caches
├── CefSharpBenchmark.sln               # Master Visual Studio / dotnet Solution
├── Run_Default_Mode.bat                # One-click launcher for unthrottled CefDefaultBrowser
├── Run_Optimized_Mode.bat              # One-click launcher for VM-hardened CefOptimizedBrowser
├── Run_Both_Comparison.bat             # Simultaneous side-by-side launcher for benchmark comparison
├── README.md                           # Master project documentation, architecture, and runbook
├── CHANGELOG.md                        # Version history adhering to Keep a Changelog
├── PROJECT_MAP.md                      # Living codebase architecture map (this document)
└── src/
    ├── CefBenchmark.Core/              # Shared business logic, telemetry, native interop, and UI
    │   ├── CefBenchmark.Core.csproj    # Class library targeting net8.0-windows (win-x64)
    │   ├── CefEngineManager.cs         # Coordinates CEF lifecycle, cache isolation, and app execution (SRP)
    │   ├── Contracts/
    │   │   ├── IBrowserConfig.cs       # Strategy contract for CEF settings injection (OCP / DIP)
    │   │   ├── ITelemetryService.cs    # Contract for real-time process CPU & memory sampling (ISP)
    │   │   └── TelemetrySnapshot.cs    # Immutable telemetry data transfer records
    │   ├── Configuration/
    │   │   ├── DefaultBrowserConfig.cs # Standard unthrottled Chromium configuration
    │   │   └── OptimizedBrowserConfig.cs# VM-hardened configuration (disables GPU, throttles timers)
    │   ├── Native/
    │   │   └── WindowsJobObject.cs     # Windows Kernel Job Object wrapper (KILL_ON_JOB_CLOSE, CPU cap)
    │   ├── Telemetry/
    │   │   └── SystemTelemetryService.cs# High-frequency delta CPU & working set monitor (SRP)
    │   ├── Benchmarks/
    │   │   └── StressPages.cs          # Embedded offline HTML/JS benchmark suites (Canvas, Timer, DOM)
    │   └── UI/
    │       └── BrowserMainForm.cs      # Unified WinForms browser view with live HUD and controls
    ├── CefBenchmark.Default/           # Standalone executable for default unthrottled testing
    │   ├── CefBenchmark.Default.csproj # Produces CefDefaultBrowser.exe
    │   └── Program.cs                  # Entry point using DefaultBrowserConfig without Job Object
    └── CefBenchmark.Optimized/         # Standalone executable for VM-hardened testing
        ├── CefBenchmark.Optimized.csproj# Produces CefOptimizedBrowser.exe
        └── Program.cs                  # Entry point attaching WindowsJobObject & BelowNormal priority
```

---

## 2. Data & Telemetry Flow

```mermaid
flowchart TD
    subgraph HostApplication [Parent .NET Host Process]
        Entry[Program.Main] --> Job[WindowsJobObject.AssignCurrentProcess]
        Job --> Engine[CefEngineManager.Run]
        Engine --> Config[IBrowserConfig.Apply]
        Config --> CefInit[Cef.Initialize]
        CefInit --> Form[BrowserMainForm]
        Form --> Telemetry[SystemTelemetryService]
    end

    subgraph KernelAndSubprocesses [Windows OS & CEF Subprocesses]
        CefInit -. Spawns .-> SubGpu["CefSharp.BrowserSubprocess (--type=gpu-process)"]
        CefInit -. Spawns .-> SubRenderer["CefSharp.BrowserSubprocess (--type=renderer)"]
        CefInit -. Spawns .-> SubUtility["CefSharp.BrowserSubprocess (--type=utility)"]
        Job -- "Enforces Kill-On-Close & CPU Cap" --- SubGpu
        Job -- "Enforces Kill-On-Close & CPU Cap" --- SubRenderer
        Job -- "Enforces Kill-On-Close & CPU Cap" --- SubUtility
    end

    subgraph TelemetryLoop [Live Telemetry Pipeline (500ms)]
        Telemetry -->|Poll Process.TotalProcessorTime| SubRenderer
        Telemetry -->|Poll Process.TotalProcessorTime| SubGpu
        Telemetry -->|Compute CPU Delta / Core Count| Snapshot[TelemetrySnapshot]
        Snapshot -->|Marshal via BeginInvoke| Form
        Form -->|Update Metrics HUD| UI_HUD[Header Status Bar]
    end
```

---

## 3. UI Action & Command Catalog

| Action / Control | Target / Handler | Description |
| :--- | :--- | :--- |
| **Address Bar (`txtAddress`)** | `NavigateToAddress()` | Navigates to custom HTTP/HTTPS URLs with auto-prefixing. |
| **Back / Forward / Reload** | `_browser.Back()`, `_browser.Forward()`, `_browser.Reload()` | Standard web browser navigation primitives. |
| **DevTools** | `_browser.ShowDevTools()` | Opens Chromium Developer Tools to inspect DOM, network, and performance. |
| **Canvas 2D Stress** | `StressPages.GetCanvasBallStressHtml()` | Spawns 2,500 particles with shadows and continuous redraws to stress software rasterizer. |
| **High-Freq Timer Stress** | `StressPages.GetTimerStressHtml()` | Runs 50 unthrottled `setInterval(..., 0)` timers executing math loops. |
| **DOM Thrashing Stress** | `StressPages.GetDomStressHtml()` | Dynamically mutates 3,000 DOM elements every 50ms. |
| **Simulate Tab Hidden** | `ToggleSimulateHidden()` | Toggles `host.WasHidden(true)` and drops frame rate to 1 FPS, demonstrating instant CPU drop. |

---

## 4. Storage & Process Architecture Map

- **Cache Partitions**:
  - `cache/default/`: Isolated Chromium profile cache for Default Mode.
  - `cache/optimized/`: Isolated Chromium profile cache for Optimized Mode.
  - *Prevents disk and database locking conflicts when running both modes simultaneously.*
- **Process Hierarchy**:
  - `CefDefaultBrowser.exe` (PID A) -> Spawns unmanaged `CefSharp.BrowserSubprocess.exe` (PIDs B, C, D).
  - `CefOptimizedBrowser.exe` (PID X) -> Contained within `WindowsJobObject` -> Spawns throttled `CefSharp.BrowserSubprocess.exe` (PIDs Y, Z).

---

## 5. Frontend / View Layout

```
+---------------------------------------------------------------------------------------------------------+
| [MODE BADGE]  [◀] [▶] [🔄]  [ Address Input: https://...                         ]  [Go]  [DevTools]    |
| Benchmarks: [⚡ Canvas 2D (2,500 Balls)] [⏱ High-Freq Timers] [🧬 DOM Thrashing] [👁 Simulate Tab Hidden]|
| Telemetry:  CEF Subprocesses: 2 | CEF CPU: 4.2% | Host CPU: 0.8% | Total RAM: 85.2 MB | CPU Nominal     |
+---------------------------------------------------------------------------------------------------------+
|                                                                                                         |
|                                  ChromiumWebBrowser Viewport                                            |
|                               (Active Web Page / Benchmark Canvas)                                      |
|                                                                                                         |
+---------------------------------------------------------------------------------------------------------+
```
