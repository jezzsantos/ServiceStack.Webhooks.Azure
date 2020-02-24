#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Funq;
using Microsoft.WindowsAzure.ServiceRuntime;
using ServiceStack.Logging;

namespace ServiceStack.Webhooks.Azure.Worker
{
    /// <summary>
    ///     Provides a base class for Azure role instances that enables us to host more than one
    ///     <see cref="WorkerEntryPoint" /> in a single 'worker role'.
    /// </summary>
    public abstract class AzureWorkerRoleEntryPoint : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly Container container;
        private readonly ILog logger = LogManager.GetLogger(typeof(AzureWorkerRoleEntryPoint));
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        protected bool Stopped;

        protected AzureWorkerRoleEntryPoint()
        {
            container = new Container();
        }

        protected abstract IEnumerable<WorkerEntryPoint> Workers { get; }

        public override bool OnStart()
        {
            Stopped = false;
            var result = base.OnStart();

            Configure(container);

            return result;
        }

        // ReSharper disable once ParameterHidesMember
        public virtual void Configure(Container container)
        {
        }

        public override void Run()
        {
            var workerName = GetType().FullName;
            logger.Info("[ServiceStack.Webhooks.Azure.Worker.AzureWorkerRoleEntryPoint] {0}.Running".Fmt(workerName));

            try
            {
                WorkerRunner.RunOnceAsync(logger, Workers.ToList(), cancellationTokenSource.Token)
                    .Wait(cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                logger.Info("[ServiceStack.Webhooks.Azure.Worker.AzureWorkerRoleEntryPoint] {0}.Interrupted".Fmt(workerName));
                // Ignore the exception to allow worker to shutdown gracefully
            }
            finally
            {
                runCompleteEvent.Set();
            }
        }

        public override void OnStop()
        {
            var workerName = GetType().FullName;
            logger.Info("[ServiceStack.Webhooks.Azure.Worker.AzureWorkerRoleEntryPoint] {0}.OnStop Stopping".Fmt(workerName));

            cancellationTokenSource.Cancel();
            runCompleteEvent.WaitOne();

            base.OnStop();
            Stopped = true;

            logger.Info("[ServiceStack.Webhooks.Azure.Worker.AzureWorkerRoleEntryPoint] {0}.OnStop Stopped".Fmt(workerName));
        }
    }
}
#endif