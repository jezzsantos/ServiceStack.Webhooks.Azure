using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Webhooks.Azure.Queue;
using ServiceStack.Webhooks.Clients;
using ServiceStack.Webhooks.Relays;
using ServiceStack.Webhooks.ServiceModel.Types;

namespace ServiceStack.Webhooks.Azure.Worker
{
    /// <summary>
    ///     Provides a queue processor for relaying webhook events from queue to subscribers
    /// </summary>
    public class EventRelayQueueProcessor : BaseQueueProcessor<WebhookEvent>
    {
        public const int DefaultServiceClientRetries = 3;
        public const int DefaultServiceClientTimeoutSeconds = 60;
        public const int DefaultPollingIntervalSeconds = 5;
        public const int DefaultSubscriptionCacheTimeoutSeconds = 60;
        public const string DefaultTargetQueueName = AzureQueueEventSink.DefaultQueueName;
        public const string DefaultUnhandledQueueName = "unhandled" + AzureQueueEventSink.DefaultQueueName;
        public const string PollingIntervalSettingName = "EventRelayQueueProcessor.Polling.Interval.Seconds";
        public const string AzureConnectionStringSettingName = "EventRelayQueueProcessor.ConnectionString";
        public const string TargetQueueNameSettingName = "EventRelayQueueProcessor.TargetQueue.Name";
        public const string UnhandledQueneNameStringSettingName = "EventRelayQueueProcessor.UnhandledQueue.Name";
        public const string SeviceClientRetriesSettingName = "EventRelayQueueProcessor.ServiceClient.Retries";
        public const string DefaultSeviceClientTimeoutSettingName = "EventRelayQueueProcessor.ServiceClient.Timeout.Seconds";
        public const string DefaultSubscriptionCacheTimeoutSettingsName = "EventRelayQueueProcessor.SubscriptionCache.Timeout.Seconds";
        private readonly ILog logger = LogManager.GetLogger(typeof(EventRelayQueueProcessor));

        private int pollingInterval;
        private IAzureQueueStorage<WebhookEvent> targetQueue;
        private IAzureQueueStorage<IUnhandledMessage> unhandledQueue;

        public EventRelayQueueProcessor()
        {
            ServiceClientRetries = DefaultServiceClientRetries;
            SeviceClientTimeoutSeconds = DefaultServiceClientTimeoutSeconds;
            pollingInterval = DefaultPollingIntervalSeconds;
            ConnectionString = AzureStorage.AzureEmulatorConnectionString;
        }

        public EventRelayQueueProcessor(IAppSettings settings) : this()
        {
            Guard.AgainstNull(() => settings, settings);

            ServiceClientRetries = settings.Get(SeviceClientRetriesSettingName, DefaultServiceClientRetries);
            SeviceClientTimeoutSeconds = settings.Get(DefaultSeviceClientTimeoutSettingName, DefaultServiceClientTimeoutSeconds);
            pollingInterval = settings.Get(PollingIntervalSettingName, DefaultPollingIntervalSeconds);
            ConnectionString = settings.Get(AzureConnectionStringSettingName, AzureStorage.AzureEmulatorConnectionString);
            TargetQueueName = settings.Get(TargetQueueNameSettingName, DefaultTargetQueueName);
            UnhandledQueueName = settings.Get(UnhandledQueneNameStringSettingName, DefaultUnhandledQueueName);
        }

        /// <summary>
        ///     For testing only
        /// </summary>
        public override IAzureQueueStorage<WebhookEvent> TargetQueue
        {
            get { return targetQueue ?? (targetQueue = new AzureQueueStorage<WebhookEvent>(ConnectionString, TargetQueueName)); }
            set { targetQueue = value; }
        }

        /// <summary>
        ///     For testing only
        /// </summary>
        public override IAzureQueueStorage<IUnhandledMessage> UnhandledQueue
        {
            get { return unhandledQueue ?? (unhandledQueue = new AzureQueueStorage<IUnhandledMessage>(ConnectionString, UnhandledQueueName)); }
            set { unhandledQueue = value; }
        }

        public IEventSubscriptionCache SubscriptionCache { get; set; }

        public IEventServiceClient ServiceClient { get; set; }

        public ISubscriptionService SubscriptionService { get; set; }

        public override int IntervalSeconds
        {
            get { return pollingInterval; }
            set { pollingInterval = value; }
        }

        public string ConnectionString { get; set; }

        public string TargetQueueName { get; set; }

        public string UnhandledQueueName { get; set; }

        public int ServiceClientRetries { get; set; }

        public int SeviceClientTimeoutSeconds { get; set; }

        public override bool ProcessMessage(WebhookEvent webhookEvent)
        {
            logger.InfoFormat("[ServiceStack.Webhooks.Azure.Worker.EventRelayQueueProcessor] Processing webhook event {0}", webhookEvent.ToJson());

            var subscriptions = SubscriptionCache.GetAll(webhookEvent.EventName);
            var results = new List<SubscriptionDeliveryResult>();
            subscriptions.ForEach(sub =>
            {
                var result = NotifySubscription(sub, webhookEvent);
                if (result != null)
                {
                    results.Add(result);
                }
            });

            if (results.Any())
            {
                SubscriptionService.UpdateResults(results);
            }

            return true;
        }

        private SubscriptionDeliveryResult NotifySubscription(SubscriptionRelayConfig subscription, WebhookEvent webhookEvent)
        {
            ServiceClient.Retries = ServiceClientRetries;
            ServiceClient.Timeout = TimeSpan.FromSeconds(SeviceClientTimeoutSeconds);
            return ServiceClient.Relay(subscription, webhookEvent);
        }
    }
}