using Core.Domain;
using Core.Events;
using Core.Storage;
using Xunit;

namespace Core.Tests;

public class ImportTests
{
    // ---------- OrderService.ImportAsync з підмінним імпортером ----------

    [Fact]
    public async Task ImportAsync_AddsOrders_AndRaisesCreatedEvents()
    {
        var service = TestData.NewService();
        int created = 0;
        service.Changed += (_, e) => { if (e.Kind == OrderChangeKind.Created) created++; };

        var (added, skipped) = await service.ImportAsync(
            FakeImporter.Returning(TestData.OrderWithLine("A-1"), TestData.OrderWithLine("A-2")), "any.json");

        Assert.Equal(2, added);
        Assert.Equal(0, skipped);
        Assert.Equal(2, created);
        Assert.Equal(2, service.All().Count);
    }

    [Fact]
    public async Task ImportAsync_Twice_IsIdempotent()
    {
        var service = TestData.NewService();
        var importer = FakeImporter.Returning(TestData.OrderWithLine("A-1"));

        await service.ImportAsync(importer, "any.json");
        var (added, skipped) = await service.ImportAsync(importer, "any.json");

        Assert.Equal(0, added);
        Assert.Equal(1, skipped);
        Assert.Single(service.All());
    }

    // ---------- Скасування ----------

    [Fact]
    public async Task ImportAsync_AlreadyCancelledToken_ThrowsOperationCanceled_BeforeTouchingFile()
    {
        var service = TestData.NewService();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Файлу "any.json" не існує, але має бути саме скасування, а не FileNotFoundException/StorageException.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new JsonOrderImporter(), "any.json", cts.Token));
    }

    [Fact]
    public async Task JsonImporter_AlreadyCancelledToken_ThrowsOperationCanceled_NotStorageException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new JsonOrderImporter().ImportAsync("any.json", cts.Token));
    }

    [Fact]
    public async Task ImportAsync_CancelledDuringWork_ThrowsOperationCanceled()
    {
        var service = TestData.NewService();
        using var cts = new CancellationTokenSource();

        Task task = service.ImportAsync(FakeImporter.Endless(), "any.json", cts.Token);
        cts.CancelAfter(50);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.Empty(service.All());
    }

    // ---------- Додаткове: два джерела скасування (користувач / таймаут) ----------

    [Fact]
    public async Task LinkedToken_TimeoutFires_UserTokenStaysClean()
    {
        var service = TestData.NewService();
        using var userCts = new CancellationTokenSource();
        using var timeoutCts = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(userCts.Token, timeoutCts.Token);
        timeoutCts.CancelAfter(50);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(FakeImporter.Endless(), "any.json", linked.Token));

        Assert.True(timeoutCts.IsCancellationRequested);
        Assert.False(userCts.IsCancellationRequested);   // саме так відрізняємо таймаут від скасування користувачем
    }

    [Fact]
    public async Task LinkedToken_UserCancels_TimeoutStaysClean()
    {
        var service = TestData.NewService();
        using var userCts = new CancellationTokenSource();
        using var timeoutCts = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(userCts.Token, timeoutCts.Token);
        userCts.CancelAfter(50);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(FakeImporter.Endless(), "any.json", linked.Token));

        Assert.True(userCts.IsCancellationRequested);
        Assert.False(timeoutCts.IsCancellationRequested);
    }

    // ---------- Реальний JsonOrderImporter на тимчасових файлах ----------

    [Fact]
    public async Task JsonImporter_ValidFile_ReturnsOrders_AndReportsProgress()
    {
        string path = TestData.WriteTempFile(TestData.ValidJson);
        try
        {
            var percents = new List<int>();
            var progress = new SyncProgress(percents.Add);

            var orders = await new JsonOrderImporter().ImportAsync(path, progress: progress);

            Assert.Equal(2, orders.Count);
            Assert.Equal(21m, orders[0].Total);              // 10.5 × 2
            Assert.Equal(OrderStatus.Confirmed, orders[1].Status);
            Assert.Equal(new[] { 50, 100 }, percents);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task JsonImporter_MissingFile_ThrowsStorageException_WithInnerFileNotFound()
    {
        string path = Path.Combine(Path.GetTempPath(), $"no-such-{Guid.NewGuid():N}.json");

        var ex = await Assert.ThrowsAsync<StorageException>(() => new JsonOrderImporter().ImportAsync(path));

        Assert.IsType<FileNotFoundException>(ex.InnerException);
    }

    [Fact]
    public async Task JsonImporter_BrokenJson_ThrowsStorageException()
    {
        string path = TestData.WriteTempFile("[ { \"id\": \"X\", ");
        try
        {
            await Assert.ThrowsAsync<StorageException>(() => new JsonOrderImporter().ImportAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task JsonImporter_InvalidRecord_ThrowsDomainException_WithRecordNumber()
    {
        const string json = """
            [
              { "id": "OK-1", "customerId": "C", "status": "Draft",
                "lines": [ { "productId": "P", "name": "Товар", "price": 10, "quantity": 1 } ] },
              { "id": "BAD-2", "customerId": "C", "status": "Draft",
                "lines": [ { "productId": "P", "name": "Товар", "price": -5, "quantity": 1 } ] }
            ]
            """;
        string path = TestData.WriteTempFile(json);
        try
        {
            var ex = await Assert.ThrowsAsync<DomainException>(() => new JsonOrderImporter().ImportAsync(path));

            Assert.Contains("Запис 2", ex.Message);
            Assert.IsAssignableFrom<ArgumentException>(ex.InnerException);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task JsonImporter_DuplicateIds_ThrowsDomainException()
    {
        const string json = """
            [
              { "id": "D-1", "customerId": "C", "status": "Draft", "lines": [] },
              { "id": "d-1", "customerId": "C", "status": "Draft", "lines": [] }
            ]
            """;
        string path = TestData.WriteTempFile(json);
        try
        {
            await Assert.ThrowsAsync<DomainException>(() => new JsonOrderImporter().ImportAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ---------- Додаткове: потокове читання ----------

    [Fact]
    public async Task StreamAsync_YieldsOrdersOneByOne()
    {
        string path = TestData.WriteTempFile(TestData.ValidJson);
        try
        {
            var ids = new List<string>();
            await foreach (Order o in new JsonOrderImporter().StreamAsync(path))
                ids.Add(o.Id);

            Assert.Equal(new[] { "J-1", "J-2" }, ids);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task StreamAsync_CancelledViaWithCancellation_Throws()
    {
        string path = TestData.WriteTempFile(TestData.ValidJson);
        try
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await foreach (Order _ in new JsonOrderImporter().StreamAsync(path).WithCancellation(cts.Token))
                {
                }
            });
        }
        finally
        {
            File.Delete(path);
        }
    }

    // Синхронний IProgress<T>: Progress<T> викликає обробник у пулі потоків, і значення могли б прийти пізніше за Assert.
    private sealed class SyncProgress(Action<int> onReport) : IProgress<int>
    {
        public void Report(int value) => onReport(value);
    }
}