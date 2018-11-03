using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Webhooks.Azure.IntTests.Services;
using ServiceStack.Webhooks.Azure.Queue;
using ServiceStack.Webhooks.ServiceModel;
using ServiceStack.Webhooks.ServiceModel.Types;

namespace ServiceStack.Webhooks.Azure.IntTests
{
    public class EventRelayWorkerSpec
    {
        [TestFixture]
        public class GivenTheRelayWorkerHostedInAzureRole : AzureIntegrationTestBase
        {
            private static AppHostForAzureTesting appHost;
            private static JsonServiceClient client;
            private const string BaseUrl = "http://localhost:5567/";

            private static int subscriptionCounter = 1;
            private IAzureQueueStorage<WebhookEvent> queue;

            [OneTimeSetUp]
            public void InitializeContext()
            {
                appHost = new AppHostForAzureTesting();
                appHost.Init();
                appHost.Start(BaseUrl);

                client = new JsonServiceClient(BaseUrl);

                var settings = appHost.Resolve<IAppSettings>();
                queue = new AzureQueueStorage<WebhookEvent>(settings.GetString(AzureQueueEventSink.AzureConnectionStringSettingName), settings.GetString(AzureQueueEventSink.QueueNameSettingName));
            }

            [OneTimeTearDown]
            public void CleanupContext()
            {
                appHost.Dispose();
            }

            [SetUp]
            public void Initialize()
            {
                ((AzureTableSubscriptionStore) appHost.Resolve<ISubscriptionStore>()).Clear();
                queue.Empty();

                client.Put(new ResetConsumedEvents());

                SetupSubscription("aneventname");
            }

            [TearDown]
            public void Cleanup()
            {
            }

            [Test, Category("Integration")]
            public void WhenNoEventOnQueue_ThenNoSubscribersNotified()
            {
                WaitFor(10);

                var consumed = client.Get(new GetConsumedEvents()).Events;

                Assert.That(consumed.Count, Is.EqualTo(0));
            }

            [Test, Category("Integration.NOCI")]
            public void WhenEventQueued_ThenSubscriberNotified()
            {
                SetupEvent("aneventname");
                WaitFor(10);

                var consumed = client.Get(new GetConsumedEvents()).Events;

                Assert.That(consumed.Count, Is.EqualTo(1));
                AssertSunkEvent(consumed[0]);

                var subscriptionId = client.Get(new ListSubscriptions()).Subscriptions[0].Id;

                var history = client.Get(new GetSubscription
                {
                    Id = subscriptionId
                }).History;

                Assert.That(history.Count, Is.EqualTo(1));
                Assert.That(history[0].AttemptedDateUtc, Is.EqualTo(DateTime.UtcNow).Within(30).Seconds);
                Assert.That(history[0].StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(history[0].StatusDescription, Is.EqualTo("No Content"));
                Assert.That(history[0].SubscriptionId, Is.EqualTo(subscriptionId));
                Assert.That(history[0].EventId, Is.Not.Empty);
            }

            private static void SetupSubscription(string eventName)
            {
                client.Post(new CreateSubscription
                {
                    Name = "aname{0}".Fmt(subscriptionCounter++),
                    Events = new List<string> {eventName},
                    Config = new SubscriptionConfig
                    {
                        Url = BaseUrl.WithoutTrailingSlash() + new ConsumeEvent().ToPostUrl()
                    }
                });
            }

            private static void SetupEvent(string eventName)
            {
                client.Put(new RaiseEvent
                {
                    EventName = eventName,
                    Data = new TestEvent
                    {
                        A = 1,
                        B = 2,
                        C = new NestedTestEvent
                        {
                            D = 3,
                            E = 4,
                            F = 5
                        }
                    }
                });
            }

            private static void AssertSunkEvent(ConsumedEvent consumedEvent)
            {
                Assert.That(consumedEvent.EventName, Is.EqualTo("aneventname"));
                Assert.That(consumedEvent.Data.A, Is.EqualTo(1));
                Assert.That(consumedEvent.Data.B, Is.EqualTo(2));
                Assert.That(consumedEvent.Data.C.D, Is.EqualTo(3));
                Assert.That(consumedEvent.Data.C.E, Is.EqualTo(4));
                Assert.That(consumedEvent.Data.C.F, Is.EqualTo(5));
            }

            private static void WaitFor(int seconds)
            {
                Task.Delay(TimeSpan.FromSeconds(seconds)).Wait();
            }
        }
    }
}