@echo off
title CefSharp Benchmark - Default Mode (Unthrottled)
echo Launching CefDefaultBrowser (Unthrottled)...
start "" "%~dp0src\CefBenchmark.Default\bin\Release\net8.0-windows\win-x64\CefDefaultBrowser.exe"
