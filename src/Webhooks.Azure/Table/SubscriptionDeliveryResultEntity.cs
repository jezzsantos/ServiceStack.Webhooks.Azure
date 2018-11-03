using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace ServiceStack.Webhooks.Azure.Table
{
    public class SubscriptionDeliveryResultEntity : TableEntity
    {
        public string Id { get; set; }

        public string EventId { get; set; }

        public DateTime AttemptedDateUtc { get; set; }

        public string StatusCode { get; set; }

        public string StatusDescription { get; set; }

        public string SubscriptionId { get; set; }
    }
}