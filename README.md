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


## Лабораторна 4: Доменна модель (Order/OrderLine)

### Сутності (Core/Domain)
- Order(Id, CustomerId, Status, Lines) — агрегат, приватний конструктор, фабричний метод Create
- OrderLine(ProductId, Name, Price, Quantity) — рядок замовлення
- OrderStatus (enum) — Draft, Confirmed, Cancelled
- CatalogProduct — доменна обгортка над ProductDto (тиждень 3)

### Інваріанти

| Правило | Виняток | Метод |
|---|---|---|
| Id / CustomerId не порожні | ArgumentException | Order.Create |
| Ціна не від'ємна | ArgumentOutOfRangeException | Order.AddLine |
| Кількість > 0 | ArgumentOutOfRangeException | Order.AddLine |
| Рядки лише в статусі Draft | InvalidOperationException | Order.AddLine |
| Не підтвердити порожнє замовлення | InvalidOperationException | Order.Confirm |
| Дозволені лише певні переходи статусу | InvalidOperationException | Order.ChangeStatus |

### Запуск

    dotnet run --project src/Cli

### Вивід

    === Сценарій 1: успіх ===
    Замовлення ORD-001 (клієнт CUST-001) — 2 поз., сума 25798.99, статус: Draft
    Замовлення ORD-001 (клієнт CUST-001) — 2 поз., сума 25798.99, статус: Confirmed

    === Сценарій 2: порушення інваріантів ===
      додавання рядка до підтвердженого замовлення: InvalidOperationException — ...
      від'ємна ціна: ArgumentOutOfRangeException — ...
      нульова кількість: ArgumentOutOfRangeException — ...
      підтвердження порожнього замовлення: InvalidOperationException — ...
      повторне підтвердження: InvalidOperationException — ...
      скасування вже скасованого замовлення: InvalidOperationException — ...

### DTO vs сутність
DTO (ProductDto, OrderDto) — незмінний формат даних, без перевірок. Сутність (Order, CatalogProduct) — власник бізнес-правил, гарантує коректний стан. Зв'язок — ToDto/FromDto.

### Додаткові завдання
- CatalogProductBuilder — ImportResult<ProductDto> → ImportResult<CatalogProduct>, об'єднує помилки парсингу й доменних перевірок
- OrderPolicy — інваріант на кілька замовлень клієнта, винесений окремо від Order (потребує доступу до інших замовлень — це прийде зі сховищем на тижні 5)
- OrderStatus як enum з перевіркою дозволених переходів через switch expression у ChangeStatus


## Лабораторна робота 5 — Сервісний шар: інтерфейси, два сховища, ручний DI

### Що додано
- `Core/Abstractions/IOrderStore.cs` — контракт сховища (List, GetById, Add, Update, Remove, Find)
- `Core/Storage/InMemoryOrderStore.cs` — реалізація на Dictionary (дані живуть, поки працює процес)
- `Core/Storage/FileOrderStore.cs` — реалізація на JSON-файлі (дані зберігаються між запусками через OrderDto)
- `Core/Storage/CachingOrderStore.cs` — декоратор, що кешує список замовлень поверх будь-якого іншого IOrderStore
- `Cli/StoreFactory.cs` — вибір конкретної реалізації за аргументами командного рядка
- `Core/Services/OrderService.cs` — сервіс, що працює лише через IOrderStore (CreateOrder, AddLine, Confirm, All, Find)
- `Core/SampleData.cs` — 15 демозаписів для InMemory-режиму
- Composition root переміщено в `Cli/Program.cs`: тільки там створюються конкретні класи сховищ

### Запуск

| Команда | Сховище | Поведінка |
|---|---|---|
| `dotnet run --project src/Cli` | InMemoryOrderStore | Дані скидаються при кожному запуску |
| `dotnet run --project src/Cli -- --file` | FileOrderStore | Дані накопичуються в `data/orders.json` |
| `dotnet run --project src/Cli -- --cache` | CachingOrderStore (поверх InMemory) | Кешує список замовлень |
| `dotnet run --project src/Cli -- --file --cache` | CachingOrderStore (поверх File) | Файл + кеш одночасно |

### Ключова ідея
`OrderService` приймає в конструкторі тип `IOrderStore`, а не конкретний клас — тому його можна використовувати з будь-якою реалізацією сховища без змін у самому сервісі. Вибір конкретної реалізації відбувається один раз, у `StoreFactory`, яку викликає `Program.cs`.

### Перевірено
- Два запуски з `--file` поспіль дають накопичення записів у `data/orders.json` (не перезапис)
- Пошук `"FileOrderStore"` у `src/Core` знаходиться лише у файлі самої реалізації
- Пошук `"Console."` у `src/Core` — 0 результатів
- Сценарій відмови: дублікат id та AddLine до неіснуючого замовлення коректно кидають `InvalidOperationException` з читабельним повідомленням

## Лабораторна робота 6 — Доменні події, делегати Action/Func, лямбди

### Що додано
- `Core/Events/OrderChangeKind.cs` — enum видів зміни (Created, LineAdded, Confirmed)
- `Core/Events/OrderChangedEventArgs.cs` — дані події: OrderId, Kind, LinesCount, Total, At
- `Core/Services/OrderService.cs` — подія `event EventHandler<OrderChangedEventArgs>? Changed`, приватний метод `OnChanged`, виклик після кожної успішної зміни (CreateOrder, AddLine, Confirm)
- `Core/Import/ProductCsvImporter.cs` — додатковий параметр `Action<string>? onSkippedLine` для звіту про пропущені рядки CSV
- `Cli/Program.cs` — два підписники на подію: локальний метод `OnOrderChanged` (консоль + `logs/app.log`) і лямбда-лічильник `(_, _) => changes++`; показано `-=` і сценарій відмови

### Запуск
| Команда | Що перевіряє |
|---|---|
| `dotnet run --project src/Cli` | подія працює з InMemoryOrderStore |
| `dotnet run --project src/Cli -- --file` | подія працює з FileOrderStore |

Лог подій: `src/Cli/bin/Debug/net8.0/logs/app.log` (створюється автоматично).

### Ключова ідея
`OrderService` не знає про консоль чи файл — він лише піднімає подію `Changed` після успішної зміни стану. Хто і як реагує на подію, вирішує `Program.cs`. Подія не піднімається, якщо операція впала на інваріанті (виняток кидається раніше, ніж викликається `OnChanged`).

### Перевірено
- Пошук `"Console"` і `"logs"` у `src/Core` — 0 результатів
- Кожна успішна операція дає рівно одне повідомлення в консолі й файлі
- Операція, що впала на інваріанті (AddLine до неіснуючого замовлення, Confirm порожнього замовлення) — повідомлення немає
- Після `service.Changed -= OnOrderChanged` консоль і файл мовчать, але лямбда-лічильник продовжує рахувати
- `logs/app.log` формується через `Path.Combine` + `Directory.CreateDirectory`