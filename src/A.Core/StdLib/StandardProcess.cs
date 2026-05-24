using System;
using System.Diagnostics;
using A.Core.Common;

namespace A.Core.Runtime.Standard;

public static class ProcessModule
{
    public static Value Run(Value[] args)
    {
        if (args.Length != 1 || args[0].Type != Common.ValueType.String)
        {
            throw new ArgumentException("Runtime Error: 'Process.Run' expects exactly 1 string argument representing the system shell command.");
        }

        string command = args[0].AsString();
        
        try
        {
            // Configure a hidden cross-platform shell process wrapper
            using Process proc = new Process();
            
            // For Windows use cmd.exe, for Mac/Linux use /bin/sh or bash natively
            if (OperatingSystem.IsWindows())
            {
                proc.StartInfo.FileName = "cmd.exe";
                proc.StartInfo.Arguments = $"/c {command}";
            }
            else
            {
                proc.StartInfo.FileName = "/bin/sh";
                proc.StartInfo.Arguments = $"-c \"{command}\"";
            }

            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;

            proc.Start();
            
            // Read both standard outputs and terminal error matrices
            string output = proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            if (!string.IsNullOrEmpty(error) && proc.ExitCode != 0)
            {
                return new Value($"[Shell Error - Exit Code {proc.ExitCode}]: {error.Trim()}");
            }

            return new Value(output.Trim()); // Return the terminal output text string cleanly
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Runtime Error: Failed to execute terminal system process command: {ex.Message}");
        }
    }
}