@echo off
cd /d "%~dp0"
echo Starting MGS PAD Editor with embedded Python...
python\python.exe main.py
if %errorlevel% neq 0 (
    echo.
    echo Press any key to exit...
    pause >nul
)