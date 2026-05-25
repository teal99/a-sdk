@echo off
set "COMMAND=%~1"
set "TARGET_FILE=%~2"

if "%COMMAND%"=="" goto :usage
if "%COMMAND%"=="help" goto :usage

if "%COMMAND%"=="build" (
    call publish.bat
    exit /b 0
)

if "%COMMAND%"=="run" (
    if "%TARGET_FILE%"=="" set "TARGET_FILE=main.a"
    echo ===================================================
    echo   A LANGUAGE INTEGRATED MASTER TOOLCHAIN RUNNER
    echo ===================================================
    echo Running target: %TARGET_FILE%
    echo ---------------------------------------------------
    
    dotnet run --project src/A.CLI/A.CLI.csproj -- "%TARGET_FILE%"
    
    echo ---------------------------------------------------
    echo Execution cycle complete.
    exit /b 0
)

echo [Error]: Unknown toolchain command '%COMMAND%'.
goto :usage

:usage
echo ===================================================
echo   A LANGUAGE SDK CLI COMMAND ENVIRONMENT CONTROLLER
echo ===================================================
echo Usage:
echo   a build          - Triggers the dotnet single-file compilation pass.
echo   a run [file.a]   - Runs a specific script. Defaults to main.a if empty.
echo   a help           - Displays this command menu layout panel.
exit /b 1