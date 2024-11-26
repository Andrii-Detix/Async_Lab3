Func<int, Task<int>> func = (item) =>
{
    Task<int> resultTask = Task.Run(() =>
    {
        int delay = item * 2000;
        
        Task.Delay(delay).Wait();

        Console.WriteLine(item);
        
        if (item % 2 == 0)
            return item * 2;

        return item;
    });
    return resultTask;
};

var cancellationSource = new CancellationTokenSource();
var cancellationToken = cancellationSource.Token;
cancellationSource.CancelAfter(TimeSpan.FromSeconds(5));

var a = MapAsync([1, 2, 3, 4], func, cancellationToken);


a.ContinueWith(result =>
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

static Task<T[]> MapAsync<T>(T[] arr, Func<T, Task<T>> func, CancellationToken cancellationToken = default)
{
    var t = Task.Run(() =>
    {
        int length = arr.Length;

        T[] results = new T[length];
        Task[] tasks = new Task[length];

        for (int i = 0; i < length; i++)
        {
            int index = i;
            tasks[index] = func(arr[index]).ContinueWith(task =>
            {
                if (task.IsFaulted)
                    throw task.Exception.GetBaseException();
                
                results[index] = task.Result;
            });
        }

        int checkInterval = 200;
        while (!tasks.All(t => t.IsCompleted))
        {
            if(cancellationToken.IsCancellationRequested)
                cancellationToken.ThrowIfCancellationRequested();
            
            Task.Delay(checkInterval).Wait();
        }
        
        return results;
    });
    return t;
}
