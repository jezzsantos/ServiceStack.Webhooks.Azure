using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using ServiceStack.Webhooks.Azure.Worker;
using ServiceStack.Webhooks.Clients;
using ServiceStack.Webhooks.Relays;
using ServiceStack.Webhooks.ServiceModel.Types;

namespace ServiceStack.Webhooks.Azure.UnitTests.Worker
{
    public class EventRelayQueueProcessorSpec
    {
        [TestFixture]
        public class GivenAContext
        {
            private EventRelayQueueProcessor processor;
            private Mock<IEventServiceClient> serviceClient;
            private Mock<IEventSubscriptionCache> subscriptionCache;
            private Mock<ISubscriptionService> subscriptionService;

            [SetUp]
            public void Initialize()
            {
                serviceClient = new Mock<IEventServiceClient>();
                subscriptionCache = new Mock<IEventSubscriptionCache>();
                subscriptionService = new Mock<ISubscriptionService>();
                processor = new EventRelayQueueProcessor
                {
                    ServiceClient = serviceClient.Object,
                    SubscriptionCache = subscriptionCache.Object,
                    SubscriptionService = subscriptionService.Object
                };
            }

            [Test, Category("Unit")]
            public void WhenWriteWithNoSubscriptions_ThenIgnoresEvent()
            {
                subscriptionCache.Setup(sc => sc.GetAll(It.IsAny<string>()))
                    .Returns(new List<SubscriptionRelayConfig>());

                var result = processor.ProcessMessage(new WebhookEvent
                {
                    EventName = "aneventname",
                    Data = new Dictionary<string, string> {{"akey", "avalue"}}
                });

                Assert.That(result, Is.True);
                subscriptionCache.Verify(sc => sc.GetAll("aneventname"));
                serviceClient.Verify(sc => sc.Relay(It.IsAny<SubscriptionRelayConfig>(), It.IsAny<string>(), It.IsAny<object>()), Times.Never);
            }

            [Test, Category("Unit")]
            public void WhenWrite_ThenPostsEventToSubscribers()
            {
                var config = new SubscriptionRelayConfig();
                subscriptionCache.Setup(sc => sc.GetAll(It.IsAny<string>()))
                    .Returns(new List<SubscriptionRelayConfig>
                    {
                        config
                    });

                var result = processor.ProcessMessage(new WebhookEvent
                {
                    EventName = "aneventname",
                    Data = new Dictionary<string, string> {{"akey", "avalue"}}
                });

                Assert.That(result, Is.True);
                subscriptionCache.Verify(sc => sc.GetAll("aneventname"));
                serviceClient.VerifySet(sc => sc.Retries = EventRelayQueueProcessor.DefaultServiceClientRetries);
                serviceClient.VerifySet(sc => sc.Timeout = TimeSpan.FromSeconds(EventRelayQueueProcessor.DefaultServiceClientTimeoutSeconds));
                serviceClient.Verify(sc => sc.Relay(config, "aneventname", It.Is<Dictionary<string, string>>(dic => dic["akey"] == "avalue")));
            }

            [Test, Category("Unit")]
            public void WhenWrite_ThenPostsEventToSubscribersAndUpdatesResults()
            {
                var config = new SubscriptionRelayConfig();
                subscriptionCache.Setup(sc => sc.GetAll(It.IsAny<string>()))
                    .Returns(new List<SubscriptionRelayConfig>
                    {
                        config
                    });
                var data = new Dictionary<string, string> {{"akey", "avalue"}};
                var result = new SubscriptionDeliveryResult();
                serviceClient.Setup(sc => sc.Relay(config, "aneventname", data))
                    .Returns(result);

                processor.ProcessMessage(new WebhookEvent
                {
                    EventName = "aneventname",
                    Data = new Dictionary<string, string> {{"akey", "avalue"}}
                });

                subscriptionService.Verify(ss => ss.UpdateResults(It.Is<List<SubscriptionDeliveryResult>>(results =>
                    (results.Count == 1)
                    && (results[0] == result))));
            }
        }
    }
}