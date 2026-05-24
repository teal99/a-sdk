@echo off

echo Publishing A Language SDK...

dotnet publish "src/A.CLI/A.CLI.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o ./dist/sdk/bin

echo.
echo Publish complete.
pause