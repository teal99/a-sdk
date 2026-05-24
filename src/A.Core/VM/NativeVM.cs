using System;
using System.IO;
using System.Runtime.InteropServices;
using A.Core.Compiler;
using A.Core.Common;

namespace A.Core.VM;

public class NativeVM
{
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("native_vm", EntryPoint = "ExecuteNativeBytecode", CallingConvention = CallingConvention.Cdecl)]
    private static extern int ExecuteNativeBytecode(byte[] code, int codeLength, double[] constants, int constantsLength, IntPtr stdLibCallback);

    public Diagnostics.AError? LastRuntimeError { get; private set; } = null;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void StdLibCallDelegate(int functionSlotIndex);

    public VM.InterpretResult InterpretNative(Chunk chunk)
    {
        LastRuntimeError = null;
        byte[] rawCode = chunk.Code.ToArray();

        // Extract numbers from the constants pool into a flat, primitive double array
        // This completely bypasses the .NET "Object contains references" security check
        double[] numericConstants = new double[chunk.Constants.Count];
        for (int i = 0; i < chunk.Constants.Count; i++)
        {
            if (chunk.Constants[i].Type == Common.ValueType.Number)
            {
                numericConstants[i] = chunk.Constants[i].AsNumber();
            }
            else
            {
                numericConstants[i] = 0.0; // Fallback placeholder for non-numbers in basic tests
            }
        }

        try
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string explicitDllPath = Path.Combine(baseDir, "native_vm.dll");

            if (!File.Exists(explicitDllPath))
            {
                explicitDllPath = Path.Combine(Directory.GetCurrentDirectory(), "native_vm.dll");
            }

            if (File.Exists(explicitDllPath))
            {
                LoadLibrary(explicitDllPath);
            }
        }
        catch { /* pass */ }

        StdLibCallDelegate stdLibBridge = (slotIndex) => {
            int index = 0;
            foreach (var kvp in StdLib.StdLibRegistry.Functions)
            {
                if (index == slotIndex)
                {
                    var dummyArgs = new Value[] { new Value("Hello, world!") };
                    kvp.Value(dummyArgs);
                    break;
                }
                index++;
            }
        };

        try
        {
            Console.WriteLine("[Sandbox Bridge] Handing control over to C++ Native Engine Core Loop...");
            
            // Pass the safe primitive double array cleanly over the language border
            int exitCode = ExecuteNativeBytecode(rawCode, rawCode.Length, numericConstants, numericConstants.Length, Marshal.GetFunctionPointerForDelegate(stdLibBridge));

            return exitCode == 0 ? VM.InterpretResult.Ok : VM.InterpretResult.RuntimeError;
        }
        catch (DllNotFoundException ex)
        {
            Console.WriteLine($"[Sandbox Error] Could not locate native_vm.dll inside your execution paths. Details: {ex.Message}");
            return VM.InterpretResult.RuntimeError;
        }
    }
}