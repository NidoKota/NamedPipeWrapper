using System;
using System.Threading;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEngine;
#endif 

namespace NamedPipeWrapper
{
    public static class Utility
    {
        public static void DebugLog(string message)
        {
#if UNITY_EDITOR
            Debug.Log(message);
#else 
            Console.WriteLine(message);
#endif
        }
        
        public static Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            return task.IsCompleted
                ? task
                : task.ContinueWith(
                    completedTask => completedTask.GetAwaiter().GetResult(),
                    cancellationToken,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }
        
        public static Task WithCancellation(this Task task, CancellationToken cancellationToken)
        {
            return task.IsCompleted
                ? task
                : task.ContinueWith(
                    completedTask => completedTask.GetAwaiter().GetResult(),
                    cancellationToken,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }
    }
}