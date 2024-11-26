Func<int, CancellationToken, Task<int>> factorial = (item, cancellationToken) =>
{
    Task<int> resultTask = Task.Run(() =>
    {
        int result = 1;
        for (int i = 1; i <= item; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                throw new Exception($"item {item} is cancelled");

            result *= i;
            Task.Delay(2000).Wait();
        }

        Console.WriteLine($"Factorial of {item} is {result}");

        return result;
    });
    return resultTask;
};

var cancellationSource = new CancellationTokenSource();
var cancellationToken = cancellationSource.Token;
cancellationSource.CancelAfter(TimeSpan.FromSeconds(4));

var task = MapAsync([1, 2, 3, 4], factorial, cancellationToken);

task.ContinueWith(result =>
{
    if (result.IsFaulted)
    {
        var exceptions = result.Exception.Flatten().InnerExceptions;
        foreach (var ex in exceptions)
            Console.WriteLine(ex.GetBaseException().Message);
    }
    else
        foreach (var item in result.Result)
        {
            Console.WriteLine($"Result: {item}");
        }
}).Wait();

Task.Delay(TimeSpan.FromSeconds(5)).Wait();

static Task<T[]> MapAsync<T>(T[] arr, Func<T, CancellationToken, Task<T>> func,
    CancellationToken cancellationToken = default)
{
    var t = Task.Run(() =>
    {
        int length = arr.Length;

        T[] results = new T[length];
        Task[] tasks = new Task[length];

        for (int i = 0; i < length; i++)
        {
            int index = i;
            tasks[index] = func(arr[index], cancellationToken).ContinueWith(task =>
            {
                if (task.IsFaulted)
                    throw task.Exception.GetBaseException();

                results[index] = task.Result;
            });
        }

        Task.WaitAll(tasks);
        
        return results;
    });
    return t;
}