using System;
using System.Threading.Tasks;

namespace QuickDictionary.Utils;

public static class TaskUtils
{
    /// <summary>
    /// Blocks while condition is true or timeout occurs.
    /// </summary>
    /// <param name="condition">The condition that will perpetuate the block.</param>
    /// <param name="frequency">The frequency at which the condition will be check, in milliseconds.</param>
    /// <param name="timeout">Timeout in milliseconds.</param>
    /// <exception cref="TimeoutException"></exception>
    /// <returns>Whether this method exited without waiting for timeout. False if exited due to timeout.</returns>
    public static async Task<bool> WaitWhile(Func<bool> condition, int frequency = 25, int timeout = -1)
    {
        Task waitTask = Task.Run(async () =>
        {
            while (condition()) await Task.Delay(frequency);
        });

        return waitTask == await Task.WhenAny(waitTask, Task.Delay(timeout));
    }

    /// <summary>
    /// Blocks until condition is true or timeout occurs.
    /// </summary>
    /// <param name="condition">The break condition.</param>
    /// <param name="frequency">The frequency at which the condition will be checked.</param>
    /// <param name="timeout">The timeout in milliseconds.</param>
    /// <returns>Whether this method exited without waiting for timeout. False if exited due to timeout.</returns>
    public static async Task<bool> WaitUntil(Func<bool> condition, int frequency = 25, int timeout = -1)
    {
        Task waitTask = Task.Run(async () =>
        {
            while (!condition()) await Task.Delay(frequency);
        });

        return waitTask == await Task.WhenAny(waitTask, Task.Delay(timeout));
    }
}
