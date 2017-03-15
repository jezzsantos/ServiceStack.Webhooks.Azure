using System;
using System.Collections.Generic;
using ServiceStack.Configuration;
using ServiceStack.Webhooks.Relays;
using ServiceStack.Webhooks.Relays.Clients;
using ServiceStack.Webhooks.ServiceModel;
using ServiceStack.Webhooks.ServiceModel.Types;

namespace ServiceStack.Webhooks.Azure
{
    public class SubscriptionServiceClient : ISubscriptionService
    {
        public const string SubscriptionServiceBaseUrlSettingName = "SubscriptionServiceClient.SubscriptionService.BaseUrl";

        public SubscriptionServiceClient(IAppSettings settings)
        {
            Guard.AgainstNull(() => settings, settings);

            SubscriptionsBaseUrl = settings.GetString(SubscriptionServiceBaseUrlSettingName);
        }

        public string SubscriptionsBaseUrl { get; set; }

        public IEventServiceClientFactory ServiceClientFactory { get; set; }

        public Action<Relays.Clients.IServiceClient> OnAuthenticationRequired { get; set; }

        public List<SubscriptionRelayConfig> Search(string eventName)
        {
            var serviceClient = ServiceClientFactory.Create(SubscriptionsBaseUrl);
            if (OnAuthenticationRequired != null)
            {
                serviceClient.OnAuthenticationRequired = () => OnAuthenticationRequired(serviceClient);
            }
            return serviceClient.Get(new SearchSubscriptions
            {
                EventName = eventName
            }).Subscribers;
        }

        public void UpdateResults(List<SubscriptionDeliveryResult> results)
        {
            var serviceClient = ServiceClientFactory.Create(SubscriptionsBaseUrl);
            if (OnAuthenticationRequired != null)
            {
                serviceClient.OnAuthenticationRequired = () => OnAuthenticationRequired(serviceClient);
            }
            serviceClient.Put(new UpdateSubscriptionHistory
            {
                Results = results
            });
        }
    }
}