using A.Core.Common;
using A.Core.Compiler;

namespace A.Core.VM;

public class VM
{
    private static readonly Dictionary<string, Func<Value[], Value>> _stringPrototype = new()
    {
        { "Length",  args => new Value((double)args[0].AsString().Length) },
        { "ToUpper", args => new Value(args[0].AsString().ToUpper()) },
        { "ToLower", args => new Value(args[0].AsString().ToLower()) }
    };

    private static readonly Dictionary<string, Func<Value[], Value>> _arrayPrototype = new()
    {
        { "Length",  args => new Value((double)args[0].AsArray().Count) }
    };

    private readonly CallFrame[] _frames = new CallFrame[1024];
    private int _frameCount = 0;
    private readonly Value[] _stack = new Value[2048];
    private int _stackTop = 0;

    private readonly Value[] _globalSlots = new Value[256];
    private readonly Dictionary<string, int> _globalSymbolTable = new();
    private readonly int _globalCount = 0;

    public Diagnostics.AError? LastRuntimeError { get; private set; } = null;

    public VM()
    {

        int index = 0;
        foreach (var kvp in StdLib.StdLibRegistry.Functions)
        {
            _globalSlots[index] = new Value(args => kvp.Value(args));
            
            _globalSymbolTable[kvp.Key] = index;
            index++;
        }
        
        _globalCount = StdLib.StdLibRegistry.BuiltInCount;
    }

    public enum InterpretResult
    {
        Ok,
        CompileError,
        RuntimeError
    }

    public InterpretResult Interpret(Chunk chunk, bool isReplMode = false)
    {
        LastRuntimeError = null; // Clear previous runtime error flags

        if (!isReplMode)
        {
            _stackTop = 0;
            int standardLibraryBuiltInCount = StdLib.StdLibRegistry.BuiltInCount;
            for (int i = standardLibraryBuiltInCount; i < _globalSlots.Length; i++)
            {
                _globalSlots[i] = new Value(); // Reset to Nil
            }
        }

        FunctionObject scriptMain = new(isReplMode ? "repl_line" : "main");
        scriptMain.Chunk.Code.AddRange(chunk.Code);
        scriptMain.Chunk.Constants.AddRange(chunk.Constants);
        
        _frameCount = 0;

        int slotStart = _stackTop;
        Push(new Value(scriptMain));
        _frames[_frameCount++] = new CallFrame(scriptMain, slotStart);

        return Run();
    }

    private InterpretResult Run()
    {
        while (_frameCount > 0)
        {
            // Console.WriteLine($"IP={_frames[_frameCount - 1].Ip}, OP={(OpCode)_frames[_frameCount - 1].Function.Chunk.Code[_frames[_frameCount - 1].Ip]}");
            // Console.WriteLine($"STACK: [{string.Join(", ", _stack.Take(_stackTop).Select(v => v.ToString()))}]");
            if (_frameCount - 1 < 0 || _frameCount - 1 >= _frames.Length)
            {
                LastRuntimeError = new Diagnostics.AError
                {
                    Type = "Call Stack",
                    Message = "Runtime Fatal Error: Virtual Machine Call Stack Frame index out of bounds tracking limits.",
                    Suggestion = "Check for deep or infinite recursive call chains inside your user functions loops.",
                    Line = 1,
                    File = "main.a",
                    Severity = Diagnostics.AErrorSeverity.Fatal
                };
                return InterpretResult.RuntimeError;
            }
            CallFrame frame = _frames[_frameCount - 1];

            if (frame.Ip < 0 || frame.Ip >= frame.Function.Chunk.Code.Count)
            {
                LastRuntimeError = new Diagnostics.AError
                {
                    Type = "Runtime Execution",
                    Message = "Runtime Fatal Error: Instruction Pointer jumped out of active bytecode bounds.",
                    Suggestion = "Ensure loop boundaries or conditional control branches backpatch cleanly.",
                    Line = frame.Ip,
                    File = "main.a",
                    Severity = Diagnostics.AErrorSeverity.Fatal
                };
                return InterpretResult.RuntimeError;
            }
            int activeFrameIndex = _frameCount - 1;
            OpCode instruction = (OpCode)_frames[activeFrameIndex].Function.Chunk.Code[_frames[activeFrameIndex].Ip++];
            switch (instruction)
            {
                case OpCode.Constant:
                    {
                        byte index = _frames[activeFrameIndex].Function.Chunk.Code[_frames[activeFrameIndex].Ip++];
                        Value constant = _frames[activeFrameIndex].Function.Chunk.Constants[index];
                        Push(constant);
                        break;
                    }
                case OpCode.Add:       BinaryOp((a, b) => new Value(a + b)); break;
                case OpCode.Subtract:  BinaryOp((a, b) => new Value(a - b)); break;
                case OpCode.Multiply:  BinaryOp((a, b) => new Value(a * b)); break;
                case OpCode.Divide:    BinaryOp((a, b) => new Value(a / b)); break;
                case OpCode.Modulo:    BinaryOp((a, b) => new Value(a % b)); break;
                case OpCode.Negate:
                    {
                        if (Peek(0).Type != Common.ValueType.Number)
                        {
                            LastRuntimeError = new Diagnostics.AError
                            {
                                Type = "Runtime Math",
                                Message = "Operands must be valid numbers for arithmetic calculations.",
                                Suggestion = $"Check your stack values. You attempted to process a calculation using types: {Peek(1).Type} and {Peek(0).Type}.",
                                Line = _frames[activeFrameIndex].Function.Chunk.Code.Count > 0 ? _frames[activeFrameIndex].Ip : 1,
                                File = "main.a",
                                Severity = Diagnostics.AErrorSeverity.Fatal
                            };
                            return InterpretResult.RuntimeError;
                        }
                        Push(new Value(-Pop().AsNumber()));
                        break;
                    }
                    
                case OpCode.DefineGlobal:
                    {
                        byte slotIndex = _frames[activeFrameIndex].Function.Chunk.Code[_frames[activeFrameIndex].Ip++];
                        _globalSlots[slotIndex] = Pop();
                        break;
                    }

                case OpCode.GetGlobal:
                    {
                        // Read the operand by advancing the true array-backed pointer directly
                        byte slotIndex = _frames[activeFrameIndex].Function.Chunk.Code[_frames[activeFrameIndex].Ip++];
                        Push(_globalSlots[slotIndex]); 
                        break;
                    }
                case OpCode.AssignGlobal:
                case OpCode.GetGlobalFast:
                case OpCode.AssignGlobalFast:
                    {
                        // Read the slot coordinate byte operand directly, bypassing the constant pool
                        byte slotIndex = _frames[activeFrameIndex].Function.Chunk.Code[_frames[activeFrameIndex].Ip++];
                        _globalSlots[slotIndex] = Peek(0);
                        break;
                    }
                case OpCode.True: Push(new Value(true)); break;
                case OpCode.False: Push(new Value(false)); break;
                case OpCode.Equal:
                    {
                        Value b = Pop();
                        Value a = Pop();
                        Push(new Value(a.Type == b.Type && a.ToString() == b.ToString()));
                        break;
                    }
                case OpCode.Greater: CompareOp((a, b) => a > b); break;
                case OpCode.Lesser: CompareOp((a, b) => a < b); break;
                case OpCode.GreaterEqual: CompareOp((a, b) => a >= b); break;
                case OpCode.LesserEqual: CompareOp((a, b) => a <= b); break;
                case OpCode.EqualEqual:
                    {
                        Value b = Pop();
                        Value a = Pop();
                        // Safely evaluate matching primitive variant types and string outputs universally
                        bool areEqual = a.Type == b.Type && a.ToString() == b.ToString();
                        Push(new Value(areEqual));
                        break;
                    }
                    
                case OpCode.UnEqual:
                    {
                        Value b = Pop();
                        Value a = Pop();
                        // Invert the structural equality result
                        bool areNotEqual = !(a.Type == b.Type && a.ToString() == b.ToString());
                        Push(new Value(areNotEqual));
                        break;
                    }
                case OpCode.Call:
                    {
                        byte argCount = ReadByte();
                        Value callee = _stack[_stackTop - argCount - 1];
                        
                        if (callee.Type == Common.ValueType.NativeFn) // Look beneath the arguments to find the function object
                        {
                            Value[] args = new Value[argCount];
                            for (int i = argCount - 1; i >= 0; i--) args[i] = Pop();
                            Pop(); // Discard the native function identifier from the stack
                            Value result = callee.AsNativeFn()(args);
                            Push(result);
                        }
                        else if (callee.Type == Common.ValueType.Function)
                        {
                            FunctionObject function = callee.AsFunction();
                            if (argCount != function.Arity)
                            {
                                LastRuntimeError = new Diagnostics.AError
                                {
                                    Type = "Runtime",
                                    Message = $"Expected {function.Arity} arguments, but got {argCount}.",
                                    Suggestion = "Check the number of arguments passed to the function.",
                                    Line = _frames[_frameCount - 1].Function.Chunk.Code.Count > 0 ? _frames[_frameCount - 1].Ip : 1,
                                    File = "main.a",
                                    Severity = Diagnostics.AErrorSeverity.Fatal
                                };
                                return InterpretResult.RuntimeError;
                            }
                            if (_frameCount >= _frames.Length)
                            {
                                LastRuntimeError = new Diagnostics.AError
                                {
                                    Type = "Runtime",
                                    Message = "Stack Overflow.",
                                    Suggestion = "Check for infinite recursion or deeply nested function calls.",
                                    Line = _frames[_frameCount - 1].Function.Chunk.Code.Count > 0 ? _frames[_frameCount - 1].Ip : 1,
                                    File = "main.a",
                                    Severity = Diagnostics.AErrorSeverity.Fatal
                                };
                                return InterpretResult.RuntimeError;
                            }
                            int slotsStart = _stackTop - argCount - 1;
                            _frames[_frameCount++] = new CallFrame(function, slotsStart);
                        }
                        else
                        {
                            LastRuntimeError = new Diagnostics.AError
                            {
                                Type = "Runtime",
                                Message = "Runtime Error: Can only call functions.",
                                Suggestion = "Ensure you are calling a valid function.",
                                Line = _frames[_frameCount - 1].Function.Chunk.Code.Count > 0 ? _frames[_frameCount - 1].Ip : 1,
                                File = "main.a",
                                Severity = Diagnostics.AErrorSeverity.Fatal
                            };
                            return InterpretResult.RuntimeError;
                        }
                        break;
                    }
                case OpCode.JumpIfFalse:
                    {
                        ushort offset = ReadShort();
                        Value condition = Pop();
                        bool shouldJump = condition.Type == Common.ValueType.Nil || (condition.Type == Common.ValueType.Boolean && !condition.AsBoolean());
                        if (shouldJump)
                        {
                            _frames[_frameCount - 1].Ip += offset;
                        }
                        break;
                    }
                    
                case OpCode.Jump:
                    {
                        ushort offset = ReadShort();
                        _frames[_frameCount - 1].Ip += offset;
                        break;
                    }
                case OpCode.Loop:
                    {
                        ushort offset = ReadShort();
                        _frames[_frameCount - 1].Ip -= offset;
                        break;
                    }
                case OpCode.Not: Push(new Value(IsFalsey(Pop()))); break;
                case OpCode.GetLocal:
                    {
                        byte slot = ReadByte();
                        Push(_stack[CurrentFrame.SlotsStart + slot]);
                        break;
                    }
                case OpCode.SetLocal:
                    {
                        byte slot = ReadByte();
                        _stack[CurrentFrame.SlotsStart + slot] = Peek(0);
                        break;
                    }
                case OpCode.Pop:
                    {
                        Value discarded = Pop();
                        if (CurrentFrame.Function.Name == "repl_line" && discarded.Type != Common.ValueType.Nil)
                        {
                            Console.WriteLine(discarded.ToString());
                        }
                        break;
                    }
                case OpCode.BuildMap:
                    {
                        byte pairCount = ReadByte();
                        var map = new Dictionary<string, Value>();

                        for (int i = 0; i < pairCount; i++)
                        {
                            Value value = Pop();
                            Value key = Pop();

                            if (key.Type != Common.ValueType.String)
                            {
                                LastRuntimeError = new Diagnostics.AError
                                {
                                    Type = "Type",
                                    Message = $"Runtime Error: Expected string literal for dictionary property key, discovered '{key.Type}'.",
                                    Line = _frames[_frameCount - 1].Ip,
                                    File = "main.a",
                                    Severity = Diagnostics.AErrorSeverity.Fatal
                                };

                                return InterpretResult.RuntimeError;
                            }

                            map[key.AsString()] = value;
                        }

                        Push(new Value(map));
                        break;
                    }
                case OpCode.BuildArray:
                    {
                        byte elementCount = ReadByte();
                        var array = new List<Value>();

                        for (int i = 0; i < elementCount; i++)
                        {
                            array.Add(Pop());
                        }

                        array.Reverse();

                        Push(new Value(array));
                        break;
                    }
                case OpCode.GetIndex:
                    {
                        Value indexVal = Pop();
                        Value collectionVal = Pop();

                        if (collectionVal.Type == Common.ValueType.Dictionary)
                        {
                            string key = indexVal.AsString();
                            var dict = collectionVal.AsDictionary();

                            if (dict.TryGetValue(key, out Value? dictValue))
                            {
                                Push(dictValue);
                            }
                            else
                            {
                                Push(new Value());
                            }
                        }
                        else if (collectionVal.Type == Common.ValueType.Array)
                        {
                            int index = (int)indexVal.AsNumber();
                            List<Value> array = collectionVal.AsArray();

                            if (index < 0 || index >= array.Count)
                            {
                                LastRuntimeError = new Diagnostics.AError
                                {
                                    Type = "Runtime",
                                    Message = "Runtime Error: Array collection index out of bounds boundaries.",
                                    Suggestion = $"Check the array index is within valid range (0 - {array.Count - 1}).",
                                    Line = _frames[_frameCount - 1].Function.Chunk.Code.Count > 0 ? _frames[_frameCount - 1].Ip : 1,
                                    File = "main.a",
                                    Severity = Diagnostics.AErrorSeverity.Fatal
                                };
                                return InterpretResult.RuntimeError;
                            }
                            Push(array[index]);
                        }
                        else
                        {
                            LastRuntimeError = new Diagnostics.AError
                            {
                                Type = "Type Error",
                                Message = $"Runtime Error: Subscript indexing lookup operator is not supported on Type variant '{collectionVal.Type}'.",
                                Line = _frames[_frameCount - 1].Ip,
                                File = "main.a",
                                Severity = Diagnostics.AErrorSeverity.Fatal
                            };
                            return InterpretResult.RuntimeError;
                        }
                        break;
                    }
                case OpCode.SetIndex:
                    {
                        Value valueToSet = Pop();
                        Value indexVal = Pop();
                        Value collectionVal = Pop();

                        if (collectionVal.Type == Common.ValueType.Dictionary)
                        {
                            string key = indexVal.AsString();
                            var dict = collectionVal.AsDictionary();
                            
                            dict[key] = valueToSet;
                            Push(valueToSet);
                        }
                        else if (collectionVal.Type == Common.ValueType.Array)
                        {
                            int index = (int)indexVal.AsNumber();
                            List<Value> array = collectionVal.AsArray();

                            if (index < 0 || index >= array.Count)
                            {
                                LastRuntimeError = new Diagnostics.AError
                                {
                                    Type = "Runtime",
                                    Message = "Runtime Error: Array collection index out of bounds boundaries.",
                                    Suggestion = $"Check the array index is within valid range (0 - {array.Count - 1}).",
                                    Line = _frames[_frameCount - 1].Function.Chunk.Code.Count > 0 ? _frames[_frameCount - 1].Ip : 1,
                                    File = "main.a",
                                    Severity = Diagnostics.AErrorSeverity.Fatal
                                };
                                return InterpretResult.RuntimeError;
                            }
                            array[index] = valueToSet;
                            Push(valueToSet);
                        }
                        else
                        {
                            LastRuntimeError = new Diagnostics.AError
                            {
                                Type = "Type Error",
                                Message = $"Runtime Error: Cannot mutate indexed index properties on Type variant '{collectionVal.Type}'.",
                                Line = _frames[_frameCount - 1].Ip,
                                File = "main.a",
                                Severity = Diagnostics.AErrorSeverity.Fatal
                            };
                            return InterpretResult.RuntimeError;
                        }
                        break;
                    }
                case OpCode.GetProperty:
                    {
                        byte index = _frames[activeFrameIndex].Function.Chunk.Code[_frames[activeFrameIndex].Ip++];
                        string propertyName = _frames[activeFrameIndex].Function.Chunk.Constants[index].AsString();

                        Value instanceValue = Pop(); // Extract the container/instance object from the value stack top

                        // --- ROUTE 1: STATIC NAMESPACES & STATIC LIBRARIES ---
                        if (instanceValue.Type == Common.ValueType.Module)
                        {
                            var moduleDict = instanceValue.AsModule();
                            if (moduleDict.TryGetValue(propertyName, out Value? boundItem))
                            {
                                Push(boundItem);
                            }
                            else
                            {
                                LastRuntimeError = new Diagnostics.AError
                                {
                                    Type = "Namespace Error",
                                    Message = $"Runtime Error: Static component or method '{propertyName}' does not exist inside Module scope.",
                                    Line = _frames[activeFrameIndex].Ip,
                                    File = "main.a",
                                    Severity = Diagnostics.AErrorSeverity.Fatal
                                };
                                return InterpretResult.RuntimeError;
                            }
                            break;
                        }

                        // --- ROUTE 2: DYNAMIC USER DICTIONARIES ---
                        if (instanceValue.Type == Common.ValueType.Dictionary)
                        {
                            var dict = instanceValue.AsDictionary();
                            if (dict.TryGetValue(propertyName, out Value? dictValue)) Push(dictValue);
                            else Push(new Value()); // Fallback to Nil if missing safely
                            break;
                        }

                        // --- ROUTE 3: CLASS / STRUCT INSTANCES (DYNAMIC METHOD BINDING) ---
                        if (instanceValue.Type == Common.ValueType.Instance)
                        {
                            var instanceObj = instanceValue.AsInstance();

                            // A. State variables/Fields take explicit data priority tracking lookahead
                            if (instanceObj.Fields.TryGetValue(propertyName, out Value? fieldValue))
                            {
                                Push(fieldValue);
                                break;
                            }

                            // B. FIX: Look up the method dynamically using the qualified prefix (e.g., "Player.takeDamage")
                            // straight from your global slots table! This completely bypasses early-binding index crashes.
                            string fullyQualifiedMethodName = instanceObj.ClassName + "." + propertyName;
                            
                            if (_globalSymbolTable.TryGetValue(fullyQualifiedMethodName, out int methodSlot) && methodSlot < _globalCount)
                            {
                                Value methodValue = _globalSlots[methodSlot];

                                Value boundMethodClosure = new Value((NativeFunction)(args => {
                                    Value[] boundArgs = new Value[args.Length + 1];
                                    boundArgs[0] = instanceValue; // Passes 'this' implicitly as parameter index 0
                                    Array.Copy(args, 0, boundArgs, 1, args.Length);
                                    
                                    if (methodValue.Type == Common.ValueType.Function)
                                    {
                                        var subExecutor = new VM();
                                        // Execute the function in a sub-VM; discard the InterpretResult
                                        subExecutor.Interpret(methodValue.AsFunction().Chunk);
                                        // Return Nil as functions invoked this way do not produce a direct Value here
                                        return new Value();
                                    }
                                    else if (methodValue.Type == Common.ValueType.NativeFn)
                                    {
                                        return methodValue.AsNativeFn()(boundArgs);
                                    }
                                    return new Value();
                                }));
                                
                                Push(boundMethodClosure);
                                break;
                            }
                        }

                        // --- ROUTE 4: PRIMITIVE PROTOTYPES FALLBACK SUITE ---
                        Func<Value[], Value>? resolvedPrimitiveMethod = null;
                        if (instanceValue.Type == Common.ValueType.String) _stringPrototype.TryGetValue(propertyName, out resolvedPrimitiveMethod);
                        else if (instanceValue.Type == Common.ValueType.Array) _arrayPrototype.TryGetValue(propertyName, out resolvedPrimitiveMethod);

                        if (resolvedPrimitiveMethod != null)
                        {
                            Value primitiveBoundClosure = new Value((NativeFunction)(args => {
                                Value[] boundArgs = new Value[args.Length + 1];
                                boundArgs[0] = instanceValue;
                                Array.Copy(args, 0, boundArgs, 1, args.Length);
                                return resolvedPrimitiveMethod(boundArgs);
                            }));
                            Push(primitiveBoundClosure);
                            break;
                        }

                        // COMPREHENSIVE TYPE SAFETY DEFENSE GUARD
                        LastRuntimeError = new Diagnostics.AError
                        {
                            Type = "Property Error",
                            Message = $"Runtime Error: Property or method '{propertyName}' does not exist on data Type variant '{instanceValue.Type}'.",
                            Line = _frames[activeFrameIndex].Ip,
                            File = "main.a",
                            Severity = Diagnostics.AErrorSeverity.Fatal
                        };
                        return InterpretResult.RuntimeError;
                    }
                case OpCode.SetProperty:
                    {
                        byte index = _frames[activeFrameIndex].Function.Chunk.Code[_frames[activeFrameIndex].Ip++];
                        string propertyName = _frames[activeFrameIndex].Function.Chunk.Constants[index].AsString();

                        // Value stack layout right now: [instance/container, value_to_assign] -> assigned value is on top!
                        Value valueToAssign = Pop();
                        Value containerValue = Pop();

                        // A. Handle mutations inside User Struct/Class instances dynamically
                        if (containerValue.Type == Common.ValueType.Instance)
                        {
                            var instanceObj = containerValue.AsInstance();
                            instanceObj.Fields[propertyName] = valueToAssign; // Assign or update the field value
                            
                            // Push the assigned value back onto the stack to match expression semantics requirements
                            Push(valueToAssign); 
                        }
                        // B. Handle mutations inside standard Dictionaries dynamically
                        else if (containerValue.Type == Common.ValueType.Dictionary)
                        {
                            var dict = containerValue.AsDictionary();
                            dict[propertyName] = valueToAssign;
                            Push(valueToAssign);
                        }
                        else
                        {
                            LastRuntimeError = new Diagnostics.AError
                            {
                                Type = "Property Mutation Error",
                                Message = $"Runtime Error: Cannot assign property '{propertyName}' onto an immutable Type variant '{containerValue.Type}'.",
                                Line = _frames[activeFrameIndex].Ip,
                                File = "main.a",
                                Severity = Diagnostics.AErrorSeverity.Fatal
                            };
                            return InterpretResult.RuntimeError;
                        }
                        break;
                    }
                case OpCode.DefineClass:
                    {
                        // Read the class array slot operand byte directly, bypassing the constant pool
                        byte classSlotIndex = _frames[activeFrameIndex].Function.Chunk.Code[_frames[activeFrameIndex].Ip++];

                        // Initialize the class slot as a clean Module object container natively inside the global slots array
                        _globalSlots[classSlotIndex] = new Value(new Dictionary<string, Value>(), isModule: false);
                        break;
                    }
                case OpCode.Instantiate:
                    {
                        
                        byte classSlotIndex = _frames[activeFrameIndex].Function.Chunk.Code[_frames[activeFrameIndex].Ip++];
                        byte argCount = _frames[activeFrameIndex].Function.Chunk.Code[_frames[activeFrameIndex].Ip++];

                        // Pop constructor parameters off the stack tracking allocations sequentially
                        Value[] constructorArgs = new Value[argCount];
                        for (int i = argCount - 1; i >= 0; i--) constructorArgs[i] = Pop();

                        // Discover the original class name string by searching the global symbol table mappings backward
                        string className = "UserStructClass";
                        foreach (var kvp in _globalSymbolTable)
                        {
                            if (kvp.Value == classSlotIndex)
                            {
                                className = kvp.Key;
                                break;
                            }
                        }
                        
                        // Build a fresh, isolated InstanceObject runtime heap container allocation
                        var emptyMethodsVTable = new Dictionary<string, Value>(); 
                        var instance = new InstanceObject(className, emptyMethodsVTable);
                        Value instanceValue = new Value(instance);

                        // AUTOMATED OPTIONAL CONSTRUCTOR INITIALIZATION PASS:
                        string constructorName = className + ".init";
                        if (_globalSymbolTable.TryGetValue(constructorName, out int constructorSlot))
                        {
                            Value constructorMethod = _globalSlots[constructorSlot];
                            if (constructorMethod.Type == Common.ValueType.Function)
                            {
                                // Spawn the sub-executor engine context wrapper safely
                                var subExecutor = new VM();

                                // Pre-seed your sub-executor's baseline value stack
                                // Push the newly created instance value straight into Stack Slot 0 ("this")
                                subExecutor.Push(instanceValue);

                                // Next, push the remainder of your constructor arguments right behind it
                                foreach (var arg in constructorArgs)
                                {
                                    subExecutor.Push(arg);
                                }

                                // Execute the initialization chunk statements natively.
                                // Because C# objects pass by reference, any field mutations made via 'this' 
                                // inside the sub-VM modify the 'instance' object in memory instantly
                                subExecutor.Interpret(constructorMethod.AsFunction().Chunk);
                            }
                        }

                        // Push the completely populated structural instance object directly back onto the evaluation stack
                        Push(instanceValue);
                        break;
                    }
                case OpCode.Return:
                    {
                        Value result = Pop(); // Grab the return value expression result
                        _frameCount--; // Drop the completed execution frame

                        if (_frameCount == 0)
                        {
                            // If we finished executing the top-level script wrapper, terminate cleanly
                            return InterpretResult.Ok;
                        }

                        // Restore the stack top pointer position, popping local frames and parameters completely
                        _stackTop = _frames[_frameCount].SlotsStart;
                        Push(result);
                        break;
                    }
            }
        }

        return InterpretResult.RuntimeError;
    }

    // --- VM STACK UTILITIES ---
    private void Push(Value value)
    {
        if (_stackTop >= _stack.Length)
        {
            int frameIndex = _frameCount - 1;
            int activeIp = frameIndex >= 0 ? _frames[frameIndex].Ip : 0;
            OpCode leakingOp = frameIndex >= 0 && activeIp > 0 ? (OpCode)_frames[frameIndex].Function.Chunk.Code[activeIp - 1] : OpCode.Return;

            LastRuntimeError = new Diagnostics.AError
            {
                Type = "Stack Overflow",
                Message = $"Virtual Machine Value Stack exceeded allocation window limits (Size 2048) at OpCode: '{leakingOp}'",
                Suggestion = $"The statement loop body is leaking expression values. Ensure your block parser emits an explicit Pop instruction at line breaks.",
                Line = activeIp,
                File = "main.a",
                Severity = Diagnostics.AErrorSeverity.Fatal
            };
            return;
        }
        _stack[_stackTop++] = value;
    }

    private Value Pop()
    {
        if (_stackTop == 0) throw new InvalidOperationException("VM stack underflow.");
        return _stack[--_stackTop];
    }

    private Value Peek(int distance)
    {
        return _stack[_stackTop - 1 - distance];
    }

    private void BinaryOp(Func<double, double, Value> operation)
    {
        if (Peek(0).Type == Common.ValueType.String || Peek(1).Type == Common.ValueType.String)
        {
            Value bVal = Pop();
            Value aVal = Pop();
            Push(new Value(aVal.ToString() + bVal.ToString()));
            return;
        }

        if (Peek(0).Type != Common.ValueType.Number || Peek(1).Type != Common.ValueType.Number)
        {
            LastRuntimeError = new Diagnostics.AError
            {
                Type = "Runtime Math",
                Message = "Operands must be valid numbers for arithmetic calculations.",
                Suggestion = $"Check your stack values. You attempted to process a calculation using types: {Peek(1).Type} and {Peek(0).Type}.",
                Line = _frames[_frameCount - 1].Function.Chunk.Code.Count > 0 ? _frames[_frameCount - 1].Ip : 1,
                File = "main.a",
                Severity = Diagnostics.AErrorSeverity.Fatal
            };
            return;
        }

        double b = Pop().AsNumber();
        double a = Pop().AsNumber();
        Push(operation(a, b));
    }

    private void CompareOp(Func<double, double, bool> operation)
    {
        if (Peek(0).Type != Common.ValueType.Number || Peek(1).Type != Common.ValueType.Number)
        {
            throw new InvalidOperationException("Runtime Error: Operands must be numbers.");
        }
        double b = Pop().AsNumber();
        double a = Pop().AsNumber();
        Push(new Value(operation(a, b)));
    }

    // --- BYTE NAVIGATION UTILITIES ---
    private CallFrame CurrentFrame => _frames[_frameCount - 1];
    private byte ReadByte() 
    {
        int activeFrameIndex = _frameCount - 1;
        return _frames[activeFrameIndex].Function.Chunk.Code[_frames[activeFrameIndex].Ip++];
    }
    private Value ReadConstant()
    {
        byte index = ReadByte();
        return CurrentFrame.Function.Chunk.Constants[index];
    }
    private ushort ReadShort()
    {
        int activeFrameIndex = _frameCount - 1;
        ushort offset = (ushort)(_frames[activeFrameIndex].Function.Chunk.Code[_frames[activeFrameIndex].Ip++] << 8);
        offset |= _frames[activeFrameIndex].Function.Chunk.Code[_frames[activeFrameIndex].Ip++];
        return offset;
    }

    // --- EXTRA HELPERS ---
    private static bool IsFalsey(Value value)
    {
        // Nil and explicitly false Booleans are treated as falsey conditions
        if (value.Type == Common.ValueType.Nil) return true;
        if (value.Type == Common.ValueType.Boolean) return !value.AsBoolean();
        return false; // Numbers and strings are considered truthy
    }
}