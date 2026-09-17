@echo off
rem Builds with the C# compiler that ships with Windows (.NET Framework 4.x); no SDK needed.
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set FW=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319
set WM=%WINDIR%\System32\WinMetadata
set REFS=/r:"%WM%\Windows.Devices.winmd" /r:"%WM%\Windows.Foundation.winmd" /r:"%WM%\Windows.Storage.winmd" /r:"%FW%\System.Runtime.WindowsRuntime.dll" /r:"%FW%\System.Runtime.dll" /r:"%FW%\System.Runtime.InteropServices.WindowsRuntime.dll" /r:System.Windows.Forms.dll /r:System.Drawing.dll
set SRC=App.cs OS61.cs PpgStream.cs HeartRate.cs SpO2.cs SelfTest.cs
set OUT=%~1
if "%OUT%"=="" set OUT=%~dp0
cd /d "%~dp0"
"%CSC%" /nologo /optimize /target:winexe /out:"%OUT%\HeartRateTester.exe" %REFS% %SRC% || exit /b 1
"%CSC%" /nologo /optimize /target:exe /out:"%OUT%\hrdiag.exe" %REFS% %SRC% || exit /b 1
echo Built HeartRateTester.exe and hrdiag.exe
