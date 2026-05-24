@echo off
echo Building A Language...

dotnet build A-Language.slnx

if %ERRORLEVEL% neq 0 (
    echo Build failed.
    exit /b %ERRORLEVEL%
)

echo [NATIVE EXTENSION] Compiling C++ Virtual Machine Core Loop...
g++ -O3 -shared native-vm/main.cpp native-vm/vm.cpp native-vm/value.cpp native-vm/diagnostics.cpp -o dist/native_vm.dll

if %ERRORLEVEL% neq 0 (
    echo Native C++ compilation failed.
    exit /b %ERRORLEVEL%
)

echo Build succeeded.