string? command;
do
{
    Console.Write("$ ");
    command = Console.ReadLine();
    Console.WriteLine($"{command}: command not found");
} while (command != "exit");
