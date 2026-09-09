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