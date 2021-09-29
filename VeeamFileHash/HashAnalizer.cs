using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace VeeamFileHash
{
    public class HashAnalizer
    {
        private int HashBlockSize { get; }
        private string FileName { get; }
        
        private static OrderedActor GetOrderedActor() => new OrderedActor();
        private static ThreadPool GetThreadPool() => new ThreadPool();
        private Configuration Configuration { get; } = new Configuration();

        private int OptimalReadBlockSize => Math.Max(Configuration.OptimalReadBlockSize, HashBlockSize);
        private int OptimalThreadCount => Configuration.OptimalThreadCount;
        private long FileSize => new FileInfo(FileName).Length;
        private long FileReadBlockSize => Math.Min(HashBlockSize * (OptimalReadBlockSize / HashBlockSize), FileSize);

        public HashAnalizer(string fileName, int hashBlockSize)
        {
            FileName = fileName;
            HashBlockSize = hashBlockSize;
        }

        public void HashIt(Action<int, byte[]> hashBlockResultHandler, Action<Exception> errorHandler)
        {
            try
            {
                var cancellationTokenSource = new CancellationTokenSource();
                var readingBlocks = new BlockingCollection<int>(OptimalThreadCount);

                var orderedActor = GetOrderedActor();
                var threadPool = GetThreadPool();
                threadPool.Start(OptimalThreadCount,
                    () => TreadFunction(readingBlocks, orderedActor, hashBlockResultHandler),
                    ex =>
                    {
                        cancellationTokenSource.Cancel();
                        threadPool.Finish();
                        errorHandler(ex);
                    });

                var fileReadParts = FileSize / FileReadBlockSize;
                for (var blockNum = 0; blockNum < fileReadParts && !cancellationTokenSource.IsCancellationRequested; blockNum++)
                {
                    try
                    {
                        readingBlocks.Add(blockNum, cancellationTokenSource.Token);
                    }
                    catch
                    {
                        // ignored
                    }
                }

                readingBlocks.CompleteAdding();
            }
            catch (Exception ex)
            {
                errorHandler(ex);
            }
        }

        private void TreadFunction(
            BlockingCollection<int> readingBlocks,
            OrderedActor orderedActor,
            Action<int, byte[]> hashBlockResultHandler)
        {
            using (var fileStream = File.OpenRead(FileName))
                using (var sha256 = SHA256.Create())
                {
                    var canRead = true;
                    do
                    {
                        try
                        {
                            var readBlockNum = readingBlocks.Take();
                            var buf = new byte[FileReadBlockSize];
                            var readBlockSize = (int)FileReadBlockSize;
                            fileStream.Seek(readBlockNum * FileReadBlockSize, SeekOrigin.Begin);
                            fileStream.Read(buf, 0, readBlockSize);
                            var hashBlocksReadCount = readBlockSize / HashBlockSize;
                            for (var idx = 0; idx < hashBlocksReadCount; idx++)
                            {
                                var hash = sha256.ComputeHash(buf, HashBlockSize * idx, HashBlockSize);
                                var hashIdx = hashBlocksReadCount * readBlockNum + idx;
                                orderedActor.DoAct(hashIdx, () => hashBlockResultHandler(hashIdx, hash));
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            canRead = false;
                        }
                        catch (OperationCanceledException)
                        {
                            canRead = false;
                        }
                    }
                    while (canRead);
                }
        }
    }
}