@echo off
rem Convert a Fusion export (OBJ, FBX, STL, ...) to GLB using Blender. Nothing else to install.
rem   to-glb.cmd "C:\path\H1 - Assembly.obj"            -> writes H1 - Assembly.glb beside it
rem   to-glb.cmd "C:\path\in.obj" "C:\path\out.glb"
setlocal
set BLENDER=C:\Program Files\Blender Foundation\Blender 5.2\blender.exe
if not exist "%BLENDER%" (
  echo Blender not found at "%BLENDER%" - edit this file and point BLENDER at your blender.exe
  exit /b 1
)
if "%~1"=="" (
  echo Usage: to-glb.cmd ^<input file^> [output.glb]
  exit /b 1
)
"%BLENDER%" --background --factory-startup --python "%~dp0to_glb.py" -- "%~1" %2
