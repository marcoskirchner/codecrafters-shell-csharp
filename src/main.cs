using System.Globalization;


int exitReturnCode = 0;
bool shouldExit = false;
List<string> builtins = ["exit", "echo"];

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
            default:
                throw new ShellException($"Unknown builtin {parts[0]}");
        }
        return true;
    }
    else
    {
        return false;
    }
};


internal sealed class ShellException(string message) : Exception(message);
