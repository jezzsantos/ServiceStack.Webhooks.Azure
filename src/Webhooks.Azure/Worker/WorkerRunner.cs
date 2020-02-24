using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Logging;

namespace ServiceStack.Webhooks.Azure.Worker
{
    internal static class WorkerRunner
    {
        internal static async Task RunContinuouslyAsync(ILog logger, IList<WorkerEntryPoint> workers, CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested) await RunOnceAsync(logger, workers, cancellation);
        }

        internal static async Task RunOnceAsync(ILog logger, IList<WorkerEntryPoint> workers, CancellationToken cancellation)
        {
            await Task.CompletedTask;

            var workerName = typeof(WorkerRunner).FullName;
            logger.Info("[ServiceStack.Webhooks.Azure.Worker.WorkerRunner] {0}.RunOnceAsync Running all workers".Fmt(workerName));

            if (workers == null || !workers.Any())
            {
                logger.Info("[ServiceStack.Webhooks.Azure.Worker.WorkerRunner] {0}.RunOnceAsync No Workers to run".Fmt(workerName));
            }
            else
            {
                var options = new ParallelOptions
                {
                    CancellationToken = cancellation
                };
                try
                {
                    var exceptions = new ConcurrentQueue<Exception>();
                    Parallel.ForEach(workers, options, worker =>
                    {
                        logger.Info("[ServiceStack.Webhooks.Azure.Worker.WorkerRunner] {0}.RunOnceAsync Running worker '{1}'".Fmt(workerName, worker.GetType().FullName));

                        try
                        {
                            worker.Run(cancellation);
                        }
                        catch (Exception ex)
                        {
                            exceptions.Enqueue(ex);

                            // Continue all other tasks
                        }
                    });

                    if (exceptions.Any())
                    {
                        exceptions.Each(taskException =>
                        {
                            if (!(taskException is OperationCanceledException))
                            {
                                logger.Fatal("[ServiceStack.Webhooks.Azure.Worker.WorkerRunner] {0}.RunOnceAsync Worker crashed".Fmt(workerName), taskException);
                            }
                        });
                    }
                }
                catch (OperationCanceledException)
                {
                    logger.Info("[ServiceStack.Webhooks.Azure.Worker.WorkerRunner] {0}.RunOnceAsync Worker ended because of cancellation".Fmt(workerName));
                }
                catch (Exception ex)
                {
                    logger.Fatal("[ServiceStack.Webhooks.Azure.Worker.WorkerRunner] {0}.RunOnceAsync Worker crashed".Fmt(workerName), ex);
                    throw;
                }
            }
        }
    }
}