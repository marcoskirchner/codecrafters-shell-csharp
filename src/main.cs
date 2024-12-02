using System.Diagnostics.CodeAnalysis;
using System.Globalization;


int exitReturnCode = 0;
bool shouldExit = false;
List<string> builtins = ["exit", "echo", "type"];

while (!shouldExit)
{
    Console.Write("$ ");
    string command = Console.ReadLine()!;

    string[] parts = command.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    if (!ExecuteBuiltIn(parts))
    {
        Console.WriteLine($"{command}: command not found");
    }
}

return exitReturnCode;


bool ExecuteBuiltIn(string[] parts)
{
    if (builtins.Contains(parts[0]))
    {
        switch (parts[0])
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
                string? cmd = parts.Length > 1 ? parts[1] : null;
                if (cmd == null)
                {
                    break;
                }
                if (builtins.Contains(cmd))
                {
                    Console.WriteLine($"{cmd} is a shell builtin");
                }
                else if (SearchPath(cmd, out string? foundAt))
                {
                    Console.WriteLine($"{cmd} is {Path.Combine(foundAt, cmd)}");
                }
                else
                {
                    Console.WriteLine($"{cmd}: not found");
                }
                break;
            default:
                throw new ShellException($"Unknown builtin {parts[0]}");
        }
        return true;
    }
    else
    {
        return false;
    }
}

bool SearchPath(string cmd, [NotNullWhen(true)] out string? foundAt)
{
    string? pathEnvVar = Environment.GetEnvironmentVariable("PATH");
    if (pathEnvVar != null)
    {
        string[] parts = pathEnvVar.Split(':');
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
