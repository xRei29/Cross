# CrossApp

Наскрізний проєкт з крос-платформного програмування.

Предметна область: Замовлення.
Сутності: Customer (клієнт), Product (товар), Order (замовлення), OrderLine (рядок замовлення).
Призначення: оформлення замовлень клієнтами та підрахунок сум.

## Запуск

dotnet build
dotnet run --project src/Cli

Запуск у JSON-режимі:

dotnet run --project src/Cli -- --json

## Середовище

.NET SDK 8.0, Windows

## Публікація (додаткове завдання)

- win-x64 (self-contained): ~70.5 MB
- linux-x64 (self-contained): ~70.6 MB

Розміри майже однакові, оскільки self-contained публікація включає весь runtime .NET незалежно від цільової ОС.