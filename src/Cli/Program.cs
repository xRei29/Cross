using Core;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;

bool jsonMode = args.Contains("--json");
EnvironmentReport report = EnvironmentInfo.Collect();

Console.OutputEncoding = System.Text.Encoding.UTF8;

if (jsonMode)
{
    var payload = new
    {
        Student = "Конопка Роман",
        Group = "ФЕІ-36",
        report.OsDescription,
        report.FrameworkDescription,
        report.ProcessArchitecture,
        report.DetectedRid,
        report.ReportedRid,
        report.BaseDirectory,
        Domain = "Замовлення (клієнти, товари, замовлення, рядки замовлення)"
    };

   Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions 
{ 
    WriteIndented = true,
    Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
}));
}
else
{
    Console.WriteLine("CrossApp – інформація про середовище");
    Console.WriteLine("Студент: Конопка Роман, група ФЕІ-36");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"ОС              : {report.OsDescription}");
    Console.WriteLine($"Runtime         : {report.FrameworkDescription}");
    Console.WriteLine($"Архітектура     : {report.ProcessArchitecture}");
    Console.WriteLine($"RID (визначено) : {report.DetectedRid}");
    Console.WriteLine($"RID (від .NET)  : {report.ReportedRid}");
    Console.WriteLine($"Каталог         : {report.BaseDirectory}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine("Предметна область: Замовлення (клієнти, товари, замовлення, рядки замовлення)");
    Console.WriteLine($"Нотатка збірки  : {report.BuildNote}");
}