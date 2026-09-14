# CrossApp


Предметна область: Замовлення.
Сутності: Customer (клієнт), Product (товар), Order (замовлення), OrderLine (рядок замовлення).
Призначення: оформлення замовлень клієнтами та підрахунок сум.

## Запуск

dotnet build
dotnet run --project src/Cli

Запуск у JSON-режимі:

dotnet run --project src/Cli -- --json

## Середовище

- .NET SDK 8.0, 
- RID: win-x64

## Публікація (додаткове завдання)
 

(Get-ChildItem -Recurse "src\Cli\bin\Release\net8.0\win-x64\publish" | Measure-Object -Property Length -Sum).Sum / 1MB
(Get-ChildItem -Recurse "src\Cli\bin\Release\net8.0\linux-x64\publish" | Measure-Object -Property Length -Sum).Sum / 1MB


 win-x64 (self-contained): ~70.5 MB,
 linux-x64 (self-contained): ~70.6 MB

Розміри майже однакові, оскільки self-contained публікація включає весь runtime .NET незалежно від цільової ОС.

Перевірка запуску опублікованого бінарника без dotnet run:
.\src\Cli\bin\Release\net8.0\win-x64\publish\Cli.exe

## Перевірка публікацій (лабораторна 2)

    dotnet publish src/Cli -c Release -r win-x64 --self-contained true -o publish/win-x64-sc
    dotnet publish src/Cli -c Release -r win-x64 --self-contained false -o publish/win-x64-fd
    dotnet publish src/Cli -c Release -r linux-x64 --self-contained true -o publish/linux-x64-sc

| RID | Режим | Розмір | Runtime потрібен |
|---|---|---|---|
| win-x64 | self-contained | 70.54 MB | ні |
| win-x64 | framework-dependent | 0.17 MB | так (.NET 8) |
| linux-x64 | self-contained | ~70 MB | ні |

Запуск win-x64 напряму (без dotnet run):

    .\publish\win-x64-sc\Cli.exe

Перевірка linux-x64 через Docker (mcr.microsoft.com/dotnet/runtime-deps:8.0):

    docker run --rm -v "${PWD}\publish\linux-x64-sc:/app" mcr.microsoft.com/dotnet/runtime-deps:8.0 /app/Cli

Вивід підтвердив коректний запуск:

    ОС              : Debian GNU/Linux 12 (bookworm)
    RID (визначено) : linux-x64
    Каталог         : /app/