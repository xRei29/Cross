# CrossApp
Наскрізний проєкт з крос-платформного програмування.

Предметна область: Замовлення.
Сутності: Customer (клієнт), Product (товар), Order (замовлення), OrderLine (рядок замовлення).
Призначення: оформлення замовлень клієнтами та підрахунок сум.

## Структура проєкту
- src/Core — class library, спільна логіка (наразі: збір інформації про середовище виконання). Не має точки входу, не запускається самостійно.
- src/Cli — консольний застосунок, використовує Core через ProjectReference, лише форматує вивід.

Заплановані каталоги в Core (наступні тижні):
- Core/Dto/ — record-типи форматів даних (тиждень 3)
- Core/Domain/ — сутності з поведінкою (тиждень 4)
- Core/Storage/ — реалізації сховищ (тиждень 5)

## Середовище розробки
- ОС: Windows 10 (build 10.0.26200)
- .NET SDK: 8.0.30
- Редактор: VS Code + C# Dev Kit
- RID: win-x64

## Запуск

    dotnet build
    dotnet run --project src/Cli

Запуск у JSON-режимі:

    dotnet run --project src/Cli -- --json

## Multi-targeting (Core)

Core.csproj зібрано під два TFM (TargetFrameworks): net8.0 та net9.0.

    dotnet build src/Core/Core.csproj

У Core/EnvironmentInfo.cs додано умовну компіляцію (#if NET9_0_OR_GREATER), яка визначає поле BuildNote залежно від TFM, під яким зібрано Core. При запуску Cli (зібраного під net8.0) виводиться:

    Нотатка збірки  : збірка під net8.0

## Публікація

    dotnet publish src/Cli -c Release -r win-x64 --self-contained true -o publish/win-x64-sc
    dotnet publish src/Cli -c Release -r win-x64 --self-contained false -o publish/win-x64-fd
    dotnet publish src/Cli -c Release -r linux-x64 --self-contained true -o publish/linux-x64-sc

| RID | Режим | Розмір | Runtime потрібен |
|---|---|---|---|
| win-x64 | self-contained | 70.54 MB | ні |
| win-x64 | framework-dependent | 0.17 MB | так (.NET 8) |
| linux-x64 | self-contained | ~70 MB | ні |

Self-contained включає весь .NET runtime, тому не потребує встановленого .NET на цільовій машині — ціна: розмір у ~400 разів більший за framework-dependent.

Запуск self-contained напряму (без dotnet run, працює на будь-якому Windows x64, навіть без встановленого .NET):

    .\publish\win-x64-sc\Cli.exe

Запуск framework-dependent (потребує встановленого .NET 8 на машині, де запускається):

    .\publish\win-x64-fd\Cli.exe

## Перевірка linux-x64 через Docker

Оскільки фізична машина — Windows, для реального запуску Linux-бінарника використано контейнер з мінімальним образом (лише системні залежності, без встановленого .NET runtime — бо публікація self-contained):

    docker run --rm -v "${PWD}\publish\linux-x64-sc:/app" mcr.microsoft.com/dotnet/runtime-deps:8.0 /app/Cli

Вивід підтвердив коректний запуск:

    ОС              : Debian GNU/Linux 12 (bookworm)
    RID (визначено) : linux-x64
    Каталог         : /app/

## Додаткові завдання

### PublishSingleFile

    dotnet publish src/Cli -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/win-x64-singlefile

Кількість файлів: 3 (замість ~187 у звичайній self-contained публікації). Розмір: 64.44 MB. Застосунок запускається і працює коректно.

### PublishTrimmed

    dotnet publish src/Cli -c Release -r win-x64 --self-contained true -p:PublishTrimmed=true -o publish/win-x64-trimmed

Розмір: 18.11 MB (менше за звичайний self-contained у ~4 рази). Збірка видала 2 попередження IL2026 про JsonSerializer.Serialize — trimming-аналізатор попереджає, що серіалізація через рефлексію може непередбачувано зламатися після видалення "невикористаного" коду, оскільки лінкер не завжди здатний статично визначити, які типи насправді потрібні для JSON-серіалізації. У цьому проєкті застосунок все одно запустився коректно, але для складнішого коду з динамічними типами trimming міг би призвести до помилки під час виконання.

### Умовна компіляція (multi-targeting)

У Core/EnvironmentInfo.cs додано директиви #if #if NET9_0_OR_GREATER, які визначають поле BuildNote залежно від TFM (див. розділ "Multi-targeting" вище).

### Перевірка публікації іншої ОС у Docker

Виконано публікацію під linux-x64 і перевірку реальним запуском у Docker-контейнері (див. розділ "Перевірка linux-x64 через Docker" вище).



## Лабораторна 3: DTO та імпорт CSV/JSON

### Record-типи (Core/Dto)
- ProductDto(Id, Name, Price, Note?) — Note nullable, бо примітка необов'язкова
- CustomerDto(Id, Name, Email?) — Email nullable, бо клієнт може його не вказати

### Формат файлу
- data/sample.csv — роздільник `;`, є рядок заголовка (id;name;price), кодування UTF-8
- data/sample.json — масив об'єктів ProductDto
- data/mixed.csv — розпізнавання за префіксом (P — товар, C — клієнт)

Файл sample.csv навмисно містить 3 пошкоджені рядки (порожня ціна, порожня назва, забагато колонок) — тестові дані для перевірки обробки помилок.

### Запуск

    dotnet run --project src/Cli
    dotnet run --project src/Cli -- data/sample.json
    dotnet run --project src/Cli -- data/mixed.csv
    dotnet run --project src/Cli -- data/nonexistent.csv

### Вивід (коректний файл)

    Завантажено записів: 10
      PR-001   Ноутбук Lenovo IdeaPad           24999.99
      PR-002   Миша бездротова Logitech           399.50
      PR-003   Клавіатура механічна              1899.00
      PR-004   Монітor 27 дюймів                 8500.00
      PR-005   Навушники накладні                1250.75
    Пропущено рядків: 3
      ! рядок 12: ціна '' не є коректним невід'ємним числом
      ! рядок 13: назва порожня
      ! рядок 14: занадто багато колонок: 4
    Всього: 13, прийнято: 10, пропущено: 3, помилок: 23.1%

### Патерни у switch expression (ProductCsvImporter.ParseLine)
- Патерн властивості: `{ Length: < 3 }` — перевірка кількості колонок
- Патерн списку: `[_, "", _]` — порожня назва
- Патерн з умовою (when): перевірка коректності ціни через TryParse
- Патерн списку зі змінними: `[var id, var name, var price]` — успішний розбір
- Wildcard `_` — забагато колонок

### Додаткові завдання
- JSON-імпортер (ProductJsonImporter) — вибір за розширенням файлу через switch
- MixedImporter — розпізнавання різнорідних рядків за префіксом (P/C), повертає ImportResult<object>
- Статистика імпорту (всього/прийнято/пропущено/% помилок) одним рядком