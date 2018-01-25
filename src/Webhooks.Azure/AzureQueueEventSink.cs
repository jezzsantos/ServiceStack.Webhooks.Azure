using System.Collections.Generic;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Webhooks.Azure.Queue;
using ServiceStack.Webhooks.Azure.Worker;

namespace ServiceStack.Webhooks.Azure
{
    public class AzureQueueEventSink : IEventSink
    {
        public const string DefaultQueueName = "webhookevents";
        public const string AzureConnectionStringSettingName = "AzureQueueEventSink.ConnectionString";
        public const string QueueNameSettingName = "AzureQueueEventSink.Queue.Name";
        private readonly IAppSettings settings;
        private string connectionString;
        private IAzureQueueStorage<WebhookEvent> queueStorage;
        private readonly ILog logger = LogManager.GetLogger(typeof(AzureQueueEventSink));
        
        public AzureQueueEventSink()
        {
            QueueName = DefaultQueueName;

            connectionString = AzureStorage.AzureEmulatorConnectionString;
        }

        public AzureQueueEventSink(IAppSettings settings)
            : this()
        {
            Guard.AgainstNull(() => settings, settings);

            this.settings = settings;

            connectionString = settings.Get(AzureConnectionStringSettingName, AzureStorage.AzureEmulatorConnectionString);
            QueueName = settings.Get(QueueNameSettingName, DefaultQueueName);
        }

        /// <summary>
        ///     For testing only
        /// </summary>
        internal IAzureQueueStorage<WebhookEvent> QueueStorage
        {
            get { return queueStorage ?? (queueStorage = new AzureQueueStorage<WebhookEvent>(ConnectionString, QueueName)); }
            set { queueStorage = value; }
        }

        public virtual string ConnectionString
        {
            get
            {
                if (settings != null)
                {
                    connectionString = settings.Get(AzureConnectionStringSettingName, AzureStorage.AzureEmulatorConnectionString);
                }
                return connectionString;
            }
        }

        public string QueueName { get; set; }

        public void Write(WebhookEvent webhookEvent)
        {
            Guard.AgainstNull(() => webhookEvent, webhookEvent);

            webhookEvent.Data = webhookEvent.Data.ToSafeJson();

            logger.InfoFormat("[ServiceStack.Webhooks.Azure.AzureQueueEventSink] Sinking webhook event {0}, to queue '{1}' queue", webhookEvent.ToJson(), QueueName);
            QueueStorage.Enqueue(webhookEvent);
        }

        public List<WebhookEvent> Peek()
        {
            return QueueStorage.Peek();
        }

        public void Clear()
        {
            QueueStorage.Empty();
        }
    }
}