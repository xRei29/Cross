/// <summary>
/// Синхронна реалізація IProgress&lt;int&gt;. Стандартний Progress&lt;T&gt; у консолі викликає обробник
/// у пулі потоків (немає SynchronizationContext), тому рядки могли б з'являтись після завершення програми.
/// </summary>
public sealed class ConsoleProgress : IProgress<int>
{
    private bool _printed;

    public void Report(int percent)
    {
        _printed = true;
        Console.Write($"\rПрогрес: {percent,3}%");
    }

    /// <summary>Завершує рядок прогресу переходом на новий рядок (лише якщо щось друкувалось).</summary>
    public void Finish()
    {
        if (!_printed) return;
        Console.WriteLine();
        _printed = false;
    }
}