using System;
using System.Numerics;
using A.Core.Common;
using Raylib_cs;

namespace A.Core.StdLib;

public static class GameVisualModule
{
    public static Color ParseColor(Value arg)
    {
        if (arg.Type != Common.ValueType.String && arg.Type != Common.ValueType.Number)
            throw new ArgumentException("Runtime Error: 'Game.Visual.ParseColor' expects (hex string or hex literal).");

        uint hex = 0x000000;

        if (arg.Type == Common.ValueType.String)
        {
            string hexText = arg.AsString().Replace("0x", "").Replace("#", "");

            if (!uint.TryParse(hexText, System.Globalization.NumberStyles.HexNumber, null, out hex))
                hex = 0x000000;
        }
        else if (arg.Type == Common.ValueType.Number)
        {
            hex = Convert.ToUInt32(arg.AsNumber());
        }

        byte r = (byte)((hex >> 16) & 0xFF);
        byte g = (byte)((hex >> 8) & 0xFF);
        byte b = (byte)(hex & 0xFF);

        return new Color(r, g, b, (byte)255);
    }
    public static Value Init(Value[] args)
    {
        if (args.Length != 3 || args[0].Type != Common.ValueType.Number || args[1].Type != Common.ValueType.Number || args[2].Type != Common.ValueType.String)
            throw new ArgumentException("Runtime Error: 'Game.Visual.Init' expects (width, height, title).");

        int width = (int)args[0].AsNumber();
        int height = (int)args[1].AsNumber();
        string title = args[2].AsString();

        Raylib.InitWindow(width, height, title);
        Raylib.SetTargetFPS(60);

        Raylib.BeginDrawing();

        return new Value();
    }

    public static Value IsOpen(Value[] args)
    {
        bool open = !Raylib.WindowShouldClose();

        if (!open) Raylib.CloseWindow();
        return new Value(open);
    }

    public static Value Clear(Value[] args)
    {
        Color color = Color.Black;

        if (args.Length == 1)
        {
            color = ParseColor(args[0]);
        }

        Raylib.ClearBackground(color);
        return new Value();
    }

    public static Value DrawRect(Value[] args)
    {
        if (args.Length != 5) 
            throw new ArgumentException("Runtime Error: 'Game.Visual.DrawRect' expects (x, y, w, h, color).");

        int x = (int)args[0].AsNumber();
        int y = (int)args[1].AsNumber();
        int w = (int)args[2].AsNumber();
        int h = (int)args[3].AsNumber();

        Color color = ParseColor(args[4]);

        Raylib.DrawRectangle(x, y, w, h, color);
        return new Value();
    }

    public static Value DrawRectPro(Value[] args)
    {
        if (args.Length != 6)
            throw new ArgumentException("Runtime Error: 'Game.Visual.DrawRectPro' expects (x, y, w, h, rotation, color).");

        float x = (float)args[0].AsNumber();
        float y = (float)args[1].AsNumber();
        float w = (float)args[2].AsNumber();
        float h = (float)args[3].AsNumber();
        float rotation = (float)args[4].AsNumber();
        Color color = ParseColor(args[5]);

        Rectangle rec = new Rectangle(x, y, w, h);
        Vector2 origin = new Vector2(w /2f, h / 2f);

        Raylib.DrawRectanglePro(rec, origin, rotation, color);
        return new Value();
    }

    public static Value DrawText(Value[] args)
    {
        if (args.Length != 5)
            throw new ArgumentException("Runtime Error: 'Game.Visual.DrawText' expects (text, x, y, fontSize, color).");

        string text = args[0].AsString();
        int x = (int)args[1].AsNumber();
        int y = (int)args[2].AsNumber();
        int fontSize = (int)args[3].AsNumber();
        Color color = ParseColor(args[4]);

        Raylib.DrawText(text, x, y, fontSize, color);
        return new Value();
    }

    public static Value GetDeltaTime(Value[] args)
    {
        return new Value((double)Raylib.GetFrameTime());
    }

    public static Value CheckCollision(Value[] args)
    {
        if (args.Length != 8)
            throw new ArgumentException("Runtime Error: 'Game.Visual.CheckCollision' expects (x1, y1, w1, h1, x2, x2, w2, h2).");

                float x1 = (float)args[0].AsNumber();
        float y1 = (float)args[1].AsNumber();
        float w1 = (float)args[2].AsNumber();
        float h1 = (float)args[3].AsNumber();

        float x2 = (float)args[4].AsNumber();
        float y2 = (float)args[5].AsNumber();
        float w2 = (float)args[6].AsNumber();
        float h2 = (float)args[7].AsNumber();

        Rectangle rec1 = new Rectangle(x1, y1, w1, h1);
        Rectangle rec2 = new Rectangle(x2, y2, w2, h2);

        return new Value((bool)Raylib.CheckCollisionRecs(rec1, rec2));
    }

    public static Value IsKeyDown(Value[] args)
    {
        if (args.Length != 1 || args[0].Type != Common.ValueType.String)
            throw new ArgumentException("Runtime Error: 'Game.Visual.IsKeyDown' expects key name string.");

        string keyName = args[0].AsString().ToUpper();
        KeyboardKey key = keyName switch
        {
            "W" or "UP" => KeyboardKey.Up,
            "S" or "DOWN" => KeyboardKey.Down,
            "A" or "LEFT" => KeyboardKey.Left,
            "D" or "RIGHT" => KeyboardKey.Right,
            "ESC" or "ESCAPE" => KeyboardKey.Escape,
            "SPACE" => KeyboardKey.Space,
            _ => KeyboardKey.Null
        };
        return new Value((bool)Raylib.IsKeyDown(key));
    }

    public static Value Render(Value[] args)
    {
        Raylib.EndDrawing();
        Raylib.BeginDrawing();
        return new Value();
    }
}