using System.Collections.Generic;
using System.Fabric;
using Funq;
using ServiceStack.Configuration;
using ServiceStack.Webhooks.Azure.Settings;
using ServiceStack.Webhooks.Azure.Worker;

namespace ServiceStack.Webhooks.EventRelayStatelessService
{
    public class WorkerService : AzureWorkerServiceEntryPoint
    {
        private List<WorkerEntryPoint> workers;

        public WorkerService(StatelessServiceContext context) : base(context)
        {
        }

        protected override IEnumerable<WorkerEntryPoint> Workers => workers;

        public override void Configure(Container container)
        {
            base.Configure(container);

            container.Register<IAppSettings>(new FabricAppSettings(Context));
            container.Register(new EventRelayWorker(container));

            workers = new List<WorkerEntryPoint>
            {
                container.Resolve<EventRelayWorker>()
            };
        }
    }
}