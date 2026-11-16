using System.Text.Json;
using Core.Domain;
using Core.Storage;
using Xunit;

namespace Core.Tests;

public class FileOrderStoreTests
{
    // Кожен тест працює у власному тимчасовому каталозі, який видаляється у finally.
    private static (string Dir, string File) NewTempFile()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"crossapp-{Guid.NewGuid():N}");
        return (dir, Path.Combine(dir, "nested", "orders.json"));
    }

    private static void Cleanup(string dir)
    {
        if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
    }

    private static Order MakeOrder(string id, string name = "Ноутбук Lenovo", decimal price = 100m, int qty = 2)
    {
        var order = Order.Create(id, "C-1");
        order.AddLine("P-1", name, price, qty);
        return order;
    }

    [Fact]
    public void Add_CreatesMissingDirectories_AndFile()
    {
        var (dir, file) = NewTempFile();
        try
        {
            new FileOrderStore(file).Add(MakeOrder("O-1"));

            Assert.True(File.Exists(file));
        }
        finally { Cleanup(dir); }
    }

    [Fact]
    public void RoundTrip_NewStoreInstance_ReadsSameData()
    {
        var (dir, file) = NewTempFile();
        try
        {
            Order original = MakeOrder("O-1");
            original.Confirm();
            new FileOrderStore(file).Add(original);

            Order? loaded = new FileOrderStore(file).GetById("O-1");

            Assert.NotNull(loaded);
            Assert.Equal(OrderStatus.Confirmed, loaded.Status);
            Assert.Equal(200m, loaded.Total);
            Assert.Equal("Ноутбук Lenovo", loaded.Lines[0].Name);
        }
        finally { Cleanup(dir); }
    }

    [Fact]
    public void File_ContainsReadableCyrillic_AndCamelCaseKeys()
    {
        var (dir, file) = NewTempFile();
        try
        {
            new FileOrderStore(file).Add(MakeOrder("O-1"));

            string text = File.ReadAllText(file);

            Assert.Contains("Ноутбук Lenovo", text);          // кирилиця не як \uXXXX
            Assert.DoesNotContain("\\u04", text);
            Assert.Contains("\"customerId\"", text);          // camelCase
            Assert.Contains("\"productId\"", text);
            Assert.DoesNotContain("\"CustomerId\"", text);
        }
        finally { Cleanup(dir); }
    }

    [Fact]
    public void Flush_LeavesNoTmpFile()
    {
        var (dir, file) = NewTempFile();
        try
        {
            var store = new FileOrderStore(file);
            store.Add(MakeOrder("O-1"));
            store.Add(MakeOrder("O-2"));

            Assert.False(File.Exists(file + ".tmp"));
        }
        finally { Cleanup(dir); }
    }

    [Fact]
    public void MissingFile_ReturnsEmptyList_AndDoesNotCreateFile()
    {
        var (dir, file) = NewTempFile();
        try
        {
            Assert.Empty(new FileOrderStore(file).List());
            Assert.False(File.Exists(file));
        }
        finally { Cleanup(dir); }
    }

    [Fact]
    public void CorruptedJson_ThrowsStorageException_WithInnerJsonException()
    {
        var (dir, file) = NewTempFile();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, "{ це не json");

            var ex = Assert.Throws<StorageException>(() => new FileOrderStore(file).List());

            Assert.IsAssignableFrom<JsonException>(ex.InnerException);
            Assert.Contains("orders.json", ex.Message);
        }
        finally { Cleanup(dir); }
    }

    [Fact]
    public void InvalidOrderInFile_ThrowsStorageException_WithInnerArgumentException()
    {
        var (dir, file) = NewTempFile();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, """
                [ { "id": "O-1", "customerId": "C-1", "status": "Draft",
                    "lines": [ { "productId": "P-1", "name": "Товар", "price": -5, "quantity": 1 } ] } ]
                """);

            var ex = Assert.Throws<StorageException>(() => new FileOrderStore(file).List());

            Assert.IsAssignableFrom<ArgumentException>(ex.InnerException);
        }
        finally { Cleanup(dir); }
    }

    [Fact]
    public void OldPascalCaseFile_IsStillReadable()
    {
        var (dir, file) = NewTempFile();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, """
                [ { "Id": "O-1", "CustomerId": "C-1", "Status": "Draft",
                    "Lines": [ { "ProductId": "P-1", "Name": "Товар", "Price": 10, "Quantity": 2 } ] } ]
                """);

            Order? order = new FileOrderStore(file).GetById("O-1");

            Assert.NotNull(order);
            Assert.Equal(20m, order.Total);
        }
        finally { Cleanup(dir); }
    }

    [Fact]
    public void ReplaceAll_ReplacesWholeFile()
    {
        var (dir, file) = NewTempFile();
        try
        {
            new FileOrderStore(file).Add(MakeOrder("OLD-1"));

            new FileOrderStore(file).ReplaceAll([MakeOrder("NEW-1"), MakeOrder("NEW-2")]);

            var ids = new FileOrderStore(file).List().Select(o => o.Id).ToList();
            Assert.Equal(new[] { "NEW-1", "NEW-2" }, ids);
        }
        finally { Cleanup(dir); }
    }

    [Fact]
    public void ReplaceAll_IgnoresCorruptedOldContent()
    {
        var (dir, file) = NewTempFile();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, "{ зіпсовано");

            new FileOrderStore(file).ReplaceAll([MakeOrder("NEW-1")]);

            Assert.Single(new FileOrderStore(file).List());
        }
        finally { Cleanup(dir); }
    }

    [Fact]
    public void ReplaceAll_DuplicateIds_ThrowsDomainException_AndKeepsFileUntouched()
    {
        var (dir, file) = NewTempFile();
        try
        {
            new FileOrderStore(file).Add(MakeOrder("KEEP-1"));

            Assert.Throws<DomainException>(() =>
                new FileOrderStore(file).ReplaceAll([MakeOrder("D-1"), MakeOrder("d-1")]));

            Assert.Equal("KEEP-1", new FileOrderStore(file).List().Single().Id);
        }
        finally { Cleanup(dir); }
    }

    [Fact]
    public void Remove_IsPersisted()
    {
        var (dir, file) = NewTempFile();
        try
        {
            var store = new FileOrderStore(file);
            store.Add(MakeOrder("O-1"));
            store.Add(MakeOrder("O-2"));

            Assert.True(store.Remove("O-1"));

            Assert.Equal("O-2", new FileOrderStore(file).List().Single().Id);
        }
        finally { Cleanup(dir); }
    }
}