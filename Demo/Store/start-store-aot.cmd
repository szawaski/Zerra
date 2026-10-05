@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0start-store-aot.ps1" %*
exit /b %ERRORLEVEL%
