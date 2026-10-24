# GameTools GameTask

GameTask is a lightweight async/await library for Unity, designed to reduce memory allocations and GC overhead.

## Write async methods

Use `GameTask` for operations without a result and `GameTask<T>` for operations that return a value. Await them with normal `async`/`await` syntax, and catch failures around the await.

```csharp
using System;
using GameTools.GameTask;
using UnityEngine;

public sealed class GameTaskExample : MonoBehaviour
{
    private async void Start()
    {
        try
        {
            await RunAsync();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static async GameTask RunAsync()
    {
        int value = await GetValueAsync(42);
        Debug.Log(value);
    }

    private static async GameTask<int> GetValueAsync(int value)
    {
        await GameTask.UpdateAsync();
        return value;
    }
}
```

## Wait for Unity callbacks

| Method | Function |
| --- | --- |
| `await GameTask.UpdateAsync()` | Resume after Update callbacks. |
| `await GameTask.FixedUpdateAsync()` | Resume after FixedUpdate callbacks. |
| `await GameTask.LateUpdateAsync()` | Resume after LateUpdate callbacks. |

Depending on when the await starts, execution may resume later in the same frame or in a later frame. These methods do not guarantee a one-frame delay.

## Wait for multiple tasks

Inside an async method in the example above:

```csharp
// Wait for operations without return values.
await GameTask.WhenAll(RunAsync(), RunAsync());

// Collect return values in input order.
int[] values = await GameTask<int>.WhenAll(
    GetValueAsync(10),
    GetValueAsync(20));
```

Operations start when called, so their waits can overlap. If any task fails, `WhenAll` waits for all inputs before rethrowing the first exception in input order.

## Usage rules

**Every `GameTask` or `GameTask<T>` must be awaited exactly once, either directly or through `WhenAll`.**

- Use GameTask on the Unity main thread.
- **Do not await the same task instance more than once**, even after it has completed. Do not share it between awaiters.
- **Do not leave a task unawaited or discard it for fire-and-forget use.** It still runs, but after completion it is not returned to the pool, and its exceptions are not automatically reported.
- Pass distinct tasks to `WhenAll`; do not await its inputs separately. Await the task returned by `WhenAll` as well.
- There is no built-in cancellation. Destroying or disabling a GameObject does not stop pending work; check that scene objects are still valid after awaits before accessing them.
