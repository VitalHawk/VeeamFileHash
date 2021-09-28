using System;
using System.Collections.Concurrent;
using System.Threading;

namespace VeeamFileHash
{
    public class ThreadPool
    {
        private ConcurrentDictionary<int, Thread> threads;

        public void Start(int threadCount, Action threadFunc, Action<Exception> errorHandler)
        {
            Finish();

            threads = new ConcurrentDictionary<int, Thread>();
            for (int i = 0; i < threadCount; i++)
            {
                var thread = new Thread(
                    () =>
                    {
                        try
                        {
                            threadFunc.Invoke();
                        }
                        catch (ThreadAbortException)
                        {
                            // ignore
                        }
                        catch (Exception e)
                        {
                            errorHandler(e);
                        }
                    });

                threads[i] = thread;
            }

            foreach (var key in threads.Keys)
                if (threads.TryGetValue(key, out Thread thread))
                    thread.Start();
        }

        public void Finish()
        {
            if (threads == null)
                return;

            foreach (var key in threads.Keys)
            {
                if (threads.TryRemove(key, out Thread thread))
                {
                    try
                    {
                        thread?.Abort();
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
        }
    }
}
