@echo off
title CefSharp Benchmark - Side-by-Side Comparison
echo Launching Default Mode (Left / Background)...
start "" "%~dp0src\CefBenchmark.Default\bin\Release\net8.0-windows\win-x64\CefDefaultBrowser.exe"
timeout /t 2 /nobreak >nul
echo Launching Optimized Mode...
start "" "%~dp0src\CefBenchmark.Optimized\bin\Release\net8.0-windows\win-x64\CefOptimizedBrowser.exe"
echo Both instances launched! Compare CPU, Memory, and Subprocess behavior.
