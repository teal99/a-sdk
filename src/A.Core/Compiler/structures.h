#ifndef STRUCTURES_H
#define STRUCTURES_H
#include <vector>
#include <string>
#include <cstdint>
#include "../Common/value.h"

struct Chunk {
    std::vector<uint8_t> Code;
    std::vector<Value> Constants;

    void Write(uint8_t b) { 
        Code.push_back(b); 
    }
    
    int AddConstant(Value value) {
        Constants.push_back(value);
        return Constants.size() - 1;
    }
};

struct FunctionObject {
    std::string Name;
    Chunk chunk;
    int Arity = 0;

    FunctionObject(std::string name) : Name(name) {}
};

#endif