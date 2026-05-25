@echo off
cls
echo ===================================================
echo   A LANGUAGE INTEGRATED MASTER TOOLCHAIN RUNNER
echo ===================================================

echo.
echo [1/3] Building Core Compiler and Executables...
call .\build.bat
if %ERRORLEVEL% neq 0 (
    color 0C
    echo Build compilation process failed. Halting pipeline.
    color 0F
    exit /b %ERRORLEVEL%
)

echo.
echo [2/3] Publishing Standalone Optimized Production SDK...
dotnet publish "src/A.CLI/A.CLI.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o ./dist/sdk/bin > nul
if %ERRORLEVEL% neq 0 (
    color 0C
    echo Production release publication failed. Halting pipeline.
    color 0F
    exit /b %ERRORLEVEL%
)
echo Publish complete. Standalone single-file binary artifact ready.

echo.
echo [3/3] Launching Runtime Test Environment...
echo ---------------------------------------------------
call .\run.bat
echo ---------------------------------------------------
echo Execution cycle complete.