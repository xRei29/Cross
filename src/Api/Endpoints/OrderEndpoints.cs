using System.Security.Cryptography;
using Api.Contracts;
using Api.Filters;
using Core;
using Core.Abstractions;
using Core.Configuration;
using Core.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace Api.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrders(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders");

        // GET список: пагінація і пошук (додаткове 1) -> { items, total, page, pageSize }
        group.MapGet("/", Results<Ok<PagedResponse<OrderResponse>>, ValidationProblem>
            (IOrderStore store, int? page, int? pageSize, string? search) =>
        {
            int pageNumber = page ?? 1;
            int size = pageSize ?? 20;

            var errors = new Dictionary<string, string[]>();
            if (pageNumber < 1) errors["page"] = ["Сторінка має бути не меншою за 1."];
            if (size is < 1 or > 100) errors["pageSize"] = ["Розмір сторінки — від 1 до 100."];
            if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

            IEnumerable<Order> query = store.List();
            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim();
                query = query.Where(o =>
                    o.Id.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    o.CustomerId.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            List<Order> filtered = query.OrderBy(o => o.CreatedAt).ThenBy(o => o.Id).ToList();
            List<OrderResponse> items = filtered
                .Skip((pageNumber - 1) * size)
                .Take(size)
                .Select(o => o.ToResponse())
                .ToList();

            return TypedResults.Ok(new PagedResponse<OrderResponse>(items, filtered.Count, pageNumber, size));
        });

        // GET за id
        group.MapGet("/{id}", Results<Ok<OrderResponse>, NotFound> (string id, IOrderStore store) =>
            store.GetById(id) is { } found
                ? TypedResults.Ok(found.ToResponse())
                : TypedResults.NotFound());

        // GET сума замовлення (маршрут варіанту)
        group.MapGet("/{id}/total", Results<Ok<OrderTotalResponse>, NotFound>
            (string id, IOrderStore store, IOptions<OrdersOptions> options) =>
            store.GetById(id) is { } found
                ? TypedResults.Ok(new OrderTotalResponse(found.Id, found.Total, options.Value.Currency))
                : TypedResults.NotFound());

        // POST: валідація робиться фільтром (до обробника)
        group.MapPost("/", Results<Created<OrderResponse>, ValidationProblem>
            (SaveOrderRequest request, IOrderStore store) =>
        {
            string id = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();
            Order created = BuildOrder(id, request, createdAt: null);

            store.Add(created);
            return TypedResults.Created($"/api/orders/{created.Id}", created.ToResponse());
        })
        .AddEndpointFilter<ValidationFilter<SaveOrderRequest>>();

        // PUT повністю замінює замовлення; змінювати можна лише Draft (інакше 409 від обробника винятків)
        group.MapPut("/{id}", Results<Ok<OrderResponse>, NotFound, ValidationProblem>
            (string id, SaveOrderRequest request, IOrderStore store) =>
        {
            if (store.GetById(id) is not { } existing)
                return TypedResults.NotFound();

            if (existing.Status != OrderStatus.Draft)
                throw new DomainException(
                    $"Замовлення {existing.Id} у статусі {existing.Status}, змінювати його не можна.");

            Order replacement = BuildOrder(existing.Id, request, existing.CreatedAt);
            store.Update(replacement);
            return TypedResults.Ok(replacement.ToResponse());
        })
        .AddEndpointFilter<ValidationFilter<SaveOrderRequest>>()
        .ProducesProblem(StatusCodes.Status409Conflict);

        // DELETE
        group.MapDelete("/{id}", Results<NoContent, NotFound> (string id, IOrderStore store) =>
            store.Remove(id) ? TypedResults.NoContent() : TypedResults.NotFound());

        // Підтвердження замовлення; повторне або недозволене -> DomainException -> 409
        group.MapPost("/{id}/confirm", Results<Ok<OrderResponse>, NotFound> (string id, IOrderStore store) =>
        {
            if (store.GetById(id) is not { } order)
                return TypedResults.NotFound();

            order.Confirm();
            store.Update(order);
            return TypedResults.Ok(order.ToResponse());
        })
        .ProducesProblem(StatusCodes.Status409Conflict);
    }

    // Валідний запит гарантовано проходить інваріанти домену, тому тут без додаткових перевірок.
    private static Order BuildOrder(string id, SaveOrderRequest request, DateTimeOffset? createdAt)
    {
        Order order = Order.Create(id, request.CustomerId, createdAt);
        foreach (SaveOrderLineRequest line in request.Lines)
            order.AddLine(line.ProductId, line.Name, line.Price, line.Quantity);

        return order;
    }
}
