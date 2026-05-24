namespace A.Core.VM;

public enum OpCode: byte
{
    // ----------------------------------------------------------------->>> EXPRESSIONS
    Constant, // Pushes a constant from the pool onto the stack
    Add, // Pops 2 values, adds them, pushes result
    Subtract, // Pops 2 values, subtracts them, pushes result
    Multiply, // Pops 2 values, multiplies them, pushes result
    Divide, // Pops 2 values, divides them, pushes result
    Modulo, // Pops 2 values, calculates a % b, pushes result
    Negate, // Pushes 1 value, negates it, pushes result
    // ----------------------------------------------------------------->>> VARIABLES + MORE
    DefineGlobal, // Registers a new variable in the lookup table
    GetGlobal, // Fetches a variable value by name
    AssignGlobal, // Updates an existing mutable variable
    GetGlobalFast, // Reads directly from an array slot byte operand
    AssignGlobalFast, // Writes directly to an array slot byte operand
    GetLocal, // Reads a value from a specific stack slot index
    SetLocal, // Overwrites a value at a specific stack slot index
    BuildArray, // Pops N items off the stack and packages them into a List object wrapper
    GetIndex, // Pops index and array, reads value, pushes result
    SetIndex, // Pops value, index, and array, updates position
    BuildMap, // Packs key-value pairs off the stack into a Dictionary
    GetProperty,  // Pops property name and object, reads value, pushes result
    SetProperty, // Pops value, property name, and object, updates position
    DefineClass,
    Instantiate,
    // ----------------------------------------------------------------->>> LOGIC
    Pop, // Pops the top value off the stack and discards it
    True, // Pushes 'true' onto the stack
    False, // Pushes 'false' onto the stack
    Equal, // Pops 2 values, pushes true if equal
    Greater, // Pops 2 values, pushes true if a > b
    Lesser, // Pops 2 values, pushes true if a < b
    GreaterEqual, // Pops 2 values, pushes true if a > b or a == b (a >= b)
    LesserEqual, // Pops 2 values, pushes true if a < b or a == b (a <= b)
    EqualEqual, // Pops 2 values, pushes true if a == b
    UnEqual, // Pops 2 values, pushes true if a != b
    Not, // Pops 1 value, flips its truthiness, pushes boolean result back
    Call,
    // ----------------------------------------------------------------->>> LOGIC --> CONTROL FLOW
    JumpIfFalse, // Jumps forward if the top stack value is false
    Jump, // Unconditionally jumps forward (used to skip the 'else' block)
    Loop, // Jumps backwards to a previous execution offset frame coordinate
    // ----------------------------------------------------------------->>> STATE
    Nil, // None
    Return // Exits the current execution frame
    // ----------------------------------------------------------------->>> END
}