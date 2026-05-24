@echo off
echo Building A Language...

dotnet build A-Language.slnx

if %ERRORLEVEL% neq 0 (
    echo Build failed.
    exit /b %ERRORLEVEL%
)

echo [NATIVE EXTENSION] Compiling C++ Virtual Machine Core Loop...
g++ -O3 -shared -static-libgcc -static-libstdc++ src/A.Core/VM/main.cpp src/A.Core/Common/value.cpp src/A.Core/VM/vm.cpp src/A.Core/Diagnostics/diagnostics.cpp -o src/A.CLI/bin/Debug/net10.0/native_vm.dll

if %ERRORLEVEL% neq 0 (
    echo Native C++ compilation failed.
    exit /b %ERRORLEVEL%
)

copy src\A.CLI\bin\Debug\net10.0\native_vm.dll native_vm.dll > nul

echo Build succeeded.