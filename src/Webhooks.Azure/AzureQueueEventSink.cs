﻿using System;
using System.Collections.Generic;
using ServiceStack.Configuration;
using ServiceStack.Webhooks.Azure.Queue;

namespace ServiceStack.Webhooks.Azure
{
    public class AzureQueueEventSink : IEventSink
    {
        public const string DefaultQueueName = "webhookevents";
        public const string AzureConnectionStringSettingName = "AzureQueueEventSink.ConnectionString";
        public const string QueueNameSettingName = "AzureQueueEventSink.Queue.Name";
        private IAzureQueueStorage<WebhookEvent> queueStorage;

        public AzureQueueEventSink()
        {
            QueueName = DefaultQueueName;

            ConnectionString = AzureStorage.AzureEmulatorConnectionString;
        }

        public AzureQueueEventSink(IAppSettings settings)
            : this()
        {
            Guard.AgainstNull(() => settings, settings);

            ConnectionString = settings.Get(AzureConnectionStringSettingName, AzureStorage.AzureEmulatorConnectionString);
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

        public string ConnectionString { get; set; }

        public string QueueName { get; set; }

        public void Write(string eventName, Dictionary<string, string> data)
        {
            Guard.AgainstNullOrEmpty(() => eventName, eventName);

            QueueStorage.Enqueue(new WebhookEvent
            {
                Id = DataFormats.CreateEntityIdentifier(),
                EventName = eventName,
                Data = data,
                CreatedDateUtc = DateTime.UtcNow.ToNearestMillisecond()
            });
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