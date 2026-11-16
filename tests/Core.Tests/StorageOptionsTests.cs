using Core.Configuration;
using Xunit;

namespace Core.Tests;

public class StorageOptionsTests
{
    private static string NewTempDir() =>
        Path.Combine(Path.GetTempPath(), $"crossapp-{Guid.NewGuid():N}");

    [Fact]
    public void Defaults_AreDataAndOrdersJson()
    {
        var options = new AppOptions();

        Assert.Equal("data", options.Storage.DataPath);
        Assert.Equal("orders.json", options.Storage.FileName);
        Assert.Equal("UAH", options.Orders.Currency);
        Assert.Equal("Information", options.Logging.MinimumLevel);
    }

    [Fact]
    public void ResolveFullPath_RelativeDataPath_IsCombinedWithBaseDir_AndDirectoryIsCreated()
    {
        string baseDir = NewTempDir();
        try
        {
            var storage = new StorageOptions { DataPath = "data", FileName = "orders.json" };

            string full = storage.ResolveFullPath(baseDir);

            Assert.Equal(Path.GetFullPath(Path.Combine(baseDir, "data", "orders.json")), full);
            Assert.True(Directory.Exists(Path.Combine(baseDir, "data")));
        }
        finally
        {
            if (Directory.Exists(baseDir)) Directory.Delete(baseDir, recursive: true);
        }
    }

    [Fact]
    public void ResolveFullPath_RootedDataPath_IsUsedAsIs()
    {
        string baseDir = NewTempDir();
        string rooted = Path.Combine(NewTempDir(), "elsewhere");   // абсолютний шлях, збудований динамічно
        try
        {
            var storage = new StorageOptions { DataPath = rooted, FileName = "orders.json" };

            string full = storage.ResolveFullPath(baseDir);

            Assert.Equal(Path.GetFullPath(Path.Combine(rooted, "orders.json")), full);
            Assert.True(Directory.Exists(rooted));
            Assert.False(Directory.Exists(baseDir));              // baseDir не використовувався
        }
        finally
        {
            string? parent = Path.GetDirectoryName(rooted);
            if (parent is not null && Directory.Exists(parent)) Directory.Delete(parent, recursive: true);
        }
    }

    [Fact]
    public void ResolveFullPath_BlankFileName_Throws()
    {
        var storage = new StorageOptions { DataPath = "data", FileName = "  " };

        Assert.Throws<InvalidOperationException>(() => storage.ResolveFullPath(NewTempDir()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithDataPathOverride_BlankValue_KeepsOriginal(string? overrideValue)
    {
        var storage = new StorageOptions { DataPath = "data", FileName = "orders.json" };

        StorageOptions result = storage.WithDataPathOverride(overrideValue);

        Assert.Equal("data", result.DataPath);
        Assert.Equal("orders.json", result.FileName);
    }

    [Fact]
    public void WithDataPathOverride_Value_ReplacesDataPath_AndKeepsFileName()
    {
        var storage = new StorageOptions { DataPath = "data", FileName = "orders.json" };

        StorageOptions result = storage.WithDataPathOverride("tmp-data");

        Assert.Equal("tmp-data", result.DataPath);
        Assert.Equal("orders.json", result.FileName);
    }
}