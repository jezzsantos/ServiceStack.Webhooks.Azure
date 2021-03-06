﻿using NUnit.Framework;
using ServiceStack.Webhooks.Azure.IntTests.Services;

namespace ServiceStack.Webhooks.Azure.IntTests
{
    public class WebhookClientSpec
    {
        [TestFixture]
        public class GivenAzureConfiguredFeature : CloudServiceIntegrationTestBase
        {
            private static AppSelfHostBase appHost;
            private static JsonServiceClient client;
            private const string BaseUrl = "http://localhost:5567/";
            private static ISubscriptionStore subscriptionsStore;
            private static IEventSink eventSink;

            [OneTimeTearDown]
            public void CleanupContext()
            {
                appHost.Dispose();
            }

            [OneTimeSetUp]
            public void InitializeContext()
            {
                appHost = new AppHostForAzureTesting();
                appHost.Init();
                appHost.Start(BaseUrl);

                client = new JsonServiceClient(BaseUrl);
                subscriptionsStore = appHost.Resolve<ISubscriptionStore>();
                eventSink = appHost.Resolve<IEventSink>();
            }

            [SetUp]
            public void Initialize()
            {
                ((AzureTableSubscriptionStore) subscriptionsStore).Clear();
                ((AzureQueueEventSink) eventSink).Clear();
            }

            [Test, Category("Integration.NOCI")]
            public void WhenRaiseEvent_ThenEventSunk()
            {
                client.Put(new RaiseEvent
                {
                    EventName = "aneventname"
                });

                var events = ((AzureQueueEventSink) eventSink).Peek();

                Assert.That(events.Count, Is.EqualTo(1));
                Assert.That(events[0].EventName, Is.EqualTo("aneventname"));
            }
        }
    }
}