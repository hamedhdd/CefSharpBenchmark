@echo off
title CefSharp Benchmark - Optimized Mode (VM Safe)
echo Launching CefOptimizedBrowser (VM Hardened with Job Object)...
start "" "%~dp0src\CefBenchmark.Optimized\bin\Release\net8.0-windows\win-x64\CefOptimizedBrowser.exe"
