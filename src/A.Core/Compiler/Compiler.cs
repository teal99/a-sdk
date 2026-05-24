using System.Collections.Generic;

namespace A.Core.Compiler;

public class Local
{
    public string Name { get; }
    public int Depth { get; }
    public bool IsMutable { get; }
    public Local(string name, int depth, bool isMutable = false)
    {
        Name = name;
        Depth = depth;
        IsMutable = isMutable;
    }
}

public class CompilerState
{
    public List<Local> Locals { get; } = new();
    public int ScopeDepth { get; set; } = 0;

    public void EnterScope() => ScopeDepth++;
    public void ExitScope() => ScopeDepth--;
}