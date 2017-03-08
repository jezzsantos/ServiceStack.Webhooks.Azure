using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Configuration;
using ServiceStack.Webhooks.Azure.Table;
using ServiceStack.Webhooks.ServiceModel.Types;

namespace ServiceStack.Webhooks.Azure
{
    public class AzureTableSubscriptionStore : ISubscriptionStore
    {
        internal const string DefaultSubscriptionTableName = "webhooksubscriptions";
        internal const string DefaultDeliveryResultsTableName = "webhookdeliveryresults";
        internal const string AzureConnectionStringSettingName = "AzureTableSubscriptionStore.ConnectionString";
        internal const string SubscriptionTableNameSettingName = "AzureTableSubscriptionStore.SubscriptionsTable.Name";
        internal const string DeliveryResultsTableNameSettingName = "AzureTableSubscriptionStore.DeliveryResultsTable.Name";
        private IAzureTableStorage<SubscriptionDeliveryResultEntity> deliveryResultsTableStorage;
        private IAzureTableStorage<WebhookSubscriptionEntity> subscriptionTableStorage;

        public AzureTableSubscriptionStore()
        {
            SubscriptionTableName = DefaultSubscriptionTableName;
            DeliveryResultsTableName = DefaultDeliveryResultsTableName;

            ConnectionString = AzureStorage.AzureEmulatorConnectionString;
        }

        public AzureTableSubscriptionStore(IAppSettings settings) : this()
        {
            Guard.AgainstNull(() => settings, settings);

            SubscriptionTableName = settings.Get(SubscriptionTableNameSettingName, DefaultSubscriptionTableName);
            DeliveryResultsTableName = settings.Get(DeliveryResultsTableNameSettingName, DefaultDeliveryResultsTableName);
            ConnectionString = settings.Get(AzureConnectionStringSettingName, AzureStorage.AzureEmulatorConnectionString);
        }

        /// <summary>
        ///     For testing only
        /// </summary>
        internal IAzureTableStorage<WebhookSubscriptionEntity> SubscriptionStorage
        {
            get { return subscriptionTableStorage ?? (subscriptionTableStorage = new AzureTableStorage<WebhookSubscriptionEntity>(ConnectionString, SubscriptionTableName)); }
            set { subscriptionTableStorage = value; }
        }

        /// <summary>
        ///     For testing only
        /// </summary>
        internal IAzureTableStorage<SubscriptionDeliveryResultEntity> DeliveryResultsStorage
        {
            get { return deliveryResultsTableStorage ?? (deliveryResultsTableStorage = new AzureTableStorage<SubscriptionDeliveryResultEntity>(ConnectionString, DeliveryResultsTableName)); }
            set { deliveryResultsTableStorage = value; }
        }

        public string ConnectionString { get; set; }

        public string SubscriptionTableName { get; set; }

        public string DeliveryResultsTableName { get; set; }

        public string Add(WebhookSubscription subscription)
        {
            Guard.AgainstNull(() => subscription, subscription);

            var id = DataFormats.CreateEntityIdentifier();
            subscription.Id = id;

            SubscriptionStorage.Add(subscription.ToEntity());

            return id;
        }

        public List<WebhookSubscription> Find(string userId)
        {
            return SubscriptionStorage.Find(new TableStorageQuery(@"CreatedById", QueryOperator.EQ, userId))
                .ConvertAll(entity => entity.FromEntity());
        }

        public WebhookSubscription Get(string userId, string eventName)
        {
            Guard.AgainstNullOrEmpty(() => eventName, eventName);

            return SubscriptionStorage.Find(new TableStorageQuery(new List<QueryPart>
                {
                    new QueryPart(@"CreatedById", QueryOperator.EQ, userId),
                    new QueryPart(@"Event", QueryOperator.EQ, eventName)
                }))
                .ConvertAll(entity => entity.FromEntity())
                .FirstOrDefault();
        }

        public WebhookSubscription Get(string subscriptionId)
        {
            Guard.AgainstNullOrEmpty(() => subscriptionId, subscriptionId);

            return SubscriptionStorage.Find(new TableStorageQuery(@"Id", QueryOperator.EQ, subscriptionId))
                .ConvertAll(entity => entity.FromEntity())
                .FirstOrDefault();
        }

        public void Update(string subscriptionId, WebhookSubscription subscription)
        {
            Guard.AgainstNullOrEmpty(() => subscriptionId, subscriptionId);
            Guard.AgainstNull(() => subscription, subscription);

            var sub = SubscriptionStorage.Get(subscriptionId);
            if (sub != null)
            {
                SubscriptionStorage.Update(subscription.ToEntity());
            }
        }

        public void Delete(string subscriptionId)
        {
            Guard.AgainstNullOrEmpty(() => subscriptionId, subscriptionId);

            var subscription = SubscriptionStorage.Get(subscriptionId);
            if (subscription != null)
            {
                SubscriptionStorage.Delete(subscription);
            }
        }

        public List<SubscriptionRelayConfig> Search(string eventName, bool? isActive)
        {
            return SubscriptionStorage.Find(new TableStorageQuery(@"Event", QueryOperator.EQ, eventName))
                .ConvertAll(entity => entity.FromEntity())
                .Where(sub => !isActive.HasValue
                              || (sub.IsActive == isActive.Value))
                .Select(sub => new SubscriptionRelayConfig
                {
                    SubscriptionId = sub.Id,
                    Config = sub.Config
                })
                .ToList();
        }

        public void Add(string subscriptionId, SubscriptionDeliveryResult result)
        {
            Guard.AgainstNullOrEmpty(() => subscriptionId, subscriptionId);
            Guard.AgainstNull(() => result, result);

            var subscription = Get(subscriptionId);
            if (subscription != null)
            {
                DeliveryResultsStorage.Add(result.ToEntity());
            }
        }

        public List<SubscriptionDeliveryResult> Search(string subscriptionId, int top)
        {
            Guard.AgainstNullOrEmpty(() => subscriptionId, subscriptionId);
            if (top <= 0)
            {
                throw new ArgumentOutOfRangeException("top");
            }

            return DeliveryResultsStorage.Find(new TableStorageQuery(@"SubscriptionId", QueryOperator.EQ, subscriptionId))
                .ConvertAll(entity => entity.FromEntity())
                .OrderByDescending(sub => sub.AttemptedDateUtc)
                .Take(top)
                .ToList();
        }

        public void Clear()
        {
            DeliveryResultsStorage.Empty();
            SubscriptionStorage.Empty();
        }
    }
}