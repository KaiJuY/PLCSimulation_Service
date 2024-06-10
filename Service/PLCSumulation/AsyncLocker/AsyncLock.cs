using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PLCSumulation.AsyncLocker
{
    // 這個類別是一個非同步鎖,可以用來避免多個執行緒同時存取共用資源
    //用法僅需呼叫 LockAsync 方法,並在 await 之後的區塊中存取共用資源
    //當區塊執行完畢後,鎖會自動釋放
    //ex:
    //using (await asyncLock.LockAsync())
    //{
    //    存取共用資源的程式碼
    //}
    public class AsyncLock
    {
        private static readonly Queue<TaskCompletionSource<IDisposable>> _waiters = new Queue<TaskCompletionSource<IDisposable>>();
        private static bool _isLocked = false;

        public AsyncLock()
        {
        }

        public Task<IDisposable> LockAsync()
        {
            var tcs = new TaskCompletionSource<IDisposable>();

            lock (_waiters)
            {
                if (_isLocked)
                {
                    _waiters.Enqueue(tcs);
                }
                else
                {
                    _isLocked = true;
                    tcs.SetResult(new DisposableAction(() => _isLocked = false));
                }
            }

            return tcs.Task;
        }

        private class DisposableAction : IDisposable
        {
            private readonly Action _action;

            public DisposableAction(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                _action();

                lock (_waiters)
                {
                    if (_waiters.Count > 0)
                    {
                        var nextWaiter = _waiters.Dequeue();
                        nextWaiter.SetResult(new DisposableAction(() => _isLocked = false));
                    }
                }
            }
        }
    }
}
