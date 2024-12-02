using System.Globalization;


int exitReturnCode = 0;
bool shouldExit = false;
List<string> builtins = ["exit"];

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
                exitReturnCode = int.Parse(parts[1], CultureInfo.InvariantCulture);
                shouldExit = true;
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
