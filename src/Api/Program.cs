using Api.Contracts;
using Api.Endpoints;
using Api.Infrastructure;
using Core;
using Core.Abstractions;
using Core.Configuration;
using Core.Storage;
using FluentValidation;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();                       // формат помилок RFC 9457
builder.Services.AddExceptionHandler<DomainExceptionHandler>();   // DomainException -> 409

// Некоректне тіло (кривий JSON, "quantity":"abc") -> 400 без винятку, а не 500
builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = false);

// Ключ Orders:Currency з appsettings.json (той самий механізм, що в Lab 9)
builder.Services.Configure<OrdersOptions>(builder.Configuration.GetSection("Orders"));

// Валідатор не має стану, тому Singleton
builder.Services.AddSingleton<IValidator<SaveOrderRequest>, SaveOrderRequestValidator>();

// Сховище: Singleton, бо дані в пам'яті мають жити весь час роботи застосунку;
// LockedOrderStore робить його безпечним для паралельних запитів.
builder.Services.AddSingleton<IOrderStore>(
    _ => new LockedOrderStore(new InMemoryOrderStore(SampleData.Orders())));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // /openapi/v1.json
    app.MapScalarApiReference();    // /scalar/v1

    // Лише для демонстрації: неочікуваний виняток дає 500 без стеку й деталей
    app.MapGet("/api/boom", () =>
    {
        throw new InvalidOperationException("секретна внутрішня деталь");
    });
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapOrders();

app.Run();

// Потрібно, щоб WebApplicationFactory<Program> бачив точку входу в інтеграційних тестах
public partial class Program { }
