using System.Runtime.CompilerServices;

namespace GameTools.GameTask {

    public interface IAwaiter : ICriticalNotifyCompletion
    {
        bool IsCompleted { get; }
        void GetResult();
    }
    public interface IAwaiter<out T> : ICriticalNotifyCompletion
    {
        bool IsCompleted { get; }
        T GetResult();
    }
}