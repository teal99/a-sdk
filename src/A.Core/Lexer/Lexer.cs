using System.Collections.Generic;
using A.Core.Common;

namespace A.Core.Lexer;

public class Lexer
{
    private readonly string _source;
    private readonly List<Token> _tokens = new();
    private int _start = 0;
    private int _current = 0;
    private int _line = 1;
    private int _column = 1;

    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        { "let",    TokenType.Let       },
        { "mut",    TokenType.Mut       },
        { "fn",     TokenType.Fn        },
        { "if",     TokenType.If        },
        { "else",   TokenType.Else      },
        { "while",  TokenType.While     },
        { "return", TokenType.Return    },
        { "use",    TokenType.Use       },
        { "true",   TokenType.True      },
        { "false",  TokenType.False     },
        { "and",    TokenType.And       },
        { "or",     TokenType.Or        },
        { "as",     TokenType.As        },
        { "new",    TokenType.New       },
        { "class",  TokenType.Class     },
        { "struct", TokenType.Struct    }
    };

    public Lexer(string source)
    {
        _source = source;
    }

    public List<Token> ScanTokens()
    {
        while (!IsAtEnd())
        {
            _start = _current;
            ScanToken();
        }

        _tokens.Add(new Token(TokenType.EOF, "", _line, _column));
        return _tokens;
    }

    private void ScanToken()
    {
        char c = Advance();
        switch (c)
        {
            // Single-character symbols
            case '(': AddToken(TokenType.LeftParen); break;
            case ')': AddToken(TokenType.RightParen); break;
            case '{': AddToken(TokenType.LeftBrace); break;
            case '}': AddToken(TokenType.RightBrace); break;
            case '[': AddToken(TokenType.LeftBracket); break;
            case ']': AddToken(TokenType.RightBracket); break;
            case '+': AddToken(TokenType.Plus); break;
            case '-': AddToken(TokenType.Minus); break;
            case '*': AddToken(TokenType.Star); break;
            case '%': AddToken(TokenType.Modulo); break;
            case '.': AddToken(TokenType.Dot); break;
            case ',': AddToken(TokenType.Comma); break;
            case ':': AddToken(TokenType.Colon); break;

            // Comments
            case '/':
                if (Match('/'))
                {
                    while (Peek() != '\n' && !IsAtEnd())
                    {
                        Advance();
                    }
                }
                else
                {
                    AddToken(TokenType.Slash);
                }
                break;
            
            // Operators & Multi-character tokens
            case '=':
                if (Match('=')) AddToken(TokenType.EqualEqual);
                else if (Match('>')) AddToken(TokenType.Arrow);
                else AddToken(TokenType.Equal);
                break;
            case '!': 
                if (Match('=')) AddToken(TokenType.UnEqual);
                else AddToken(TokenType.Bang);
                break;
            case '<':
                AddToken(Match('=') ? TokenType.LessEqual : TokenType.Less);
                break;
            case '>':
                AddToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater);
                break;
            case '&':
                if (Match('&')) AddToken(TokenType.And);
                else throw new Exception($"Invalid character '&': expected '&' after '&' (&&)");
                break;
            case '|':
                if (Match('|')) AddToken(TokenType.Or);
                else throw new Exception($"Invalid character '|': expected '|' after '|' (||)");
                break;

            // Inline Whitespace (Ignored)
            case ' ':
            case '\r':
            case '\t':
                break;

            // Newlines
            case '\n':
                AddToken(TokenType.Newline);
                _line++;
                _column = 1;
                break;

            // Strings
            case '"': ScanString(); break;

            default:
                if (IsDigit(c))
                {
                    ScanNumber();
                }
                else if (IsAlpha(c))
                {
                    ScanIdentifier();
                }
                else
                {
                    AddToken(TokenType.Error, $"Unexpected character: {c}");
                }
                break;
        }
    }

    private void ScanString()
    {
        while (Peek() != '"' && !IsAtEnd())
        {
            if (Peek() == '\n')
            {
                _line++;
            }
            Advance();
        }

        if (IsAtEnd())
        {
            AddToken(TokenType.Error, "Unterminated string literal.");
            return;
        }

        Advance();

        string value = _source[(_start + 1)..(_current - 1)];
        AddToken(TokenType.String, value);
    }

    private void ScanNumber()
    {
        while (IsDigit(Peek())) Advance();

        if (Peek() == '.' && IsDigit(PeekNext()))
        {
            Advance();
            while (IsDigit(Peek())) Advance();
        }

        AddToken(TokenType.Number);
    }

    private void ScanIdentifier()
    {
        while (IsAlphaNumeric(Peek())) Advance();

        string text = _source[_start.._current];
        if (!Keywords.TryGetValue(text, out TokenType type))
        {
            type = TokenType.Identifier;
        }
        
        AddToken(type);
    }

    // Helper Navigation Utilities
    private bool IsAtEnd() => _current >= _source.Length;
    private char Advance()
    {
        char c = _source[_current++];
        _column++;
        return c;
    }
    private char Peek() => IsAtEnd() ? '\0' : _source[_current];
    private char PeekNext() => _current + 1 >= _source.Length ? '\0' : _source[_current + 1];

    private bool Match(char expected)
    {
        if (IsAtEnd() || _source[_current] != expected) return false;
        _current++;
        return true;
    }

    private static bool IsDigit(char c) => c >= '0' && c <= '9';
    private static bool IsAlpha(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';
    private static bool IsAlphaNumeric(char c) => IsAlpha(c) || IsDigit(c);

    private void AddToken(TokenType type) => AddToken(type, _source[_start.._current]);
    private void AddToken(TokenType type, string lexeme) 
    {
        int tokenStartColumn = _column - lexeme.Length; 
        _tokens.Add(new Token(type, lexeme, _line, tokenStartColumn));
    }
}