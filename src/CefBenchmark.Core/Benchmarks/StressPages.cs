namespace CefBenchmark.Core.Benchmarks
{
    /// <summary>
    /// Self-contained HTML/JS benchmark suites designed to simulate intense graphics,
    /// background timers, and DOM thrashing. Runs completely offline.
    /// </summary>
    public static class StressPages
    {
        public static string GetCanvasBallStressHtml()
        {
            return @"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Canvas 2D Stress Test (2,500 Balls)</title>
    <style>
        body { margin: 0; padding: 0; background: #0f172a; color: #f8fafc; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; overflow: hidden; }
        #hud { position: absolute; top: 15px; left: 15px; background: rgba(15, 23, 42, 0.85); padding: 12px 20px; border-radius: 8px; border: 1px solid #334155; font-size: 14px; backdrop-filter: blur(4px); box-shadow: 0 4px 6px -1px rgba(0,0,0,0.5); }
        .stat-value { font-weight: bold; color: #38bdf8; }
        canvas { display: block; }
    </style>
</head>
<body>
    <div id='hud'>
        <div><strong>Benchmark:</strong> 2,500 Particle Physics & ShadowBlur Canvas</div>
        <div><strong>FPS:</strong> <span id='fps' class='stat-value'>0</span></div>
        <div><strong>Particle Count:</strong> <span class='stat-value'>2,500</span></div>
        <div style='margin-top:6px; font-size:12px; color:#94a3b8;'>Stresses GPU Process / Software Rasterizer (SwiftShader)</div>
    </div>
    <canvas id='c'></canvas>

    <script>
        const canvas = document.getElementById('c');
        const ctx = canvas.getContext('2d', { alpha: false });
        let w = canvas.width = window.innerWidth;
        let h = canvas.height = window.innerHeight;

        window.addEventListener('resize', () => {
            w = canvas.width = window.innerWidth;
            h = canvas.height = window.innerHeight;
        });

        const BALL_COUNT = 2500;
        const balls = [];
        for (let i = 0; i < BALL_COUNT; i++) {
            balls.push({
                x: Math.random() * w,
                y: Math.random() * h,
                vx: (Math.random() - 0.5) * 5,
                vy: (Math.random() - 0.5) * 5,
                r: Math.random() * 4 + 2,
                color: `hsl(${Math.floor(Math.random() * 360)}, 80%, 60%)`
            });
        }

        let lastTime = performance.now();
        let frameCount = 0;
        let lastFpsUpdate = lastTime;

        function render(now) {
            frameCount++;
            if (now - lastFpsUpdate >= 500) {
                const fps = Math.round((frameCount * 1000) / (now - lastFpsUpdate));
                document.getElementById('fps').innerText = fps;
                frameCount = 0;
                lastFpsUpdate = now;
            }

            // Canvas background clear
            ctx.fillStyle = '#0f172a';
            ctx.fillRect(0, 0, w, h);

            // Intentional heavy composite operations to test software rasterizer
            ctx.shadowBlur = 4;
            for (let i = 0; i < BALL_COUNT; i++) {
                const b = balls[i];
                b.x += b.vx;
                b.y += b.vy;

                if (b.x - b.r < 0 || b.x + b.r > w) b.vx = -b.vx;
                if (b.y - b.r < 0 || b.y + b.r > h) b.vy = -b.vy;

                ctx.shadowColor = b.color;
                ctx.fillStyle = b.color;
                ctx.beginPath();
                ctx.arc(b.x, b.y, b.r, 0, Math.PI * 2);
                ctx.fill();
            }

            requestAnimationFrame(render);
        }

        requestAnimationFrame(render);
    </script>
</body>
</html>";
        }

        public static string GetTimerStressHtml()
        {
            return @"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>High-Frequency Timer Stress Test</title>
    <style>
        body { margin: 0; padding: 25px; background: #1e1e2e; color: #cdd6f4; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; }
        .card { background: #313244; padding: 20px; border-radius: 10px; border: 1px solid #45475a; max-width: 700px; margin-bottom: 20px; }
        h2 { margin-top: 0; color: #f38ba8; }
        .grid { display: grid; grid-template-columns: repeat(5, 1fr); gap: 10px; margin-top: 15px; }
        .box { background: #181825; padding: 10px; text-align: center; border-radius: 6px; font-size: 13px; font-family: monospace; color: #a6e3a1; }
        .pill { display: inline-block; padding: 4px 10px; border-radius: 12px; font-size: 12px; font-weight: bold; background: #f38ba8; color: #11111b; }
    </style>
</head>
<body>
    <div class='card'>
        <h2>High-Frequency Background Timer Stress <span class='pill'>50 Fast Intervals</span></h2>
        <p>This page spins up 50 unthrottled <code>setInterval(..., 0)</code> loops executing math hashing and DOM updates.</p>
        <p><strong>VM Test:</strong> In Default mode, background tabs burn 100% CPU. In Optimized mode (or when hidden), timers are throttled to 1Hz, instantly freeing CPU.</p>
        <div>Total Executions: <span id='totalOps' style='font-weight:bold; color:#89b4fa; font-size:18px;'>0</span></div>
    </div>

    <div class='card'>
        <div style='font-weight:bold; margin-bottom:8px;'>Active Interval Counters:</div>
        <div id='grid' class='grid'></div>
    </div>

    <script>
        const grid = document.getElementById('grid');
        const totalOpsElem = document.getElementById('totalOps');
        let totalOps = 0;

        const boxes = [];
        for (let i = 0; i < 50; i++) {
            const b = document.createElement('div');
            b.className = 'box';
            b.innerText = '0';
            grid.appendChild(b);
            boxes.push(b);
        }

        for (let i = 0; i < 50; i++) {
            let count = 0;
            setInterval(() => {
                count++;
                totalOps++;
                // Artificial CPU math loop
                let sum = 0;
                for (let j = 0; j < 500; j++) {
                    sum += Math.sqrt(j) * Math.sin(j);
                }
                if (count % 20 === 0) {
                    boxes[i].innerText = count;
                    totalOpsElem.innerText = totalOps.toLocaleString();
                }
            }, 0);
        }
    </script>
</body>
</html>";
        }

        public static string GetDomStressHtml()
        {
            return @"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>DOM Churn & Layout Thrashing Stress</title>
    <style>
        body { margin: 0; padding: 20px; background: #111827; color: #f9fafb; font-family: sans-serif; }
        .controls { background: #1f2937; padding: 15px; border-radius: 8px; margin-bottom: 15px; }
        .node-container { display: flex; flex-wrap: wrap; gap: 4px; }
        .node { width: 14px; height: 14px; border-radius: 3px; }
    </style>
</head>
<body>
    <div class='controls'>
        <h3>DOM Churn & Tree Thrashing Stress (3,000 Elements Mutating)</h3>
        <div>Mutations performed: <span id='mCount' style='color:#10b981; font-weight:bold;'>0</span></div>
    </div>
    <div id='container' class='node-container'></div>

    <script>
        const container = document.getElementById('container');
        const mCountElem = document.getElementById('mCount');
        const count = 3000;
        const nodes = [];

        for (let i = 0; i < count; i++) {
            const el = document.createElement('div');
            el.className = 'node';
            el.style.backgroundColor = '#374151';
            container.appendChild(el);
            nodes.push(el);
        }

        let mutations = 0;
        setInterval(() => {
            mutations += 300;
            for (let i = 0; i < 300; i++) {
                const idx = Math.floor(Math.random() * count);
                const r = Math.floor(Math.random() * 255);
                const g = Math.floor(Math.random() * 255);
                const b = Math.floor(Math.random() * 255);
                nodes[idx].style.backgroundColor = `rgb(${r},${g},${b})`;
                nodes[idx].style.transform = `scale(${Math.random() * 0.5 + 0.75})`;
            }
            mCountElem.innerText = mutations.toLocaleString();
        }, 50);
    </script>
</body>
</html>";
        }

        public static string GetOverviewHtml(string modeName, string description, bool isOptimized)
        {
            string badgeColor = isOptimized ? "#10b981" : "#ef4444";
            string badgeText = isOptimized ? "OPTIMIZED (VM HARDENED)" : "DEFAULT (UNTHROTTLED)";

            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>CefSharp Benchmark Environment</title>
    <style>
        body {{ margin: 0; padding: 30px; background: #0f172a; color: #f8fafc; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; }}
        .badge {{ display: inline-block; background: {badgeColor}; color: #ffffff; padding: 4px 12px; border-radius: 9999px; font-weight: bold; font-size: 13px; text-transform: uppercase; margin-bottom: 12px; }}
        .header {{ border-bottom: 1px solid #334155; padding-bottom: 20px; margin-bottom: 25px; }}
        h1 {{ margin: 0 0 10px 0; font-size: 26px; }}
        .lead {{ color: #94a3b8; font-size: 15px; margin: 0; }}
        .grid {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 20px; }}
        .card {{ background: #1e293b; border: 1px solid #334155; border-radius: 12px; padding: 20px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.3); }}
        .card h3 {{ margin-top: 0; color: #38bdf8; font-size: 18px; }}
        .features {{ list-style: none; padding-left: 0; }}
        .features li {{ padding: 6px 0; border-bottom: 1px solid #334155; font-size: 14px; color: #cbd5e1; }}
        .features li strong {{ color: #f1f5f9; }}
        .guide-box {{ background: rgba(56, 189, 248, 0.1); border-left: 4px solid #38bdf8; padding: 15px; border-radius: 4px; margin-top: 25px; font-size: 14px; }}
    </style>
</head>
<body>
    <div class='header'>
        <span class='badge'>{badgeText}</span>
        <h1>{modeName}</h1>
        <p class='lead'>{description}</p>
    </div>

    <div class='grid'>
        <div class='card'>
            <h3>🧪 Benchmark Scenarios</h3>
            <ul class='features'>
                <li><strong>Canvas Stress:</strong> 2,500 particles with shadows and continuous redraws. Demonstrates SwiftShader software rasterizer spikes.</li>
                <li><strong>Timer Stress:</strong> 50 unthrottled intervals testing JS background throttling.</li>
                <li><strong>DOM Churn:</strong> 3,000 rapid DOM mutations testing memory churn.</li>
                <li><strong>Real World:</strong> Navigate to any live website using the address bar above.</li>
            </ul>
        </div>

        <div class='card'>
            <h3>📊 Live Telemetry Bar</h3>
            <ul class='features'>
                <li><strong>Subprocess Count:</strong> Tracks all spawned <code>CefSharp.BrowserSubprocess</code> instances.</li>
                <li><strong>CefSubprocess CPU:</strong> Aggregated CPU percentage across all child processes.</li>
                <li><strong>Host CPU:</strong> Parent .NET UI process CPU usage.</li>
                <li><strong>Total Memory:</strong> Combined Working Set memory footprint in MB.</li>
            </ul>
        </div>
    </div>

    <div class='guide-box'>
        <strong>How to Compare:</strong> Run <code>CefDefaultBrowser.exe</code> and <code>CefOptimizedBrowser.exe</code> side-by-side. Click <em>'Canvas Stress'</em> in both. Watch the live telemetry bar at the top: the default version will spike CPU usage due to software rasterization, while the optimized version stays contained and responsive!
    </div>
</body>
</html>";
        }
    }
}
