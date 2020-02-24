using Funq;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Webhooks.Clients;
using ServiceStack.Webhooks.Relays;
using ServiceStack.Webhooks.Relays.Clients;

namespace ServiceStack.Webhooks.Azure.Worker
{
    /// <summary>
    ///     Provides a worker for relaying webhook events from queue to subscribers
    /// </summary>
    public class EventRelayWorker : WorkerEntryPoint<EventRelayQueueProcessor>
    {
        public EventRelayWorker(Container container)
        {
            Guard.AgainstNull(() => container, container);

            var appSettings = container.Resolve<IAppSettings>();

            if (!container.Exists<ICacheClient>())
            {
                container.RegisterAutoWiredAs<MemoryCacheClient, ICacheClient>();
            }

            container.RegisterAutoWiredAs<EventServiceClientFactory, IEventServiceClientFactory>();
            container.Register<ISubscriptionService>(x => new SubscriptionServiceClient(appSettings)
            {
                ServiceClientFactory = x.Resolve<IEventServiceClientFactory>()
            });
            container.RegisterAutoWiredAs<CacheClientEventSubscriptionCache, IEventSubscriptionCache>();
            container.Register<IEventSubscriptionCache>(x => new CacheClientEventSubscriptionCache
            {
                ExpiryTimeSeconds = appSettings.Get(EventRelayQueueProcessor.DefaultSubscriptionCacheTimeoutSettingsName, EventRelayQueueProcessor.DefaultSubscriptionCacheTimeoutSeconds),
                SubscriptionService = x.Resolve<ISubscriptionService>(),
                CacheClient = x.Resolve<ICacheClient>()
            });

            container.RegisterAutoWiredAs<EventServiceClient, IEventServiceClient>();
            container.Register(x => new EventRelayQueueProcessor(appSettings)
            {
                ServiceClient = x.Resolve<IEventServiceClient>(),
                SubscriptionCache = x.Resolve<IEventSubscriptionCache>(),
                SubscriptionService = x.Resolve<ISubscriptionService>()
            });

            Processor = container.Resolve<EventRelayQueueProcessor>();
        }
    }
}