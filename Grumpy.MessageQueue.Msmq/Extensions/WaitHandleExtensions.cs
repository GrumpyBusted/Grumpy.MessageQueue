using System.Threading;
using System.Threading.Tasks;

namespace Grumpy.MessageQueue.Msmq.Extensions
{
    internal static class WaitHandleExtensions
    {
        public static Task ToTask(this WaitHandle handle, CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<object>();

            cancellationToken.Register(() => { taskCompletionSource.TrySetCanceled(); });

            var localVariableInitLock = new object();

            lock (localVariableInitLock)
            {
                RegisteredWaitHandle[] callbackHandle = {null};

                callbackHandle[0] = ThreadPool.RegisterWaitForSingleObject(handle, (state, timedOut) =>
                {
                    taskCompletionSource.TrySetResult(null);
                    lock (localVariableInitLock)
                    {
                        callbackHandle[0].Unregister(null); 
                    }
                }, null, Timeout.Infinite, true);
            }

            return taskCompletionSource.Task;
        }
    }
}