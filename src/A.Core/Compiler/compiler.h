#ifndef COMPILER_H
#define COMPILER_H
#include <string>
#include <vector>

class Local {
public:
    std::string Name;
    int Depth;
    bool IsMutable;

    Local(std::string name, int depth, bool isMutable = false) 
        : Name(name), Depth(depth), IsMutable(isMutable) {}
};

class CompilerState {
public:
    std::vector<Local> Locals;
    int ScopeDepth = 0;

    void EnterScope() { ScopeDepth++; }
    void ExitScope()  { ScopeDepth--; }
};


#endif