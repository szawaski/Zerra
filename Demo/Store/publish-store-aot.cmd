@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0publish-store-aot.ps1" %*
exit /b %ERRORLEVEL%
