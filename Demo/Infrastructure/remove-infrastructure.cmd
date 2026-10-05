@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0remove-infrastructure.ps1" %*
exit /b %ERRORLEVEL%
