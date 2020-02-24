#if NETSTANDARD
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Funq;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceStack.Logging;

namespace ServiceStack.Webhooks.Azure.Worker
{
    /// <summary>
    ///     Provides a base class for Azure Service fabric stateless service instances that enables us to host more than one
    ///     <see cref="WorkerEntryPoint" /> in a single 'cluster'.
    /// </summary>
    public abstract class AzureWorkerServiceEntryPoint : StatelessService
    {
        private readonly Container container;
        private readonly ILog logger = LogManager.GetLogger(typeof(AzureWorkerServiceEntryPoint));

        protected AzureWorkerServiceEntryPoint(StatelessServiceContext context)
            : base(context)
        {
            container = new Container();
        }

        protected abstract IEnumerable<WorkerEntryPoint> Workers { get; }

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[0];
        }

        protected override async Task RunAsync(CancellationToken cancellation)
        {
            Configure(container);

            var workerName = GetType().FullName;
            logger.Info("[ServiceStack.Webhooks.Azure.Worker.AzureWorkerServiceEntryPoint] {0}.Running".Fmt(workerName));

            try
            {
                await WorkerRunner.RunContinuouslyAsync(logger, Workers.ToList(), cancellation);
            }
            catch (OperationCanceledException)
            {
                logger.Info("[ServiceStack.Webhooks.Azure.Worker.AzureWorkerServiceEntryPoint] {0}.Interrupted".Fmt(workerName));
                // Ignore the exception to allow worker to shutdown gracefully
            }
        }

        // ReSharper disable once ParameterHidesMember
        public virtual void Configure(Container container)
        {
        }
    }
}
#endif