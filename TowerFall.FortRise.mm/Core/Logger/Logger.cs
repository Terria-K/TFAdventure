using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Monocle;

namespace FortRise;

public static class Logger 
{
    public enum LogLevel { Info, Warning, Error, Debug, Verbose, Assert}
    private static StringBuilder builder = new();
    private static IConsole consoleWindow;

    public static LogLevel Verbosity = LogLevel.Info;

    public static void AttachConsole(IConsole window) 
    {
        consoleWindow = window;
        consoleWindow.Attach();
    }

    public static void DetachConsole() 
    {
        consoleWindow?.Detach();
    }

    private static void LogInternal(LogLevel level, string message, int lineNumber) 
    {
        if (Verbosity < level)
            return;

        var logName = level switch 
        {
            LogLevel.Debug => "[DEBUG]",
            LogLevel.Assert => "[ASSERT]",
            LogLevel.Warning => "[WARNING]",
            LogLevel.Error => "[ERROR]",
            LogLevel.Verbose => "[VERBOSE]",
            _ => "[INFO]"
        };
        var text = $"{logName} Ln: {lineNumber} {message}";

        builder.AppendLine(text);
        Engine.Instance?.Commands?.Log(text);
        if (consoleWindow != null)
            WriteLine(consoleWindow, text, level);
        if (level == LogLevel.Assert)
            Debugger.Break();
    }

    private static void WriteLine(IConsole console, string text, LogLevel level) 
    {
        var colors = level switch 
        {
            LogLevel.Debug => $"\u001b[37m",
            LogLevel.Assert => $"\u001b[91m",
            LogLevel.Warning => $"\u001b[93m",
            LogLevel.Error => $"\u001b[91m",
            LogLevel.Verbose => $"\u001b[95m",
            _ => "\u001b[96m"
        };
        console.StdOut.WriteLine($"\u001b[37m({DateTime.Now.ToString("HH:mm:ss")}) {colors}{text}\u001b[0m");
    }

    public static void Log(
        string log, 
        LogLevel logLevel = LogLevel.Debug,
        [CallerLineNumber] int callerLineNumber = 0
    ) 
    {
        LogInternal(logLevel, log, callerLineNumber);
    }

    public static void Log(
        object log,
        LogLevel logLevel = LogLevel.Debug,
        [CallerLineNumber] int callerLineNumber = 0
    ) 
    {
        string message = log switch 
        {
            null => "null",
            _ => log.ToString() ?? "null"
        };
        LogInternal(logLevel, message, callerLineNumber);
    }

    [Conditional("DEBUG")]
    public static void Assert(
        bool condition, 
        string message, 
        [CallerLineNumber] int callerLineNumber = 0
    ) 
    {
        if (!condition)
            LogInternal(LogLevel.Assert, message, callerLineNumber);
    }

    public static void Info(
        string log, 
        [CallerLineNumber] int callerLineNumber = 0
    ) 
    {
        LogInternal(LogLevel.Info, log, callerLineNumber);
    }


    public static void Info(
        object log, 
        [CallerLineNumber] int callerLineNumber = 0
    ) 
    {
        string message = log switch 
        {
            null => "null",
            _ => log.ToString() ?? "null"
        };
        
        LogInternal(LogLevel.Info, message, callerLineNumber);
    }

    public static void Warning(
        string log, 
        [CallerLineNumber] int callerLineNumber = 0
    ) 
    {
        LogInternal(LogLevel.Warning, log, callerLineNumber);
    }


    public static void Warning(
        object log, 
        [CallerLineNumber] int callerLineNumber = 0
    ) 
    {
        string message = log switch 
        {
            null => "null",
            _ => log.ToString() ?? "null"
        };
        
        LogInternal(LogLevel.Warning, message, callerLineNumber);
    }

    public static void Verbose(
        string log, 
        [CallerLineNumber] int callerLineNumber = 0
    ) 
    {
        LogInternal(LogLevel.Verbose, log, callerLineNumber);
    }


    public static void Verbose(
        object log, 
        [CallerLineNumber] int callerLineNumber = 0
    ) 
    {
        string message = log switch 
        {
            null => "null",
            _ => log.ToString() ?? "null"
        };
        
        LogInternal(LogLevel.Verbose, message, callerLineNumber);
    }

    public static void Error(
        string log, 
        [CallerLineNumber] int callerLineNumber = 0
    ) 
    {
        LogInternal(LogLevel.Error, log, callerLineNumber);
    }


    public static void Error(
        object log, 
        [CallerLineNumber] int callerLineNumber = 0
    ) 
    {
        string message = log switch 
        {
            null => "null",
            _ => log.ToString() ?? "null"
        };
        
        LogInternal(LogLevel.Error, message, callerLineNumber);
    }

#if !ANDROID && !Blazor
    public static void OpenLog(string path) 
    {
        var process = new Process() 
        {
            StartInfo = new ProcessStartInfo(path) 
            {
                UseShellExecute = true
            }
        };
        if (File.Exists(path))
            process.Start();
    }
#endif

    public static void WriteToFile(string path) 
    {
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        WriteToFile(fs);
    }

    public static void WriteToFile(Stream stream) 
    {
        var texts = builder.ToString();
        if (string.IsNullOrEmpty(texts))
            return;
        using var textWriter = new StreamWriter(stream);
        textWriter.WriteLine(texts);
    }
}