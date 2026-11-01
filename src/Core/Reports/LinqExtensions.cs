namespace Core.Reports;

public static class LinqExtensions
{
    public static IReadOnlyList<T> TopBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector, int n) =>
        source.OrderByDescending(keySelector).Take(n).ToList();
}