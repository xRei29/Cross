using System.Net;
using System.Net.Http.Json;
using System.Text;
using Api.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.Tests;

public class OrdersApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public OrdersApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private static SaveOrderRequest ValidRequest(string customer = "CUST-001") =>
        new(customer, [new SaveOrderLineRequest("P-1", "Мишка", 100m, 2)]);

    private async Task<OrderResponse> CreateOrderAsync(string customer = "CUST-001")
    {
        var response = await _client.PostAsJsonAsync("/api/orders", ValidRequest(customer));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        return order!;
    }

    private static StringContent Json(string body) =>
        new(body, Encoding.UTF8, "application/json");

    [Fact]
    public async Task Post_Valid_Returns201_WithLocation_AndOrderIsReadable()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", ValidRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(created);
        Assert.Equal($"/api/orders/{created!.Id}", response.Headers.Location!.OriginalString);
        Assert.Equal(200m, created.Total);

        var get = await _client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
    }

    [Fact]
    public async Task Post_Invalid_Returns400_WithFieldErrors()
    {
        var bad = new SaveOrderRequest("", [new SaveOrderLineRequest("P-1", "Мишка", -5m, 0)]);

        var response = await _client.PostAsJsonAsync("/api/orders", bad);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.NotEmpty(problem!.Errors);
        Assert.Contains(problem.Errors.Keys, k => k.Equals("CustomerId", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Post_EmptyBody_Returns400()
    {
        var response = await _client.PostAsync("/api/orders", Json(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WrongFieldType_Returns400()
    {
        var body = """{"customerId":"CUST-001","lines":[{"productId":"P-1","name":"Мишка","price":1,"quantity":"abc"}]}""";

        var response = await _client.PostAsync("/api/orders", Json(body));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_UnknownId_Returns404()
    {
        var response = await _client.GetAsync("/api/orders/no-such-id");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task FullCrudCycle_EndsWith404()
    {
        var created = await CreateOrderAsync();

        var put = await _client.PutAsJsonAsync($"/api/orders/{created.Id}",
            new SaveOrderRequest("CUST-002", [new SaveOrderLineRequest("P-9", "Клавіатура", 50m, 3)]));
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var updated = await put.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal("CUST-002", updated!.CustomerId);
        Assert.Equal(150m, updated.Total);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);

        var delete = await _client.DeleteAsync($"/api/orders/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var after = await _client.GetAsync($"/api/orders/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);
    }

    [Fact]
    public async Task Put_EmptyCustomer_Returns400()
    {
        var created = await CreateOrderAsync();

        var response = await _client.PutAsJsonAsync($"/api/orders/{created.Id}",
            new SaveOrderRequest("", [new SaveOrderLineRequest("P-1", "Мишка", 10m, 1)]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_ConfirmedOrder_Returns409()
    {
        var created = await CreateOrderAsync();
        var confirm = await _client.PostAsync($"/api/orders/{created.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);

        var response = await _client.PutAsJsonAsync($"/api/orders/{created.Id}", ValidRequest());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Confirm_Twice_Returns409()
    {
        var created = await CreateOrderAsync();

        var first = await _client.PostAsync($"/api/orders/{created.Id}/confirm", null);
        var second = await _client.PostAsync($"/api/orders/{created.Id}/confirm", null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task List_Pagination_ReturnsRequestedPageSize()
    {
        var page = await _client.GetFromJsonAsync<PagedResponse<OrderResponse>>("/api/orders?page=1&pageSize=5");

        Assert.NotNull(page);
        Assert.Equal(5, page!.Items.Count);
        Assert.Equal(1, page.Page);
        Assert.Equal(5, page.PageSize);
        Assert.True(page.Total >= 15);
    }

    [Fact]
    public async Task List_Search_FiltersById()
    {
        var page = await _client.GetFromJsonAsync<PagedResponse<OrderResponse>>("/api/orders?search=ORD-001");

        Assert.NotNull(page);
        Assert.NotEmpty(page!.Items);
        Assert.All(page.Items, o =>
            Assert.True(o.Id.Contains("ORD-001", StringComparison.OrdinalIgnoreCase)
                        || o.CustomerId.Contains("ORD-001", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task List_InvalidPageSize_Returns400()
    {
        var response = await _client.GetAsync("/api/orders?pageSize=1000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Total_ForSampleOrder_Is231_InUah()
    {
        var total = await _client.GetFromJsonAsync<OrderTotalResponse>("/api/orders/ORD-001/total");

        Assert.NotNull(total);
        Assert.Equal(231.00m, total!.Total);
        Assert.Equal("UAH", total.Currency);
    }

    [Fact]
    public async Task Boom_Returns500_WithoutInternalDetails()
    {
        var response = await _client.GetAsync("/api/boom");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("секретна", body);
        Assert.DoesNotContain("InvalidOperationException", body);
    }
}