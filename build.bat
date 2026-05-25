@echo off
echo Building A Language...

dotnet build A-Language.slnx

if %ERRORLEVEL% neq 0 (
    echo Build failed.
    exit /b %ERRORLEVEL%
)

echo Build succeeded.