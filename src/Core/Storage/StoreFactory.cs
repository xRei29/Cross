using Core.Abstractions;

namespace Core.Storage;

public static class StoreFactory
{
    public static IOrderStore Create(string[] args, string dataPath)
    {
        bool useFile = args.Contains("--file");
        bool useCache = args.Contains("--cache");

        IOrderStore store = useFile
            ? new FileOrderStore(dataPath)
            : new InMemoryOrderStore(SampleData.Orders());

        return useCache ? new CachingOrderStore(store) : store;
    }
}