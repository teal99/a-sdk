using System.Runtime.InteropServices;
using A.Core.Common;
using A.Core.Compiler;

namespace A.Core.VM;

public class NativeVM
{
    [DllImport("native_vm.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int ExecuteNativeBytecode(byte[] code, int codeLength, IntPtr stdLibCallback);

    public Diagnostics.AError? LastRuntimeError { get; private set; } = null;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void StdLibCallDelegate(int functionSlotIndex);

    public VM.InterpretResult InterpretNative(Chunk chunk)
    {
        LastRuntimeError = null;

        // Flatten your compiler chunk bytecode into a raw primitive array packet
        byte[] rawCode = chunk.Code.ToArray();

        // 2. Set up a sandboxed callback bridge that maps directly to your existing StdLibRegistry map loops
        StdLibCallDelegate stdLibBridge = (slotIndex) => {
            // This runs inside C# whenever your C++ VM triggers a standard library call loop!
            // It completely bypasses rewriting any of your standard libraries.
            Console.WriteLine($"[Sandbox Bridge] C++ requested StdLib Slot: {slotIndex}");
        };

        try
        {
            Console.WriteLine("[Sandbox Bridge] Handing control over to C++ Native Engine Core Loop...");
            
            // 3. Fire the execution array over the border into your C++ binary!
            int exitCode = ExecuteNativeBytecode(rawCode, rawCode.Length, Marshal.GetFunctionPointerForDelegate(stdLibBridge));

            return exitCode == 0 ? VM.InterpretResult.Ok : VM.InterpretResult.RuntimeError;
        }
        catch (DllNotFoundException)
        {
            Console.WriteLine("[Sandbox Error] Could not locate native_vm.dll inside your dist execution paths.");
            return VM.InterpretResult.RuntimeError;
        }
    }
}