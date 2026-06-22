@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo ================================
echo   LocalMark 发布打包
echo   生成独立可执行文件...
echo ================================
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
if %ERRORLEVEL% EQU 0 (
    echo.
    echo ================================
    echo   打包成功！
    echo   输出: %~dp0publish\LocalMark.exe
    echo ================================
    echo.
    echo 双击 publish\LocalMark.exe 即可一键启动（无需安装 .NET 运行时）
    pause
) else (
    echo.
    echo [错误] 打包失败
    pause
)
