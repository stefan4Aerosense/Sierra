@echo off
rem Tiny static server for local testing (PowerShell HttpListener - nothing to install).
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0serve.ps1"
