@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0start-infrastructure.ps1" %*
exit /b %ERRORLEVEL%
