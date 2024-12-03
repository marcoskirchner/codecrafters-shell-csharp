using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;


int exitReturnCode = 0;
bool shouldExit = false;
List<string> builtins = ["exit", "echo", "type", "pwd", "cd"];

while (!shouldExit)
{
    Console.Write("$ ");
    string command = Console.ReadLine()!;

    string[] parts = ParseAndSplitCommandLine(command);
    if (!ExecuteBuiltIn(parts) && !ExecuteProgram(parts))
    {
        Console.WriteLine($"{parts[0]}: command not found");
    }
}

string[] ParseAndSplitCommandLine(ReadOnlySpan<char> command)
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
                    ConsumeInputSkipChar(ref command);
                    pos = command.IndexOf('\'');
                    ConsumeInputSkipChar(ref command);
                    break;
                case '"':
                    ConsumeInputSkipChar(ref command);
                    pos = command.IndexOf('"');
                    ConsumeInputSkipChar(ref command);
                    break;
                case '\\':
                    ConsumeInputSkipChar(ref command);
                    ConsumeChar(ref command);
                    break;
                default:
                    throw new ShellException($"Unhandled case `{command[pos]}`");
            }
        }
    }
    _ = sb.Append(command);
    parts.Add(sb.ToString());

    return [.. parts];


    void ConsumeInputSkipChar(ref ReadOnlySpan<char> command)
    {
        _ = sb.Append(command[0..pos]);
        command = command[(pos + 1)..];
    }

    void ConsumeChar(ref ReadOnlySpan<char> command)
    {
        _ = sb.Append(command[0]);
        command = command[1..];
    }
}

return exitReturnCode;


bool ExecuteBuiltIn(string[] parts)
{
    string builtInName = parts[0];
    if (builtins.Contains(builtInName))
    {
        switch (builtInName)
        {
            case "exit":
                if (parts.Length > 1)
                {
                    exitReturnCode = int.Parse(parts[1], CultureInfo.InvariantCulture);
                }
                shouldExit = true;
                break;
            case "echo":
                for (int i = 1; i < parts.Length; i++)
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
                string? cmdName = parts.Length > 1 ? parts[1] : null;
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
                    Console.WriteLine($"{cmdName}: not found");
                }
                break;
            case "pwd":
                Console.WriteLine(Environment.GetEnvironmentVariable("PWD"));
                break;
            case "cd":
                string? dirName = parts.Length > 1 ? parts[1] : null;
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
                    Console.WriteLine($"cd: {dirName}: No such file or directory");
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

bool ExecuteProgram(string[] parts)
{
    string cmd = parts[0];
    if (SearchPath(cmd, out string? foundAt))
    {
        string fullCmd = Path.Combine(foundAt, cmd);
        ProcessStartInfo processStartInfo = new(fullCmd, parts.Skip(1))
        {
            RedirectStandardOutput = true
        };
        Process? p = Process.Start(processStartInfo);
        if (p != null)
        {
            p.WaitForExit();
            string output = p.StandardOutput.ReadToEnd();
            Console.Write(output);
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
