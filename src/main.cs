Console.Write("$ ");

string? command;
do
{
    command = Console.ReadLine();
    Console.WriteLine($"{command}: command not found");
} while (command != "exit");
