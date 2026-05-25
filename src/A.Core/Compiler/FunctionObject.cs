using A.Core.Compiler;

namespace A.Core.Common;

public class FunctionObject
{
    public string Name { get; }
    public Chunk Chunk { get; } = new();
    public int Arity { get; set; }

    public FunctionObject(string name)
    {
        Name = name;
    }
}