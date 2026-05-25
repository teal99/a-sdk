using System.Text;
using A.Core.Common;
using A.Core.Compiler;
using A.Core.Diagnostics;
using A.Core.VM;

namespace A.Core.Parser;

public class Parser
{
    private string _currentNamespacePrefix = "";
    private CompilerState _compiler = new();
    private Dictionary<string, string> _imports = new();
    private readonly Dictionary<string, bool> _variableMutability = new();
    private readonly List<Token> _tokens;
    private int _current = 0;
    private Chunk _chunk = new();


    private static readonly Dictionary<string, byte> _globalSlotMapping = new();
    private byte _nextGlobalSlotIndex = 0;

    private byte ResolveGlobalSlotIndex(string name)
    {
        if (_globalSlotMapping.TryGetValue(name, out byte existingSlot))
        {
            return existingSlot;
        }

        string standardPath = "Standard." + name;
        if (_globalSlotMapping.TryGetValue(standardPath, out existingSlot))
        {
            return existingSlot;
        }

        byte newSlot = _nextGlobalSlotIndex++;
        _globalSlotMapping[name] = newSlot;
        _globalSlotMapping[standardPath] = newSlot;
        
        return newSlot;
    }

    static Parser()
    {
        byte slot = 0;
        foreach (string fullPath in StdLib.StdLibRegistry.Functions.Keys)
        {
            _globalSlotMapping[fullPath] = slot;

            int firstDot = fullPath.IndexOf('.');
            if (firstDot != -1)
            {
                string shortPath = fullPath[(firstDot + 1)..];
                _globalSlotMapping[shortPath] = slot;
            }
            slot++;
        }
    }

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    public Chunk Compile()
    {
        HadError = false;

        byte StandardLibraryBuiltInCount = (byte)StdLib.StdLibRegistry.Functions.Count;
        _nextGlobalSlotIndex = StandardLibraryBuiltInCount;

        if (Config.RelayTokens)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("\n--- CONFIG RELAY: LEXICAL TOKEN SEQUENCE STREAM ---");

            foreach (var token in _tokens)
            {
                Console.WriteLine($"Token Type: {token.Type,-18} | Lexeme: \"{token.Lexeme}\"");
            }

            Console.WriteLine("---------------------------------------------------\n");
            Console.ResetColor();
        }

        while (Match(TokenType.Newline));

        while (!IsAtEnd() && Peek().Type != TokenType.EOF)
        {
            ParseDeclaration();
            while (Match(TokenType.Newline)) { /* skip leading newlines */ }
        }

        _chunk.Write(OpCode.Constant);
        int nilIndex = _chunk.AddConstant(new Value());
        _chunk.Write((byte)nilIndex);
        _chunk.Write(OpCode.Return);

        if (Config.RelayOpcodes)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n--- CONFIG RELAY: BYTECODE DISASSEMBLY TRACE ---");
            Console.ResetColor();
            
            int offset = 0;
            while (offset < _chunk.Code.Count)
            {
                offset = Disassembler.DisassembleInstruction(_chunk, offset);
            }
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("------------------------------------------------\n");
            Console.ResetColor();
        }

        return _chunk;
    }

    private void ParseDeclaration()
    {
        if (Match(TokenType.Let)) ParseVariableDeclaration();
        else if (Match(TokenType.Use)) ParseImportDeclaration();
        else if (Match(TokenType.If)) ParseIfStatement();
        else if (Match(TokenType.While)) ParseWhileStatement();
        else if (Match(TokenType.Fn)) ParseFunctionDeclaration();
        else if (Match(TokenType.Return)) ParseReturnStatement();
        else if (Match(TokenType.Struct) || Match(TokenType.Class)) ParseClassDeclaration();
        else ParseStatement();
    }

    private void ParseVariableDeclaration()
    {
        bool isMutable = Match(TokenType.Mut);

        Token nameToken = Consume(TokenType.Identifier, "Expected variable name.");
        string prefixedGlobalName = _currentNamespacePrefix + nameToken.Lexeme;
        
        Consume(TokenType.Equal, "Expected '=' assignment after variable name.");
        ParseExpression(Precedence.None);

        if (_compiler.ScopeDepth > 0)
        {
            _compiler.Locals.Add(new Local(nameToken.Lexeme, _compiler.ScopeDepth, isMutable));
        }
        else
        {
            _variableMutability[prefixedGlobalName] = isMutable;
            
            byte slotIndex = ResolveGlobalSlotIndex(prefixedGlobalName);
            _chunk.Write(OpCode.DefineGlobal);
            _chunk.Write(slotIndex);
        }
    }

    private void CompileMemberAccess()
    {
        Token memberToken = Consume(TokenType.Identifier, "Expected property or method name after '.'.");
        int nameIndex = _chunk.AddConstant(new Value(memberToken.Lexeme));

        if (Match(TokenType.Equal))
        {
            ParseExpression(Precedence.None);

            _chunk.Write(OpCode.SetProperty);
            _chunk.Write((byte)nameIndex);
        }
        else
        {
            _chunk.Write(OpCode.GetProperty);
            _chunk.Write((byte)nameIndex);

            if (Match(TokenType.LeftParen))
            {
                byte argCount = 0;
                if (Peek().Type != TokenType.RightParen)
                {
                    do
                    {
                        ParseExpression(Precedence.None);
                        argCount++;
                    } while (Match(TokenType.Comma));
                }
                Consume(TokenType.RightParen, "Expected ')' after method arguments list.");

                _chunk.Write(OpCode.Call);
                _chunk.Write(argCount);
            }
        }
    }

    private void CompileVariableAccess()
    {
        string name = _previous().Lexeme;
        int localIndex = ResolveLocal(name);

        if (name == "this")
        {
            localIndex = 0;
        }

        string globalLookupName = name;
        bool isModuleImport = _imports.TryGetValue(name, out string? resolvedPath);

        if (localIndex == -1)
        {
            var globalLookupBuilder = new StringBuilder();
            if (isModuleImport)
            {
                globalLookupBuilder.Append(resolvedPath);
            }
            else
            {
                globalLookupBuilder.Append(name);
            }

            if (isModuleImport)
            {
                while (Peek().Type == TokenType.Dot)
                {
                    Advance();
                    Token prop = Consume(TokenType.Identifier, "Expected property or module name after '.'.");
                    globalLookupBuilder.Append('.');
                    globalLookupBuilder.Append(prop.Lexeme);
                }
            }
            globalLookupName = globalLookupBuilder.ToString();
        }

        if (!isModuleImport && Peek().Type == TokenType.Dot)
        {
            if (localIndex != -1)
            {
                _chunk.Write(OpCode.GetLocal);
                _chunk.Write((byte)localIndex);
            }
            else
            {
                byte slotIndex = ResolveGlobalSlotIndex(globalLookupName);
                _chunk.Write(OpCode.GetGlobal);
                _chunk.Write(slotIndex);
            }

            while (Match(TokenType.Dot))
            {
                CompileMemberAccess();
            }
            return;
        }

        if (Match(TokenType.Equal))
        {
            if (localIndex != -1)
            {
                if (!_compiler.Locals[localIndex].IsMutable)
                {
                    ErrorAt(_previous(), $"Cannot reassign immutable local variable '{name}'.", "Declare the variable using 'let mut' if you intend to modify its value.");
                    return;
                }
            }
            else
            {
                if (_variableMutability.TryGetValue(globalLookupName, out bool isGloballyMutable) && !isGloballyMutable)
                {
                    ErrorAt(_previous(), $"Cannot reassign immutable global variable '{globalLookupName}'.", "Declare the variable using 'let mut' if you intend to modify its value.");
                    return;
                }
            }

            ParseExpression(Precedence.None);
            if (localIndex != -1)
            {
                _chunk.Write(OpCode.SetLocal);
                _chunk.Write((byte)localIndex);
            }
            else
            {
                byte slotIndex = ResolveGlobalSlotIndex(globalLookupName);
                _chunk.Write(OpCode.AssignGlobal);
                _chunk.Write(slotIndex); 
            }
        }
        else
        {
            if (localIndex != -1)
            {
                _chunk.Write(OpCode.GetLocal);
                _chunk.Write((byte)localIndex);
            }
            else
            {
                byte slotIndex = ResolveGlobalSlotIndex(globalLookupName);
                _chunk.Write(OpCode.GetGlobal);
                _chunk.Write(slotIndex); 
            }
        }
    }

    private void ParseFunctionDeclaration()
    {
        Token nameToken = Consume(TokenType.Identifier, "Expected function name after 'fn'.");
        ParseFunctionDeclarationInternal(nameToken);
    }

    private void ParseFunctionDeclarationInternal(Token nameToken)
    {
        FunctionObject function = new FunctionObject(nameToken.Lexeme);

        Chunk mainChunkBackup = _chunk;
        _chunk = function.Chunk;

        var parentLocalsBackup = new List<Local>(_compiler.Locals);
        int parentScopeDepthBackup = _compiler.ScopeDepth;

        _compiler.Locals.Clear();
        _compiler.ScopeDepth = 1;

        _compiler.Locals.Add(new Local("", _compiler.ScopeDepth));

        Consume(TokenType.LeftParen, "Expected '(' after function identifier.");
        if (Peek().Type != TokenType.RightParen)
        {
            do
            {
                function.Arity++;
                if (function.Arity > 255) throw new Exception("Functions cannot accept more than 255 parameters.");
                
                Token paramToken = Consume(TokenType.Identifier, "Expected parameter name.");
                _compiler.Locals.Add(new Local(paramToken.Lexeme, _compiler.ScopeDepth));
            } while (Match(TokenType.Comma));
        }
        Consume(TokenType.RightParen, "Expected ')' after parameter definitions.");

        Consume(TokenType.LeftBrace, "Expected '{' before function body.");
        while (Match(TokenType.Newline)) { /* skip leading newlines inside function body */ }

        while (!IsAtEnd() && Peek().Type != TokenType.RightBrace)
        {
            ParseDeclaration();
            while (Match(TokenType.Newline)) { /* skip trailing newlines between declarations */ }
        }
        Consume(TokenType.RightBrace, "Expected '}' to terminate function.");

        _chunk.Write(OpCode.Constant);
        int nilIndex = _chunk.AddConstant(new Value());
        _chunk.Write((byte)nilIndex);
        _chunk.Write(OpCode.Return);

        _compiler.Locals.Clear();
        _compiler.Locals.AddRange(parentLocalsBackup);
        _compiler.ScopeDepth = parentScopeDepthBackup;

        _chunk = mainChunkBackup;

        string prefixedFuncName = _currentNamespacePrefix + nameToken.Lexeme;
        
        int funcIndex = _chunk.AddConstant(new Value(function));
        
        byte slotIndex = ResolveGlobalSlotIndex(prefixedFuncName);
        
        _chunk.Write(OpCode.Constant);
        _chunk.Write((byte)funcIndex);
        
        _chunk.Write(OpCode.DefineGlobal);
        _chunk.Write(slotIndex);
    }

    private void ParseClassDeclaration()
    {
        Token classToken = Consume(TokenType.Identifier, "Expected class or struct name descriptor definition.");
        string className = classToken.Lexeme;

        Consume(TokenType.LeftBrace, "Expected '{' to open class declaration body structural container.");
        while (Match(TokenType.Newline)) { /* skip leading newlines */ }

        while (!IsAtEnd() && Peek().Type != TokenType.RightBrace)
        {
            if (Match(TokenType.Let))
            {
                Consume(TokenType.Identifier, "Expected local instance field property name.");
                while (Match(TokenType.Newline)) { /* skip leading newlines */ }
            }
            else if (Match(TokenType.Fn))
            {
                Token methodToken = Consume(TokenType.Identifier, "Expected member method function token descriptor.");
                
                string oldPrefix = _currentNamespacePrefix;
                _currentNamespacePrefix = className + ".";

                ParseFunctionDeclarationInternal(methodToken);

                _currentNamespacePrefix = oldPrefix;
            }
            while (Match(TokenType.Newline)) { /* skip leading newlines */ }
        }
        Consume(TokenType.RightBrace, "Expected '}' to close class declaration blueprint block.");

        byte classSlotIndex = ResolveGlobalSlotIndex(className);
        
        _chunk.Write(OpCode.DefineClass);
        _chunk.Write(classSlotIndex);
    }

    private void CompileInstantiation()
    {
        Token classToken = Consume(TokenType.Identifier, "Expected structural class name template following 'new' instantiation operator.");
        
        byte classSlotIndex = ResolveGlobalSlotIndex(classToken.Lexeme);

        Consume(TokenType.LeftParen, "Expected '(' parameters block initialization open brace marker.");
        byte argCount = 0;
        if (Peek().Type != TokenType.RightParen)
        {
            do
            {
                ParseExpression(Precedence.None);
                argCount++;
            } while (Match(TokenType.Comma));
        }
        Consume(TokenType.RightParen, "Expected ')' to close initializer arguments stream sequence.");

        _chunk.Write(OpCode.Instantiate);
        _chunk.Write(classSlotIndex);
        _chunk.Write(argCount); 
    }

    private void ParseReturnStatement()
    {
        if (Peek().Type == TokenType.Newline || Peek().Type == TokenType.EOF || Peek().Type == TokenType.RightBrace)
        {
            _chunk.Write(OpCode.Constant); 
            _chunk.Write(OpCode.Return);
        }
        else
        {
            ParseExpression(Precedence.None);
            _chunk.Write(OpCode.Return);
        }
    }
    
    private void ParseImportDeclaration()
    {
        Token rootToken = Consume(TokenType.Identifier, "Expected module path after 'use'.");
        var fullPathBuilder = new StringBuilder(rootToken.Lexeme);

        while (Match(TokenType.Dot))
        {
            Token segment = Consume(TokenType.Identifier, "Expected module segment name after '.'.");
            fullPathBuilder.Append('.').Append(segment.Lexeme);
        }
        string fullPath = fullPathBuilder.ToString();

        int lastDot = fullPath.LastIndexOf('.');
        string alias = lastDot == -1 ? fullPath : fullPath[(lastDot + 1)..];

        if (Match(TokenType.As))
        {
            Token customAliasToken = Consume(TokenType.Identifier, "Expected custom alias identifier name after 'as'.");
            alias = customAliasToken.Lexeme;
        }

        _imports[alias] = fullPath;

        // --- NEW EXTERNAL FILE LOADING HOOK ---
        if (!fullPath.StartsWith("Standard"))
        {
            string localFilePath = fullPath.Replace('.', Path.DirectorySeparatorChar) + ".a";

            if (File.Exists(localFilePath))
            {
                string dependencySource = File.ReadAllText(localFilePath);
                
                Lexer.Lexer moduleLexer = new(dependencySource);
                var moduleTokens = moduleLexer.ScanTokens();

                string modulePrefix = fullPath + "."; 

                var moduleParser = new Parser(moduleTokens)
                {
                    _imports = _imports,
                    _chunk = _chunk,
                    _compiler = _compiler,
                    _currentNamespacePrefix = modulePrefix
                };
                
                moduleParser.CompileModuleContents();
            }
            else
            {
                ErrorAt(rootToken, $"Could not resolve local file module dependency source at location: '{localFilePath}'");
            }
        }
    }

    private void ParseStatement()
    {
        if (Match(TokenType.LeftBrace))
        {
            CompileBlockScope();
        }
        else
        {
            ParseExpression(Precedence.None);
            _chunk.Write(OpCode.Pop);
        }
    }

    private void ParseIfStatement()
    {
        List<int> exitJumps = new();

        ParseExpression(Precedence.None);
        int jumpPlaceholder = EmitJump(OpCode.JumpIfFalse);

        Consume(TokenType.LeftBrace, "Expected '{' before conditional body code.");
        while (Match(TokenType.Newline)) { /* skip leading newlines */ }

        while (!IsAtEnd() && Peek().Type != TokenType.RightBrace)
        {
            ParseDeclaration();
            while (Match(TokenType.Newline)) { /* skip leading newlines */ }
        }
        Consume(TokenType.RightBrace, "Expected '}' after conditional body code.");

        if (Peek().Type == TokenType.Else)
        {
            int exitJump = EmitJump(OpCode.Jump);
            exitJumps.Add(exitJump);
        }

        PatchJump(jumpPlaceholder);

        while (Match(TokenType.Else))
        {
            if (Match(TokenType.If))
            {
                ParseExpression(Precedence.None);
                int elseIfFalseJump = EmitJump(OpCode.JumpIfFalse);

                Consume(TokenType.LeftBrace, "Expected '{' before conditional body code.");
                while (Match(TokenType.Newline)) { /* skip leading newlines */ }

                while (!IsAtEnd() && Peek().Type != TokenType.RightBrace)
                {
                    ParseDeclaration();
                    while (Match(TokenType.Newline)) { /* skip leading newlines */ }
                }
                Consume(TokenType.RightBrace, "Expected '}' after conditional body code.");

                if (Peek().Type == TokenType.Else)
                {
                    int exitJump = EmitJump(OpCode.Jump);
                    exitJumps.Add(exitJump);
                }

                PatchJump(elseIfFalseJump);
            }
            else
            {
                Consume(TokenType.LeftBrace, "Expected '{' before else body code.");
                while (Match(TokenType.Newline)) { /* skip leading newlines */ }

                while (!IsAtEnd() && Peek().Type != TokenType.RightBrace)
                {
                    ParseDeclaration();
                    while (Match(TokenType.Newline)) { /* skip leading newlines */ }
                }
                Consume(TokenType.RightBrace, "Expected '}' after else body code.");
                break;
            }
        }

        foreach (int jumpLocation in exitJumps)
        {
            PatchJump(jumpLocation);
        }
    }

    private void ParseWhileStatement()
    {
        int loopStartOffset = _chunk.Code.Count;

        ParseExpression(Precedence.None);

        int exitJumpPlaceholderOffset = EmitJump(OpCode.JumpIfFalse);

        Consume(TokenType.LeftBrace, "Expected '{' before while loop body.");
        while (Match(TokenType.Newline)) { /* skip leading newlines */ }

        while (!IsAtEnd() && Peek().Type != TokenType.RightBrace)
        {
            ParseDeclaration();
            while (Match(TokenType.Newline)) { /* skip leading newlines */ }
        }
        Consume(TokenType.RightBrace, "Expected '}' after while loop body.");

        EmitLoopJump(loopStartOffset);
        PatchJump(exitJumpPlaceholderOffset);
    }

    private void ParseExpression(Precedence precedence)
    {
        Token token = Advance();
        Action? prefixRule = GetPrefixRule(token.Type);
        if (prefixRule == null)
        {
            ErrorAt(token, $"Expected an expression but found invalid token '{token.Lexeme}'.");
            Synchronize();
            return;
        }

        prefixRule();

        while (!IsAtEnd()
            && Peek().Type != TokenType.Newline
            && Peek().Type != TokenType.EOF
            && Peek().Type != TokenType.Comma
            && Peek().Type != TokenType.RightBrace
            && Peek().Type != TokenType.RightBracket
        )
        {
            TokenType next = Peek().Type;

            Action? infixRule = GetInfixRule(next);
            if (infixRule == null) break;

            if (precedence >= GetPrecedence(next))
                break;

            Token _ = Advance();
            infixRule.Invoke();
        }
    }

    // --- PREFIX COMPILATION FUNCTIONS ---
    
    private void CompileNumber()
    {
        double value = double.Parse(_previous().Lexeme);
        int constantIndex = _chunk.AddConstant(new Value(value));

        _chunk.Write(OpCode.Constant);
        _chunk.Write((byte)constantIndex);
    }

    private void CompileString()
    {
        string text = _previous().Lexeme;
        int constantIndex = _chunk.AddConstant(new Value(text));

        _chunk.Write(OpCode.Constant);
        _chunk.Write((byte)constantIndex);
    }

    private void CompileTrue() => _chunk.Write(OpCode.True);
    private void CompileFalse() => _chunk.Write(OpCode.False);

    private void CompileGrouping()
    {
        ParseExpression(Precedence.None);
        Consume(TokenType.RightParen, "Expected ')' after grouped expression.");
    }

    private void CompileUnary()
    {
        TokenType operatorType = _previous().Type;
        ParseExpression(Precedence.Unary);

        if (operatorType == TokenType.Minus)
        {
            _chunk.Write(OpCode.Negate);
        }
    }

    private void CompileDictionaryLiteral()
    {
        byte pairCount = 0;
        
        while (Match(TokenType.Newline)) { /* skip leading newlines */ }

        if (Peek().Type != TokenType.RightBrace)
        {
            do
            {
                while (Match(TokenType.Newline)) { /* skip leading newlines */ }

                if (Peek().Type == TokenType.RightBrace) break;

                Token keyToken = Advance();
                if (keyToken.Type != TokenType.Identifier && keyToken.Type != TokenType.String)
                {
                    throw new Exception($"Invalid dictionary key: {keyToken.Type}");
                }
                
                _chunk.Write(OpCode.Constant);
                int keyConstIndex = _chunk.AddConstant(new Value(keyToken.Lexeme));
                _chunk.Write((byte)keyConstIndex);

                Consume(TokenType.Colon, "Expected ':' field separator after dictionary key.");

                while (Match(TokenType.Newline)) { /* skip leading newlines */ }

                if (Peek().Type == TokenType.RightBrace)
                {
                    ErrorAt(Peek(), "Expected an expression value after directionary field colon");
                    return;
                }

                ParseExpression(Precedence.None);

                pairCount++;
                
                if (pairCount > 255) 
                    throw new Exception("Dictionaries cannot store more than 255 inline literal fields.");
                
                while (Match(TokenType.Newline)) { /* skip newlines */ }

            } while (Match(TokenType.Comma));
        }
        
        while (Match(TokenType.Newline)) { /* skip trailing newlines */ }
        
        Consume(TokenType.RightBrace, "Expected '}' to terminate dictionary container initialization.");
        
        _chunk.Write(OpCode.BuildMap);
        _chunk.Write(pairCount);
    }

    private void CompileArrayLiteral()
    {
        byte elementCount = 0;
        
        while (Match(TokenType.Newline)) { /* skip leading newlines */ }

        if (Peek().Type != TokenType.RightBracket)
        {
            do
            {
                while (Match(TokenType.Newline)) { /* skip leading newlines */ }

                ParseExpression(Precedence.None);
                elementCount++;
                
                if (elementCount > 255) 
                    throw new Exception("Arrays cannot store more than 255 inline literals.");
                
                while (Match(TokenType.Newline)) { /* skip leading newlines */ }

            } while (Match(TokenType.Comma));
        }
        
        while (Match(TokenType.Newline)) { /* skip leading newlines */ }
        
        Consume(TokenType.RightBracket, "Expected ']' to terminate array container initialization.");
        
        _chunk.Write(OpCode.BuildArray);
        _chunk.Write(elementCount);
    }

    private void CompileSubscriptIndex()
    {
        ParseExpression(Precedence.None);
        Consume(TokenType.RightBracket, "Expected ']' after array index subscription reference.");

        if (Match(TokenType.Equal))
        {
            ParseExpression(Precedence.None);
            _chunk.Write(OpCode.SetIndex);
        }
        else
        {
            _chunk.Write(OpCode.GetIndex);
        }
    }

    // --- INFIX COMPILATION FUNCTIONS ---

    private void CompileCall()
    {
        byte argCount = 0;
        if (Peek().Type != TokenType.RightParen)
        {
            do
            {
                ParseExpression(Precedence.None);
                argCount++;
                if (argCount > 255) throw new Exception("Cannot have more than 255 arguments.");
            } while (Match(TokenType.Comma));
        }
        
        Consume(TokenType.RightParen, "Expected ')' after function arguments.");
        _chunk.Write(OpCode.Call);
        _chunk.Write(argCount);
    }
    
    private void CompileBinary()
    {
        TokenType operatorType = _previous().Type;
        Precedence precedence = GetPrecedence(operatorType);

        ParseExpression(precedence);

        switch (operatorType)
        {
            case TokenType.Plus:            _chunk.Write(OpCode.Add); break;
            case TokenType.Minus:           _chunk.Write(OpCode.Subtract); break;
            case TokenType.Star:            _chunk.Write(OpCode.Multiply); break;
            case TokenType.Slash:           _chunk.Write(OpCode.Divide); break;
            case TokenType.Modulo:          _chunk.Write(OpCode.Modulo); break;
            case TokenType.Less:            _chunk.Write(OpCode.Lesser); break;
            case TokenType.LessEqual:       _chunk.Write(OpCode.LesserEqual); break;
            case TokenType.Greater:         _chunk.Write(OpCode.Greater); break;
            case TokenType.GreaterEqual:    _chunk.Write(OpCode.GreaterEqual); break;
            case TokenType.EqualEqual:      _chunk.Write(OpCode.Equal); break;
            case TokenType.UnEqual:         _chunk.Write(OpCode.UnEqual); break;
        }
    }

    private void CompileNot()
    {
        ParseExpression(Precedence.Unary);
        _chunk.Write(OpCode.Not);
    }

    private void CompileAnd()
    {
        int endJump = EmitJump(OpCode.JumpIfFalse);

        ParseExpression(Precedence.And);

        PatchJump(endJump);
    }

    private void CompileOr()
    {
        int trueJump = EmitJump(OpCode.JumpIfFalse);
        int endJump = EmitJump(OpCode.Jump);

        PatchJump(trueJump);
        ParseExpression(Precedence.Or);
        PatchJump(endJump);
    }

    // --- LOOKUP CONFIGURATION TABLES ---

    private Action? GetPrefixRule(TokenType type) => type switch
    {
        TokenType.Number        => CompileNumber,
        TokenType.String        => CompileString,
        TokenType.Minus         => CompileUnary,
        TokenType.LeftParen     => CompileGrouping,
        TokenType.Identifier    => CompileVariableAccess,
        TokenType.True          => CompileTrue,
        TokenType.False         => CompileFalse,
        TokenType.Bang          => CompileNot,
        TokenType.LeftBracket   => CompileArrayLiteral,
        TokenType.LeftBrace     => CompileDictionaryLiteral,
        TokenType.New           => CompileInstantiation,
        _                       => null
    };

    private Action? GetInfixRule(TokenType type) => type switch
    {
        TokenType.Plus          => CompileBinary,
        TokenType.Minus         => CompileBinary,
        TokenType.Star          => CompileBinary,
        TokenType.Slash         => CompileBinary,
        TokenType.Modulo        => CompileBinary,
        TokenType.LeftParen     => CompileCall,
        TokenType.Less          => CompileBinary,
        TokenType.Greater       => CompileBinary,
        TokenType.LessEqual     => CompileBinary,
        TokenType.GreaterEqual  => CompileBinary,
        TokenType.EqualEqual    => CompileBinary,
        TokenType.UnEqual       => CompileBinary,
        TokenType.And           => CompileAnd,
        TokenType.Or            => CompileOr,
        TokenType.LeftBracket   => CompileSubscriptIndex,
        TokenType.Dot           => CompileMemberAccess,
        _                       => null
    };

    private static Precedence GetPrecedence(TokenType type) => type switch
    {
        TokenType.Plus          => Precedence.Sum,
        TokenType.Minus         => Precedence.Sum,
        TokenType.Star          => Precedence.Product,
        TokenType.Slash         => Precedence.Product,
        TokenType.Modulo        => Precedence.Product,
        TokenType.LeftParen     => Precedence.Call,
        TokenType.LeftBracket   => Precedence.Call,
        TokenType.Dot           => Precedence.Call,
        TokenType.Less          => Precedence.Comparison,
        TokenType.Greater       => Precedence.Comparison,
        TokenType.LessEqual     => Precedence.Comparison,
        TokenType.GreaterEqual  => Precedence.Comparison,
        TokenType.EqualEqual    => Precedence.Comparison,
        TokenType.UnEqual       => Precedence.Comparison,
        TokenType.And           => Precedence.And,
        TokenType.Or            => Precedence.Or,
        _                       => Precedence.None
    };

    // --- NAVIGATION HELPERS ---
    private bool IsAtEnd() => _current >= _tokens.Count;
    private Token Peek() => IsAtEnd() ? _tokens[^1] : _tokens[_current];
    private Token _previous() => _tokens[_current - 1];
    private Token Consume(TokenType type, string errorMessage)
    {
        if (Peek().Type == type) return Advance();
        
        string hint = type switch
        {
            TokenType.LeftBrace  => "Insert an opening brace '{' right before this statement block.",
            TokenType.RightBrace => "Ensure your matching closing brace '}' completes this layout scope.",
            TokenType.RightParen => "Check for unclosed parameters; append a matching closing parenthesis ')'.",
            _                    => $"Ensure this sequence provides a valid '{type}' operation token target."
        };

        ErrorAt(Peek(), errorMessage, hint);
        Synchronize();
        return Peek();
    }

    private Token Advance()
    {
        if (!IsAtEnd()) _current++;
        return _previous();
    }

    private bool Match(TokenType type)
    {
        if (IsAtEnd() || Peek().Type != type) return false;
        _current++;
        return true;
    }

    // --- EXTRA HELPERS ---

    // Track if compilation encountered errors to block execution of broken bytecode
    public bool HadError { get; private set; } = false;
    private readonly string _currentFileName = "main.a"; // 'main.a' always for now

    private void ErrorAt(Token token, string message, string? suggestion = null)
    {
        HadError = true;

        var error = new Diagnostics.AError
        {
            Type = token.Type == TokenType.EOF ? "EOF" : "Syntax",
            Message = message,
            Suggestion = suggestion,
            Line = token.Line,
            File = _currentFileName,
            Severity = Diagnostics.AErrorSeverity.Error,
            ExtraInfo = token.Type != TokenType.EOF && token.Type != TokenType.Newline 
                ? $"Near token lexeme segment '{token.Lexeme}'" 
                : null
        };

        Diagnostics.DiagnosticPrinter.Print(error);
    }

    private void Synchronize()
    {
        Advance();

        while (!IsAtEnd())
        {
            // Stop synchronizing if it hits a statement boundary line break
            if (_previous().Type == TokenType.Newline) return;

            switch (Peek().Type)
            {
                case TokenType.Let:
                case TokenType.Fn:
                case TokenType.If:
                case TokenType.While:
                case TokenType.Return:
                case TokenType.Use:
                    return;
            }

            Advance();
        }
    }

    private int ResolveLocal(string name)
    {
        for (int i = _compiler.Locals.Count - 1; i >= 0; i--)
        {
            if (_compiler.Locals[i].Name == name)
            {
                return i;
            }
        }
        return -1;
    }

    private void CompileBlockScope()
    {
        _compiler.EnterScope();
        
        while (!IsAtEnd() && Peek().Type != TokenType.RightBrace)
        {
            ParseDeclaration();
            while (Match(TokenType.Newline)) { /* skip leading newlines */ }
        }
        
        Consume(TokenType.RightBrace, "Expected '}' to close block scope boundaries.");
        
        while (_compiler.Locals.Count > 0 && _compiler.Locals[^1].Depth == _compiler.ScopeDepth)
        {
            _chunk.Write(OpCode.Pop);
            _compiler.Locals.RemoveAt(_compiler.Locals.Count - 1);
        }

        _compiler.ExitScope();
    }

    private int EmitJump(OpCode instruction)
    {
        _chunk.Write(instruction);
        _chunk.Write(0xff);
        _chunk.Write(0xff);
        return _chunk.Code.Count - 2;
    }

    private void PatchJump(int offset)
    {
        int jumpDistance = _chunk.Code.Count - offset - 2;

        if (jumpDistance > ushort.MaxValue)
        {
            throw new Exception("Too much code compiled inside conditional block scope.");
        }

        _chunk.Code[offset]     = (byte)((jumpDistance >> 8) & 0xff);
        _chunk.Code[offset + 1] = (byte)(jumpDistance & 0xff);
    }

    private void EmitLoopJump(int loopStartOffset)
    {
        _chunk.Write(OpCode.Loop);
        int offset = _chunk.Code.Count + 2 - loopStartOffset;
        if (offset > ushort.MaxValue)
        {
            throw new Exception("Compilation Error: Loop body scope block size exceeds maximum bytecode branch jump limit.");
        }
        _chunk.Write((byte)((offset >> 8) & 0xff));
        _chunk.Write((byte)(offset & 0xff));
    }

    public void CompileModuleContents()
    {
        while (!IsAtEnd() && Peek().Type != TokenType.EOF)
        {
            ParseDeclaration();
            while (Match(TokenType.Newline)) { /* skip leading newlines */ }
        }
    }
}