using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Webhooks.Azure.IntTests
{
    public class AzureQueueEventSinkSpec
    {
        [TestFixture]
        public class GivenAQueue : AzureIntegrationTestBase
        {
            private AzureQueueEventSink sink;

            [SetUp]
            public void Initialize()
            {
                sink = new AzureQueueEventSink();
                sink.Clear();
            }

            [Test, Category("Integration")]
            public void WhenCreate_ThenQueuesEvent()
            {
                var id = DataFormats.CreateEntityIdentifier();
                var datum = SystemTime.UtcNow.ToNearestMillisecond();
                var data = new TestData();

                sink.Write(new WebhookEvent
                {
                    CreatedDateUtc = datum,
                    EventName = "aneventname",
                    Data = data,
                    Id = id
                });

                var result = sink.Peek();

                Assert.That(result.Count, Is.EqualTo(1));
                Assert.That(result[0].CreatedDateUtc, Is.EqualTo(datum));
                Assert.That(result[0].EventName, Is.EqualTo("aneventname"));
                Assert.That(result[0].Data, Is.EqualTo(new TestData().ToJson()));
                Assert.That(result[0].Id, Is.EqualTo(id));
            }
        }
    }

    public class TestData
    {
    }
}