using System;
using System.Collections.Generic;
using A.Core.Common;

namespace A.Core.StdLib;

public static class StdLibRegistry
{
    public static IReadOnlyDictionary<string, Func<Value[], Value>> Functions { get; } = new Dictionary<string, Func<Value[], Value>>
    {
        // Console Module
        { "Standard.Console.Print",      ConsoleModule.Print      },
        { "Standard.Console.Write",      ConsoleModule.Write      },
        { "Standard.Console.Input",      ConsoleModule.Input      },
        { "Standard.Console.Clear",      ConsoleModule.Clear      },
        { "Standard.Console.ReadLine",   ConsoleModule.ReadLine   },
        { "Standard.Console.SetColor",   ConsoleModule.SetColor   },
        { "Standard.Console.ResetColor", ConsoleModule.ResetColor },

        // Math Module
        { "Standard.Math.Abs",           MathModule.Abs   },
        { "Standard.Math.Sqrt",          MathModule.Sqrt  },
        { "Standard.Math.Pow",           MathModule.Pow   },
        { "Standard.Math.Round",         MathModule.Round },
        { "Standard.Math.Floor",         MathModule.Floor },
        { "Standard.Math.Ceil",          MathModule.Ceil  },

        // Collections Module
        { "Standard.Collections.Length", CollectionsModule.Length },

        // String Module
        { "Standard.String.Length",      StringModule.Length    },
        { "Standard.String.Sub",         StringModule.Sub       },
        { "Standard.String.ToNumber",    StringModule.ToNumber  },
        { "Standard.String.Split",       StringModule.Split     },
        { "Standard.String.Replace",     StringModule.Replace   },
        { "Standard.String.Trim",        StringModule.Trim      },
        { "Standard.String.ToLower",     StringModule.ToLower   },
        { "Standard.String.ToUpper",     StringModule.ToUpper   },

        // FileSystem Module
        { "Standard.FileSystem.Read",       FileModule.Read       },
        { "Standard.FileSystem.Write",      FileModule.Write      },
        { "Standard.FileSystem.Exists",     FileModule.Exists     },
        { "Standard.FileSystem.Append",     FileModule.Append     },
        { "Standard.FileSystem.Delete",     FileModule.Delete     },
        { "Standard.FileSystem.ReadLines",  FileModule.ReadLines  },
        { "Standard.FileSystem.WriteLines", FileModule.WriteLines },

        // Time Module
        { "Standard.Time.Now",           TimeModule.Now    },
        { "Standard.Time.Format",        TimeModule.Format },
        { "Standard.Console.Sleep",      TimeModule.Sleep  },

        // Json Module
        { "Standard.Json.Parse",         JsonModule.Parse     },
        { "Standard.Json.Stringify",     JsonModule.Stringify },

        // Game.Terminal Module
        { "Standard.Game.Terminal.Clear",    GameTerminalModule.Clear   },
        { "Standard.Game.Terminal.DrawBar",  GameTerminalModule.DrawBar },
        { "Standard.Game.Terminal.Menu",     GameTerminalModule.Menu    },
        { "Standard.Game.Terminal.MsgBox",   GameTerminalModule.MsgBox  },

        // Game.Visual Module
        { "Standard.Game.Visual.Init",              GameVisualModule.Init           },
        { "Standard.Game.Visual.GetDeltaTime",      GameVisualModule.GetDeltaTime   },
        { "Standard.Game.Visual.IsOpen",            GameVisualModule.IsOpen         },
        { "Standard.Game.Visual.Clear",             GameVisualModule.Clear          },
        { "Standard.Game.Visual.DrawText",          GameVisualModule.DrawText       },
        { "Standard.Game.Visual.DrawRect",          GameVisualModule.DrawRect       },
        { "Standard.Game.Visual.DrawRectPro",       GameVisualModule.DrawRectPro    },
        { "Standard.Game.Visual.CheckCollision",    GameVisualModule.CheckCollision },
        { "Standard.Game.Visual.IsKeyDown",         GameVisualModule.IsKeyDown      },
        { "Standard.Game.Visual.Render",            GameVisualModule.Render         }
    };

    // Dynamically exposes the total count of built-in standard library elements
    public static int BuiltInCount => Functions.Count;
}