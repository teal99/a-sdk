using A.Core.Compiler;

namespace A.Core.Common;

public class FunctionObject
{
    public string Name { get; }
    public Chunk Chunk { get; } = new();
    public int Arity { get; set; } // Track number of expected parameters

    public FunctionObject(string name)
    {
        Name = name;
    }
}