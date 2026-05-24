using System.Collections.Generic;
using A.Core;
using A.Core.Common;
using A.Core.VM;

namespace A.Core.Compiler;

public class Chunk
{
    public List<byte> Code { get; } = new();
    public List<Value> Constants { get; } = new();

    public void Write(byte b) => Code.Add(b);
    public void Write(OpCode op) => Code.Add((byte)op);

    public int AddConstant(Value value)
    {
        Constants.Add(value);
        return Constants.Count - 1;
    }
}