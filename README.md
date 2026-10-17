# CefSharp Performance Benchmark & VM Optimization Suite

A high-performance C# / .NET 8 application suite built with **CefSharp** to demonstrate, benchmark, and solve the 100% CPU lockup and zombie subprocess accumulation issues frequently encountered in Virtual Machines (VMware, Hyper-V, Citrix, Remote Desktop / VDI, Azure, AWS).

The solution provides two fully compiled, ready-to-run executables:
1. **`CefDefaultBrowser.exe` (Default Mode)**: Out-of-the-box unthrottled settings (GPU enabled, falls back to SwiftShader software rasterizer on VMs, unbounded renderer processes, unthrottled background timers, no Job Object).
2. **`CefOptimizedBrowser.exe` (Optimized Mode)**: VM-hardened configuration (disabled GPU fallback, throttled background timers, capped renderer processes, Windows Job Object `KILL_ON_JOB_CLOSE`, and priority lowering).

Both applications include a **Live HUD Telemetry Bar** displaying active CEF subprocess count, CEF CPU %, Host CPU %, and Working Set memory usage in real time.

---

## Architecture Overview

Adheres strictly to the **SOLID** design principles:
- **Single Responsibility Principle (SRP)**: Distinct decoupled modules for CEF engine coordination (`CefEngineManager`), process metrics telemetry (`SystemTelemetryService`), Windows kernel lifecycle confinement (`WindowsJobObject`), and user interface (`BrowserMainForm`).
- **Open/Closed Principle (OCP)**: Configuration strategies implement the `IBrowserConfig` contract, allowing new testing profiles (e.g. headless, kiosk, or ultra-low memory) to be added without modifying core browser infrastructure.
- **Liskov Substitution Principle (LSP)**: Both `DefaultBrowserConfig` and `OptimizedBrowserConfig` seamlessly interchange across the browser engine.
- **Interface Segregation Principle (ISP)**: Lean contracts (`IBrowserConfig`, `ITelemetryService`).
- **Dependency Inversion Principle (DIP)**: `BrowserMainForm` and `CefEngineManager` depend on abstractions, not concrete settings implementations.

---

## Prerequisites

- **OS**: Windows 10 / 11 / Windows Server 2016+ (64-bit)
- **Runtime**: [.NET 8.0 Windows Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Architecture**: `win-x64`
- **Compiler**: .NET SDK 8.0 or 9.0 (already compiled binaries included in `bin/`)

---

## Quick Start & Testing Guide

### 1. Launching via Batch Scripts

In the project root, double-click:
- **`Run_Default_Mode.bat`**: Launches the default unthrottled browser.
- **`Run_Optimized_Mode.bat`**: Launches the VM-optimized browser.
- **`Run_Both_Comparison.bat`**: Launches both applications side-by-side for instantaneous comparison.

### 2. Real-World VM Benchmark Scenarios

Once running, test the following built-in stress suites using the buttons in the header:

1. **⚡ Canvas 2D Stress (2,500 Balls)**:
   - Spawns 2,500 continuous particle physics with collision detection and canvas `shadowBlur`.
   - **Default Mode**: On a VM lacking a discrete GPU, Chromium triggers SwiftShader CPU rasterization (`--type=gpu-process`), saturating the VM at **90% - 100% CPU**.
   - **Optimized Mode**: GPU software rasterization is bypassed, maintaining responsive UI and significantly lower CPU.
2. **⏱ High-Frequency Timers (50 Intervals)**:
   - Runs 50 unconstrained `setInterval(..., 0)` math loops.
   - Click **`👁 Simulate Tab Hidden`**: Watch the live telemetry bar. In optimized mode or with `WasHidden(true)` active, CEF throttles timers to 1 Hz, dropping CPU usage back to near 0%.
3. **🧬 DOM Thrashing (3,000 Elements)**:
   - Rapidly mutates and re-renders 3,000 DOM elements every 50ms, measuring layout thrashing resilience.
4. **🌐 Real Web Browsing**:
   - Type any URL in the address bar (e.g. `youtube.com`, `cnn.com`, `tradingview.com`) or press `DevTools` to inspect live performance profiles.

---

## Configuration Matrix

| Feature / Setting | Default Mode (`CefDefaultBrowser`) | Optimized Mode (`CefOptimizedBrowser`) | Impact on VMs |
| :--- | :--- | :--- | :--- |
| **GPU Hardware Acceleration** | Enabled (default) | Disabled (`disable-gpu`) | Prevents SwiftShader / WARP CPU rasterization lockups |
| **GPU Compositing** | Enabled | Disabled (`disable-gpu-compositing`) | Prevents compositing deadlock on virtual displays |
| **Software Rasterizer** | Enabled fallback | Disabled (`disable-software-rasterizer`) | Eliminates the #1 cause of 100% CPU spikes |
| **DirectX 11 Emulation** | Enabled | Disabled (`disable-d3d11`) | Stops software D3D11 WARP emulation on Hyper-V / VMware |
| **Renderer Subprocess Cap**| Unlimited | Capped to 2 (`renderer-process-limit=2`) | Stops process proliferation across tabs and frames |
| **Process Sharing** | Isolated per origin | `process-per-site` | Reuses existing subprocesses for identical domains |
| **Background Timer Throttling**| Default | Forced (`enable-background-timer-throttling`)| Caps background JavaScript execution to 1 Hz |
| **Audio Threading** | Active | Muted (`mute-audio`) | Shuts down background audio polling loops |
| **Zombie Subprocess Protection**| None | **Windows Job Object (`KILL_ON_JOB_CLOSE`)**| Kernel automatically terminates all child PIDs on exit |
| **Process Priority** | Normal | `BelowNormal` | Ensures host OS and VM critical services never starve |

---

## Building from Source

To recompile both projects from the command line:

```powershell
# Build Core Library
dotnet build src/CefBenchmark.Core/CefBenchmark.Core.csproj -c Release

# Build Default Executable
dotnet build src/CefBenchmark.Default/CefBenchmark.Default.csproj -c Release

# Build Optimized Executable
dotnet build src/CefBenchmark.Optimized/CefBenchmark.Optimized.csproj -c Release
```

Output binaries will be located at:
- `src/CefBenchmark.Default/bin/Release/net8.0-windows/win-x64/CefDefaultBrowser.exe`
- `src/CefBenchmark.Optimized/bin/Release/net8.0-windows/win-x64/CefOptimizedBrowser.exe`

---

## Documentation & Runbooks

For an in-depth architectural guide on CefSharp CPU management in production, see:
- [CefSharp_High_CPU_and_VM_Optimization_Runbook.md](file:///C:/Users/Hamed/Documents/Antigravity_Docs/CefSharp_High_CPU_and_VM_Optimization_Runbook.md)
- [PROJECT_MAP.md](PROJECT_MAP.md)
- [CHANGELOG.md](CHANGELOG.md)
