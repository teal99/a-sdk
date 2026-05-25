@echo off

echo Publishing A Language SDK...

REM Clean old output directory before compiling
if exist dist rmdir /s /q dist

dotnet publish "src/A.CLI/A.CLI.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o ./dist/sdk/bin

if %ERRORLEVEL% neq 0 (
    echo [ERROR]: Native .NET assembly compilation failed. Distribution packaging aborted.
    exit /b %ERRORLEVEL%
)

echo.
echo [INFO]: Staging release directory assets...
mkdir dist\sdk\examples

REM Copy the license and working base files into the distribution envelope
if exist LICENSE copy LICENSE dist\sdk\LICENSE > nul
if exist examples copy examples dist\sdk\examples > nul

echo.
echo Publish complete.
echo Standalone portable binary bundle created inside: .\dist\sdk
pause