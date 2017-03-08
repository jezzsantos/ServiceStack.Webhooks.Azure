using System.Net;
using ServiceStack.Webhooks.ServiceModel.Types;

namespace ServiceStack.Webhooks.Azure.Table
{
    internal static class SubscriptionDeliveryResultEntityExtensions
    {
        /// <summary>
        ///     Converts the specified subscription DTO to a <see cref="SubscriptionDeliveryResultEntity" /> entity
        /// </summary>
        public static SubscriptionDeliveryResultEntity ToEntity(this SubscriptionDeliveryResult subscription)
        {
            Guard.AgainstNull(() => subscription, subscription);

            var res = subscription.ConvertTo<SubscriptionDeliveryResultEntity>();
            res.PartitionKey = string.Empty;
            res.RowKey = res.Id;
            res.StatusCode = subscription.StatusCode.ToString();
            res.AttemptedDateUtc = subscription.AttemptedDateUtc.ToSafeAzureDateTime();
            return res;
        }

        /// <summary>
        ///     Converts the specified subscription entity to a <see cref="SubscriptionDeliveryResult" /> DTO
        /// </summary>
        /// <param name="subscription"></param>
        /// <returns></returns>
        public static SubscriptionDeliveryResult FromEntity(this SubscriptionDeliveryResultEntity subscription)
        {
            Guard.AgainstNull(() => subscription, subscription);

            var res = subscription.ConvertTo<SubscriptionDeliveryResult>();
            res.StatusCode = subscription.StatusCode.ToEnumOrDefault(HttpStatusCode.OK);

            return res;
        }
    }
}