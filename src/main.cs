using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;


int exitReturnCode = 0;
bool shouldExit = false;
List<string> builtins = ["exit", "echo", "type", "pwd", "cd"];
TextWriter consoleOut = Console.Out, consoleError = Console.Error;

while (!shouldExit)
{
    Console.Write("$ ");
    string command = Console.ReadLine()!;

    List<string> parts = ParseAndSplitCommandLine(command);
    RedirectConsoleIfRequested(parts);
    if (!ExecuteBuiltIn(parts) && !ExecuteProgram(parts))
    {
        Console.Error.WriteLine($"{parts[0]}: command not found");
    }
    RestoreConsoleRedirection();
}

void RedirectConsoleIfRequested(List<string> parts)
{
    for (int i = 0; i < parts.Count - 1; i++)
    {
        switch (parts[i])
        {
            case ">":
            case "1>":
                Console.SetOut(new StreamWriter(parts[i + 1], append: false));
                parts[i] = parts[i + 1] = string.Empty;
                break;
            case ">>":
            case "1>>":
                Console.SetOut(new StreamWriter(parts[i + 1], append: true));
                parts[i] = parts[i + 1] = string.Empty;
                break;
            case "2>":
                Console.SetError(new StreamWriter(parts[i + 1], append: false));
                parts[i] = parts[i + 1] = string.Empty;
                break;
            case "2>>":
                Console.SetError(new StreamWriter(parts[i + 1], append: true));
                parts[i] = parts[i + 1] = string.Empty;
                break;
            default:
                break;
        }
    }
    _ = parts.RemoveAll(string.IsNullOrEmpty);
}

void RestoreConsoleRedirection()
{
    if (Console.Out != consoleOut)
    {
        Console.Out.Dispose();
        Console.SetOut(consoleOut);
    }
    if (Console.Error != consoleError)
    {
        Console.Error.Dispose();
        Console.SetError(consoleError);
    }
}

List<string> ParseAndSplitCommandLine(ReadOnlySpan<char> command)
{
    command = command.Trim();
    List<string> parts = [];
    StringBuilder sb = new();

    int pos = 0;
    while (pos != -1)
    {
        pos = command.IndexOfAny([' ', '"', '\'', '\\']);
        if (pos >= 0)
        {
            switch (command[pos])
            {
                case ' ':
                    _ = sb.Append(command[0..pos]);
                    command = command[pos..].TrimStart();
                    parts.Add(sb.ToString());
                    _ = sb.Clear();
                    break;
                case '\'':
                    ConsumeInput(ref command, false);
                    pos = command.IndexOf('\'');
                    ConsumeInput(ref command, false);
                    break;
                case '"':
                    ConsumeInput(ref command, false);
                    pos = command.IndexOfAny('"', '\\');
                    while (command[pos] == '\\')
                    {
                        if (command[pos + 1] is '\\' or '\"')
                        {
                            ConsumeInput(ref command, false);
                            ConsumeChar(ref command);
                        }
                        else
                        {
                            ConsumeInput(ref command, true);
                        }
                        pos = command.IndexOfAny('"', '\\');
                    }
                    ConsumeInput(ref command, false);
                    break;
                case '\\':
                    ConsumeInput(ref command, false);
                    ConsumeChar(ref command);
                    break;
                default:
                    throw new ShellException($"Unhandled case `{command[pos]}`");
            }
        }
    }
    _ = sb.Append(command);
    parts.Add(sb.ToString());

    return parts;


    void ConsumeInput(ref ReadOnlySpan<char> command, bool includeCurrentPos)
    {
        _ = sb.Append(command[0..pos]);
        if (includeCurrentPos)
        {
            _ = sb.Append(command[pos]);
        }
        command = command[(pos + 1)..];
    }

    void ConsumeChar(ref ReadOnlySpan<char> command)
    {
        _ = sb.Append(command[0]);
        command = command[1..];
    }
}

return exitReturnCode;


bool ExecuteBuiltIn(List<string> parts)
{
    string builtInName = parts[0];
    if (builtins.Contains(builtInName))
    {
        switch (builtInName)
        {
            case "exit":
                if (parts.Count > 1)
                {
                    exitReturnCode = int.Parse(parts[1], CultureInfo.InvariantCulture);
                }
                shouldExit = true;
                break;
            case "echo":
                for (int i = 1; i < parts.Count; i++)
                {
                    if (i > 1)
                    {
                        Console.Write(' ');
                    }
                    Console.Write(parts[i]);
                }
                Console.WriteLine();
                break;
            case "type":
                string? cmdName = parts.Count > 1 ? parts[1] : null;
                if (cmdName == null)
                {
                    break;
                }
                if (builtins.Contains(cmdName))
                {
                    Console.WriteLine($"{cmdName} is a shell builtin");
                }
                else if (SearchPath(cmdName, out string? foundAt))
                {
                    Console.WriteLine($"{cmdName} is {Path.Combine(foundAt, cmdName)}");
                }
                else
                {
                    Console.Error.WriteLine($"{cmdName}: not found");
                }
                break;
            case "pwd":
                Console.WriteLine(Environment.GetEnvironmentVariable("PWD"));
                break;
            case "cd":
                string? dirName = parts.Count > 1 ? parts[1] : null;
                if (dirName == null)
                {
                    break;
                }

                string fullName;
                fullName = dirName == "~"
                    ? Environment.GetEnvironmentVariable("HOME")!
                    : Path.Combine(Environment.GetEnvironmentVariable("PWD")!, dirName);
                if (Directory.Exists(fullName))
                {
                    fullName = new DirectoryInfo(fullName).FullName.TrimEnd(Path.DirectorySeparatorChar);
                    Environment.SetEnvironmentVariable("PWD", fullName);
                }
                else
                {
                    Console.Error.WriteLine($"cd: {dirName}: No such file or directory");
                }
                break;
            default:
                throw new ShellException($"Unknown builtin {builtInName}");
        }
        return true;
    }
    else
    {
        return false;
    }
}

bool ExecuteProgram(List<string> parts)
{
    string cmd = parts[0];
    if (SearchPath(cmd, out string? foundAt))
    {
        string fullCmd = Path.Combine(foundAt, cmd);
        ProcessStartInfo processStartInfo = new(fullCmd, parts.Skip(1))
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        Process? p = Process.Start(processStartInfo);
        if (p != null)
        {
            p.WaitForExit();
            string output = p.StandardOutput.ReadToEnd();
            Console.Write(output);
            string error = p.StandardError.ReadToEnd();
            Console.Error.Write(error);
            return true;
        }
    }

    return false;
}

bool SearchPath(string cmd, [NotNullWhen(true)] out string? foundAt)
{
    string? pathEnvVar = Environment.GetEnvironmentVariable("PATH");
    if (pathEnvVar != null)
    {
        string[] parts = pathEnvVar.Split(Path.PathSeparator);
        foreach (string part in parts)
        {
            if (File.Exists(Path.Combine(part, cmd)))
            {
                foundAt = part;
                return true;
            }
        }
    }

    foundAt = null;
    return false;
};


internal sealed class ShellException(string message) : Exception(message);
