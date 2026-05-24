using A.Core.Common;

namespace A.Core.VM;

public struct CallFrame
{
    public FunctionObject Function { get; }
    public int Ip { get; set; } // Tracks the instruction pointer for this specific frame
    public int SlotsStart { get; } // Tracks where this function's local variables begin on the VM value stack

    public CallFrame(FunctionObject function, int slotsStart)
    {
        Function = function;
        Ip = 0;
        SlotsStart = slotsStart;
    }
}