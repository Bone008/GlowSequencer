using System;
using System.Threading.Tasks;

namespace GlowSequencer.Util
{
    public static class AsyncUtil
    {
        /// <summary>
        /// Allows "fire and forget" behavior of an asynchronous task and posts exceptions raised by the task to the main dispatcher.
        /// The method will return immediately, without blocking or awaiting the task.
        /// </summary>
        public static void Forget(this Task task)
        {
            task.ContinueWith(t =>
            {
                Exception e = t.Exception.InnerException;
                var _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => { throw new Exception("Unhandled exception in fire-and-forget task.", e); }));
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
