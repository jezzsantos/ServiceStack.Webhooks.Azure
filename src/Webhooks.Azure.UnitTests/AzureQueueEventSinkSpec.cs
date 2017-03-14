﻿using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Webhooks.Azure.Queue;

namespace ServiceStack.Webhooks.Azure.UnitTests
{
    public class AzureQueueEventSinkSpec
    {
        [TestFixture]
        public class GivenAStore
        {
            private Mock<IAzureQueueStorage<WebhookEvent>> queueStorage;
            private AzureQueueEventSink sink;

            [SetUp]
            public void Initialize()
            {
                queueStorage = new Mock<IAzureQueueStorage<WebhookEvent>>();
                sink = new AzureQueueEventSink
                {
                    QueueStorage = queueStorage.Object
                };
            }

            [Test, Category("Unit")]
            public void WhenCtorWithNoSetting_ThenInitializes()
            {
                sink = new AzureQueueEventSink();

                Assert.That(sink.QueueName, Is.EqualTo(AzureQueueEventSink.DefaultQueueName));
                Assert.That(sink.ConnectionString, Is.EqualTo(AzureStorage.AzureEmulatorConnectionString));
            }

            [Test, Category("Unit")]
            public void WhenCtorWithSettings_ThenInitializesFromSettings()
            {
                var appSettings = new Mock<IAppSettings>();
                appSettings.Setup(settings => settings.Get(AzureQueueEventSink.AzureConnectionStringSettingName, It.IsAny<string>()))
                    .Returns("aconnectionstring");
                appSettings.Setup(settings => settings.Get(AzureQueueEventSink.QueueNameSettingName, It.IsAny<string>()))
                    .Returns("aqueuename");

                sink = new AzureQueueEventSink(appSettings.Object);

                Assert.That(sink.QueueName, Is.EqualTo("aqueuename"));
                Assert.That(sink.ConnectionString, Is.EqualTo("aconnectionstring"));
            }

            [Test, Category("Unit")]
            public void WhenCreate_ThenCreatesEvent()
            {
                sink.Write("aneventname", new Dictionary<string, string> {{"akey", "avalue"}});

                queueStorage.Verify(qs => qs.Enqueue(It.Is<WebhookEvent>(whe =>
                    whe.EventName == "aneventname"
                    && whe.Data["akey"] == "avalue"
                    && whe.CreatedDateUtc.IsNear(DateTime.UtcNow)
                    && whe.Id.IsEntityId()
                )));
            }

            [Test, Category("Unit")]
            public void WhenPeek_ThenReturnsPeekedEvents()
            {
                var events = new List<WebhookEvent>();
                queueStorage.Setup(qs => qs.Peek())
                    .Returns(events);

                var results = sink.Peek();

                Assert.That(results, Is.EqualTo(events));
                queueStorage.Verify(qs => qs.Peek());
            }

            [Test, Category("Unit")]
            public void WhenClear_ThenEmptiesStore()
            {
                sink.Clear();

                queueStorage.Verify(qs => qs.Empty());
            }
        }
    }
}