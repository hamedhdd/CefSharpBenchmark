using System;
using System.Drawing;
using System.Windows.Forms;
using CefBenchmark.Core.Benchmarks;
using CefBenchmark.Core.Contracts;
using CefSharp;
using CefSharp.WinForms;

namespace CefBenchmark.Core.UI
{
    public sealed class BrowserMainForm : Form
    {
        private readonly IBrowserConfig _config;
        private readonly ITelemetryService _telemetryService;
        private ChromiumWebBrowser? _browser;
        private bool _isCurrentlySimulatingHidden = false;

        // UI Controls
        private Panel _pnlHeader = null!;
        private FlowLayoutPanel _pnlToolbar = null!;
        private FlowLayoutPanel _pnlBenchmarks = null!;
        private Panel _pnlTelemetry = null!;
        private Panel _pnlBrowser = null!;

        private TextBox _txtAddress = null!;
        private Button _btnGo = null!;
        private Button _btnBack = null!;
        private Button _btnForward = null!;
        private Button _btnReload = null!;
        private Button _btnDevTools = null!;
        private Button _btnToggleHidden = null!;

        private Label _lblModeBadge = null!;
        private Label _lblSubprocessCount = null!;
        private Label _lblSubCpu = null!;
        private Label _lblHostCpu = null!;
        private Label _lblTotalMem = null!;
        private Label _lblStatusAlert = null!;

        public BrowserMainForm(IBrowserConfig config, ITelemetryService telemetryService)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));

            InitializeComponents();
            InitializeBrowser();
            StartTelemetry();
        }

        private void InitializeComponents()
        {
            Text = $"CefSharp Benchmark — {_config.ModeName}";
            Size = new Size(1280, 850);
            MinimumSize = new Size(950, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(15, 23, 42); // Dark slate
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // Container Panel for Header
            _pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 118,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(10, 8, 10, 6)
            };

            // Toolbar Row 1: Mode Badge + Navigation
            _pnlToolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 38,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            _lblModeBadge = new Label
            {
                Text = _config.IsOptimized ? "  OPTIMIZED (VM HARDENED)  " : "  DEFAULT (UNTHROTTLED)  ",
                BackColor = _config.IsOptimized ? Color.FromArgb(16, 185, 129) : Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Height = 30,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 2, 10, 0)
            };

            _btnBack = CreateButton("◀", 35, (s, e) => { if (_browser?.CanGoBack == true) _browser.Back(); });
            _btnForward = CreateButton("▶", 35, (s, e) => { if (_browser?.CanGoForward == true) _browser.Forward(); });
            _btnReload = CreateButton("🔄", 35, (s, e) => _browser?.Reload());

            _txtAddress = new TextBox
            {
                Width = 550,
                Height = 30,
                Font = new Font("Segoe UI", 10.5f),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(4, 2, 4, 0)
            };
            _txtAddress.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    NavigateToAddress();
                }
            };

            _btnGo = CreateButton("Go", 50, (s, e) => NavigateToAddress());
            _btnDevTools = CreateButton("DevTools", 75, (s, e) => _browser?.ShowDevTools());

            _pnlToolbar.Controls.Add(_lblModeBadge);
            _pnlToolbar.Controls.Add(_btnBack);
            _pnlToolbar.Controls.Add(_btnForward);
            _pnlToolbar.Controls.Add(_btnReload);
            _pnlToolbar.Controls.Add(_txtAddress);
            _pnlToolbar.Controls.Add(_btnGo);
            _pnlToolbar.Controls.Add(_btnDevTools);

            // Toolbar Row 2: Benchmark Action Presets
            _pnlBenchmarks = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 36,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 4, 0, 0)
            };

            var lblBenchTitle = new Label
            {
                Text = "Benchmarks:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 2, 6, 0)
            };

            var btnCanvas = CreateButton("⚡ Canvas 2D (2,500 Balls)", 190, (s, e) =>
            {
                _browser?.LoadHtml(StressPages.GetCanvasBallStressHtml(), "http://benchmark.local/canvas");
                _txtAddress.Text = "benchmark://canvas-balls";
            });

            var btnTimer = CreateButton("⏱ High-Freq Timers (50)", 175, (s, e) =>
            {
                _browser?.LoadHtml(StressPages.GetTimerStressHtml(), "http://benchmark.local/timer");
                _txtAddress.Text = "benchmark://timer-stress";
            });

            var btnDom = CreateButton("🧬 DOM Thrashing (3k)", 165, (s, e) =>
            {
                _browser?.LoadHtml(StressPages.GetDomStressHtml(), "http://benchmark.local/dom");
                _txtAddress.Text = "benchmark://dom-thrash";
            });

            var btnOverview = CreateButton("📋 Overview", 95, (s, e) => LoadOverviewPage());

            _btnToggleHidden = CreateButton("👁 Simulate Tab Hidden", 175, (s, e) => ToggleSimulateHidden());
            _btnToggleHidden.BackColor = Color.FromArgb(51, 65, 85);

            _pnlBenchmarks.Controls.Add(lblBenchTitle);
            _pnlBenchmarks.Controls.Add(btnCanvas);
            _pnlBenchmarks.Controls.Add(btnTimer);
            _pnlBenchmarks.Controls.Add(btnDom);
            _pnlBenchmarks.Controls.Add(btnOverview);
            _pnlBenchmarks.Controls.Add(_btnToggleHidden);

            // Telemetry Bar
            _pnlTelemetry = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(10, 4, 10, 4)
            };

            _lblSubprocessCount = CreateMetricLabel("Subprocesses: 0", 150);
            _lblSubCpu = CreateMetricLabel("CEF CPU: 0.0%", 140);
            _lblHostCpu = CreateMetricLabel("Host CPU: 0.0%", 140);
            _lblTotalMem = CreateMetricLabel("Memory: 0.0 MB", 160);
            _lblStatusAlert = new Label
            {
                Text = "Telemetry Active",
                ForeColor = Color.FromArgb(56, 189, 248),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Dock = DockStyle.Right,
                Width = 240,
                TextAlign = ContentAlignment.MiddleRight
            };

            var telemetryFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                Width = 750,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            telemetryFlow.Controls.Add(_lblSubprocessCount);
            telemetryFlow.Controls.Add(_lblSubCpu);
            telemetryFlow.Controls.Add(_lblHostCpu);
            telemetryFlow.Controls.Add(_lblTotalMem);

            _pnlTelemetry.Controls.Add(telemetryFlow);
            _pnlTelemetry.Controls.Add(_lblStatusAlert);

            _pnlHeader.Controls.Add(_pnlBenchmarks);
            _pnlHeader.Controls.Add(_pnlToolbar);
            _pnlHeader.Controls.Add(_pnlTelemetry);

            // Browser Hosting Area
            _pnlBrowser = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black
            };

            Controls.Add(_pnlBrowser);
            Controls.Add(_pnlHeader);
        }

        private void InitializeBrowser()
        {
            _browser = new ChromiumWebBrowser("about:blank")
            {
                Dock = DockStyle.Fill
            };

            _browser.AddressChanged += (s, e) =>
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => _txtAddress.Text = e.Address));
                }
                else
                {
                    _txtAddress.Text = e.Address;
                }
            };

            _browser.TitleChanged += (s, e) =>
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => Text = $"{e.Title} — {_config.ModeName}"));
                }
                else
                {
                    Text = $"{e.Title} — {_config.ModeName}";
                }
            };

            _browser.IsBrowserInitializedChanged += (s, e) =>
            {
                if (_browser.IsBrowserInitialized)
                {
                    BeginInvoke(new Action(LoadOverviewPage));
                }
            };

            _pnlBrowser.Controls.Add(_browser);
        }

        private void LoadOverviewPage()
        {
            string html = StressPages.GetOverviewHtml(_config.ModeName, _config.Description, _config.IsOptimized);
            _browser?.LoadHtml(html, "http://benchmark.local/overview");
            _txtAddress.Text = "benchmark://overview";
        }

        private void NavigateToAddress()
        {
            string url = _txtAddress.Text.Trim();
            if (string.IsNullOrWhiteSpace(url)) return;

            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }

            _browser?.Load(url);
        }

        private void ToggleSimulateHidden()
        {
            var host = _browser?.GetBrowserHost();
            if (host == null) return;

            _isCurrentlySimulatingHidden = !_isCurrentlySimulatingHidden;

            if (_isCurrentlySimulatingHidden)
            {
                // Simulate hiding view or switching away in VM
                host.WasHidden(true);
                host.WindowlessFrameRate = 1;
                _btnToggleHidden.Text = "👁 Restore Tab (WasHidden=false)";
                _btnToggleHidden.BackColor = Color.FromArgb(220, 38, 38);
                _lblStatusAlert.Text = "Tab Hidden / WasHidden Active";
                _lblStatusAlert.ForeColor = Color.FromArgb(239, 68, 68);
            }
            else
            {
                host.WasHidden(false);
                host.WindowlessFrameRate = _config.IsOptimized ? 15 : 60;
                _btnToggleHidden.Text = "👁 Simulate Tab Hidden";
                _btnToggleHidden.BackColor = Color.FromArgb(51, 65, 85);
                _lblStatusAlert.Text = "Tab Active";
                _lblStatusAlert.ForeColor = Color.FromArgb(16, 185, 129);
            }
        }

        private void StartTelemetry()
        {
            _telemetryService.TelemetryUpdated += (s, snapshot) =>
            {
                if (IsDisposed || Disposing) return;

                try
                {
                    BeginInvoke(new Action(() => UpdateTelemetryUI(snapshot)));
                }
                catch { }
            };
            _telemetryService.Start(intervalMs: 500);
        }

        private void UpdateTelemetryUI(TelemetrySnapshot s)
        {
            _lblSubprocessCount.Text = $"CEF Subprocesses: {s.SubprocessCount}";
            _lblSubCpu.Text = $"CEF CPU: {s.SubprocessCpuPercent:0.0}%";
            _lblHostCpu.Text = $"Host CPU: {s.HostCpuPercent:0.0}%";
            _lblTotalMem.Text = $"Total RAM: {s.TotalMemoryMb:0.0} MB";

            // Colorize CPU alerts
            if (s.SubprocessCpuPercent > 70.0)
            {
                _lblSubCpu.ForeColor = Color.FromArgb(239, 68, 68); // Red
                _lblStatusAlert.Text = "⚠️ Heavy CPU Load Detected";
                _lblStatusAlert.ForeColor = Color.FromArgb(239, 68, 68);
            }
            else if (s.SubprocessCpuPercent > 30.0)
            {
                _lblSubCpu.ForeColor = Color.FromArgb(245, 158, 11); // Amber
                _lblStatusAlert.Text = "Moderate Load";
                _lblStatusAlert.ForeColor = Color.FromArgb(245, 158, 11);
            }
            else
            {
                _lblSubCpu.ForeColor = Color.FromArgb(52, 211, 153); // Emerald
                if (!_isCurrentlySimulatingHidden)
                {
                    _lblStatusAlert.Text = "CPU Nominal / Responsive";
                    _lblStatusAlert.ForeColor = Color.FromArgb(52, 211, 153);
                }
            }
        }

        private Button CreateButton(string text, int width, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Width = width,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                Margin = new Padding(2, 2, 4, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += onClick;
            return btn;
        }

        private Label CreateMetricLabel(string text, int width)
        {
            return new Label
            {
                Text = text,
                Width = width,
                Height = 24,
                ForeColor = Color.FromArgb(203, 213, 225),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 10, 0)
            };
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            _telemetryService.Stop();
            _telemetryService.Dispose();

            if (_browser != null)
            {
                _browser.Stop();
                _browser.Dispose();
                _browser = null;
            }
        }
    }
}
