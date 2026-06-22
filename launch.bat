@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo ================================
echo   LocalMark 本地数据标注工具
echo   正在启动...
echo ================================
dotnet run
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [错误] 启动失败，请检查 .NET 8 SDK 是否已安装
    echo 下载地址: https://dotnet.microsoft.com/download
    pause
)
