using System.Runtime.InteropServices;
using System.Text.Json;

bool jsonMode = args.Contains("--json");

Console.OutputEncoding = System.Text.Encoding.UTF8;

if (jsonMode)
{
    var info = new
    {
        Student = "Конопка Роман", 
        Group = "ФЕІ-36",
        OsDescription = RuntimeInformation.OSDescription,
        OsEnvironment = Environment.OSVersion.ToString(),
        ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
        DotnetVersion = Environment.Version.ToString(),
        Runtime = RuntimeInformation.FrameworkDescription,
        AppDirectory = AppContext.BaseDirectory,
        CurrentDirectory = Environment.CurrentDirectory,
        Domain = "Замовлення (клієнти, товари, замовлення, рядки замовлення)"
    };

    string json = JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine(json);
}
else
{
    Console.WriteLine("CrossApp – практикум з крос-платформного програмування");
    Console.WriteLine("Студент: Конопка Роман, група ФЕІ-36");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"ОС (OSDescription) : {RuntimeInformation.OSDescription}");
    Console.WriteLine($"ОС (Environment) : {Environment.OSVersion}");
    Console.WriteLine($"Архітектура процесу : {RuntimeInformation.ProcessArchitecture}");
    Console.WriteLine($"Версія .NET (CLR) : {Environment.Version}");
    Console.WriteLine($"Runtime : {RuntimeInformation.FrameworkDescription}");
    Console.WriteLine($"Каталог застосунку : {AppContext.BaseDirectory}");
    Console.WriteLine($"Поточний каталог : {Environment.CurrentDirectory}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine("Предметна область: Замовлення (клієнти, товари, замовлення, рядки замовлення)");
}