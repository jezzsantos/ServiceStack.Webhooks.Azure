using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Table;
using Moq;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Webhooks.Azure.Table;
using ServiceStack.Webhooks.ServiceModel.Types;

namespace ServiceStack.Webhooks.Azure.UnitTests
{
    public class AzureTableSubscriptionStoreSpec
    {
        [TestFixture]
        public class GivenAStore
        {
            private Mock<IAzureTableStorage<SubscriptionDeliveryResultEntity>> deliveryResultStorage;
            private AzureTableSubscriptionStore store;
            private Mock<IAzureTableStorage<WebhookSubscriptionEntity>> subscriptionStorage;

            [SetUp]
            public void Initialize()
            {
                subscriptionStorage = new Mock<IAzureTableStorage<WebhookSubscriptionEntity>>();
                subscriptionStorage.Setup(ss => ss.Find(It.IsAny<TableStorageQuery>()))
                    .Returns(new List<WebhookSubscriptionEntity>());
                deliveryResultStorage = new Mock<IAzureTableStorage<SubscriptionDeliveryResultEntity>>();
                deliveryResultStorage.Setup(ss => ss.Find(It.IsAny<TableStorageQuery>()))
                    .Returns(new List<SubscriptionDeliveryResultEntity>());
                store = new AzureTableSubscriptionStore
                {
                    SubscriptionStorage = subscriptionStorage.Object,
                    DeliveryResultsStorage = deliveryResultStorage.Object,
                };
            }

            [Test, Category("Unit")]
            public void WhenCtorWithNoSetting_ThenInitializes()
            {
                store = new AzureTableSubscriptionStore();

                Assert.That(store.SubscriptionTableName, Is.EqualTo(AzureTableSubscriptionStore.DefaultSubscriptionTableName));
                Assert.That(store.DeliveryResultsTableName, Is.EqualTo(AzureTableSubscriptionStore.DefaultDeliveryResultsTableName));
                Assert.That(store.ConnectionString, Is.EqualTo(AzureStorage.AzureEmulatorConnectionString));
            }

            [Test, Category("Unit")]
            public void WhenCtorWithSettings_ThenInitializesFromSettings()
            {
                var appSettings = new Mock<IAppSettings>();
                appSettings.Setup(settings => settings.Get(AzureTableSubscriptionStore.AzureConnectionStringSettingName, It.IsAny<string>()))
                    .Returns("aconnectionstring");
                appSettings.Setup(settings => settings.Get(AzureTableSubscriptionStore.SubscriptionTableNameSettingName, It.IsAny<string>()))
                    .Returns("atablename1");
                appSettings.Setup(settings => settings.Get(AzureTableSubscriptionStore.DeliveryResultsTableNameSettingName, It.IsAny<string>()))
                    .Returns("atablename2");

                store = new AzureTableSubscriptionStore(appSettings.Object);

                Assert.That(store.SubscriptionTableName, Is.EqualTo("atablename1"));
                Assert.That(store.DeliveryResultsTableName, Is.EqualTo("atablename2"));
                Assert.That(store.ConnectionString, Is.EqualTo("aconnectionstring"));
            }

            [Test, Category("Unit")]
            public void WhenConnectionStringWithNoSettings_ThenReturnsDefault()
            {
                this.store = new AzureTableSubscriptionStore();

                var result = store.ConnectionString;

                Assert.That(result, Is.EqualTo(AzureStorage.AzureEmulatorConnectionString));
            }

            [Test, Category("Unit")]
            public void WhenConnectionStringWithSettings_ThenReturnsSetting()
            {
                var settings = new Mock<IAppSettings>();
                settings.Setup(s => s.Get(AzureTableSubscriptionStore.AzureConnectionStringSettingName, AzureStorage.AzureEmulatorConnectionString))
                    .Returns("aconnectionstring");
                this.store = new AzureTableSubscriptionStore(settings.Object);

                var result = store.ConnectionString;

                Assert.That(result, Is.EqualTo("aconnectionstring"));
                settings.Verify(s => s.Get(AzureTableSubscriptionStore.AzureConnectionStringSettingName, AzureStorage.AzureEmulatorConnectionString));
            }

            [Test, Category("Unit")]
            public void WhenAddWithNullSubscription_ThenThrows()
            {
                Assert.Throws<ArgumentNullException>(() => store.Add(null));
            }

            [Test, Category("Unit")]
            public void WhenAdd_ThenAddsToStorage()
            {
                var subscription = new WebhookSubscription();

                var result = store.Add(subscription);

                Assert.That(result.IsEntityId());
                Assert.That(subscription.Id.IsEntityId());
                Assert.That(subscription.Id, Is.EqualTo(result));
                subscriptionStorage.Verify(ts => ts.Add(It.Is<WebhookSubscriptionEntity>(wse =>
                    wse.Id == result)));
            }

            [Test, Category("Unit")]
            public void WhenFindWithUserId_ThenReturnsAll()
            {
                subscriptionStorage.Setup(ts => ts.Find(It.IsAny<TableStorageQuery>()))
                    .Returns(new List<WebhookSubscriptionEntity>
                    {
                        new WebhookSubscriptionEntity
                        {
                            Id = "asubscriptionentityid"
                        }
                    });

                var result = store.Find("auserid");

                Assert.That(result.Count, Is.EqualTo(1));
                Assert.That(result[0].Id, Is.EqualTo("asubscriptionentityid"));
                subscriptionStorage.Verify(ts => ts.Find(It.Is<TableStorageQuery>(tsq =>
                    tsq.Parts.Count == 1
                    && tsq.Parts[0].PropertyName == "CreatedById"
                    && tsq.Parts[0].Operation == QueryOperator.EQ
                    && tsq.Parts[0].Value.ToString() == "auserid")));
            }

            [Test, Category("Unit")]
            public void WhenSearchWithEventName_ThenReturnsAll()
            {
                subscriptionStorage.Setup(ts => ts.Find(It.IsAny<TableStorageQuery>()))
                    .Returns(new List<WebhookSubscriptionEntity>
                    {
                        new WebhookSubscriptionEntity
                        {
                            Id = "asubscriptionentityid",
                            Config = new SubscriptionConfig
                            {
                                Url = "aurl"
                            }.ToJson()
                        }
                    });

                var result = store.Search("aneventname", null);

                Assert.That(result.Count, Is.EqualTo(1));
                Assert.That(result[0].Config.Url, Is.EqualTo("aurl"));
                Assert.That(result[0].SubscriptionId, Is.EqualTo("asubscriptionentityid"));
                subscriptionStorage.Verify(ts => ts.Find(It.Is<TableStorageQuery>(tsq =>
                    tsq.Parts.Count == 1
                    && tsq.Parts[0].PropertyName == "Event"
                    && tsq.Parts[0].Operation == QueryOperator.EQ
                    && tsq.Parts[0].Value.ToString() == "aneventname")));
            }

            [Test, Category("Unit")]
            public void WhenSearchWithEventNameAndIsActive_ThenReturnsIsActiveOnly()
            {
                subscriptionStorage.Setup(ts => ts.Find(It.IsAny<TableStorageQuery>()))
                    .Returns(new List<WebhookSubscriptionEntity>
                    {
                        new WebhookSubscriptionEntity
                        {
                            Id = "asubscriptionentityid1",
                            Config = new SubscriptionConfig
                            {
                                Url = "aurl1"
                            }.ToJson(),
                            IsActive = false.ToString()
                        },
                        new WebhookSubscriptionEntity
                        {
                            Id = "asubscriptionentityid2",
                            Config = new SubscriptionConfig
                            {
                                Url = "aurl2"
                            }.ToJson(),
                            IsActive = true.ToString()
                        }
                    });

                var result = store.Search("aneventname", true);

                Assert.That(result.Count, Is.EqualTo(1));
                Assert.That(result[0].Config.Url, Is.EqualTo("aurl2"));
                Assert.That(result[0].SubscriptionId, Is.EqualTo("asubscriptionentityid2"));
                subscriptionStorage.Verify(ts => ts.Find(It.Is<TableStorageQuery>(tsq =>
                    tsq.Parts.Count == 1
                    && tsq.Parts[0].PropertyName == "Event"
                    && tsq.Parts[0].Operation == QueryOperator.EQ
                    && tsq.Parts[0].Value.ToString() == "aneventname")));
            }

            [Test, Category("Unit")]
            public void WhenGetByEventNameWithNullEventName_ThenThrows()
            {
                Assert.Throws<ArgumentNullException>(() => store.Get(null, null));
            }

            [Test, Category("Unit")]
            public void WhenGetByEventName_ThenReturnsFirst()
            {
                subscriptionStorage.Setup(ts => ts.Find(It.IsAny<TableStorageQuery>()))
                    .Returns(new List<WebhookSubscriptionEntity>
                    {
                        new WebhookSubscriptionEntity
                        {
                            Id = "asubscriptionentityid"
                        }
                    });

                var result = store.Get("auserid", "aneventname");

                Assert.That(result.Id, Is.EqualTo("asubscriptionentityid"));
                subscriptionStorage.Verify(ts => ts.Find(It.Is<TableStorageQuery>(tsq =>
                    tsq.Parts.Count == 2
                    && tsq.Parts[0].PropertyName == "CreatedById"
                    && tsq.Parts[0].Operation == QueryOperator.EQ
                    && tsq.Parts[0].Value.ToString() == "auserid"
                    && tsq.Parts[1].PropertyName == "Event"
                    && tsq.Parts[1].Operation == QueryOperator.EQ
                    && tsq.Parts[1].Value.ToString() == "aneventname"
                )));
            }

            [Test, Category("Unit")]
            public void WhenGetBySubscriptionIdWithNullSubscriptionId_ThenThrows()
            {
                Assert.Throws<ArgumentNullException>(() => store.Get(null));
            }

            [Test, Category("Unit")]
            public void WhenWhenGetBySubscriptionId_ThenReturnsSubscription()
            {
                subscriptionStorage.Setup(ts => ts.Find(It.IsAny<TableStorageQuery>()))
                    .Returns(new List<WebhookSubscriptionEntity>
                    {
                        new WebhookSubscriptionEntity
                        {
                            Id = "asubscriptionid"
                        }
                    });

                var result = store.Get("asubscriptionid");

                Assert.That(result.Id, Is.EqualTo("asubscriptionid"));
                subscriptionStorage.Verify(ts => ts.Find(It.Is<TableStorageQuery>(tsq =>
                    tsq.Parts.Count == 1
                    && tsq.Parts[0].PropertyName == "Id"
                    && tsq.Parts[0].Operation == QueryOperator.EQ
                    && tsq.Parts[0].Value.ToString() == "asubscriptionid"
                )));
            }

            [Test, Category("Unit")]
            public void WhenUpdateWithNullId_ThenThrows()
            {
                Assert.Throws<ArgumentNullException>(() => store.Update(null, new WebhookSubscription()));
            }

            [Test, Category("Unit")]
            public void WhenUpdateWithNullSubscription_ThenThrows()
            {
                Assert.Throws<ArgumentNullException>(() => store.Update("asubscriptionid", null));
            }

            [Test, Category("Unit")]
            public void WhenUpdateAndNotExists_ThenDoesNotUpdate()
            {
                subscriptionStorage.Setup(ts => ts.Get(It.IsAny<string>()))
                    .Returns((WebhookSubscriptionEntity) null);

                store.Update("asubscriptionid", new WebhookSubscription());

                subscriptionStorage.Verify(ts => ts.Get("asubscriptionid"));
                subscriptionStorage.Verify(ts => ts.Update(It.IsAny<WebhookSubscriptionEntity>()), Times.Never);
            }

            [Test, Category("Unit")]
            public void WhenUpdate_ThenUpdates()
            {
                subscriptionStorage.Setup(ts => ts.Get("asubscriptionid"))
                    .Returns(new WebhookSubscriptionEntity());
                var subscription = new WebhookSubscription
                {
                    Id = "asubscriptionid"
                };

                store.Update("asubscriptionid", subscription);

                subscriptionStorage.Verify(ts => ts.Get("asubscriptionid"));
                subscriptionStorage.Verify(ts => ts.Update(It.Is<WebhookSubscriptionEntity>(wse =>
                    wse.Id == "asubscriptionid")));
            }

            [Test, Category("Unit")]
            public void WhenDeleteWithNullId_ThenThrows()
            {
                Assert.Throws<ArgumentNullException>(() => store.Delete(null));
            }

            [Test, Category("Unit")]
            public void WhenDeleteAndNotExists_ThenDoesNotDelete()
            {
                subscriptionStorage.Setup(ts => ts.Get(It.IsAny<string>()))
                    .Returns((WebhookSubscriptionEntity) null);

                store.Delete("asubscriptionid");

                subscriptionStorage.Verify(ts => ts.Get("asubscriptionid"));
                subscriptionStorage.Verify(ts => ts.Delete(It.IsAny<WebhookSubscriptionEntity>()), Times.Never);
            }

            [Test, Category("Unit")]
            public void WhenDelete_ThenDeletes()
            {
                subscriptionStorage.Setup(ts => ts.Get("asubscriptionid"))
                    .Returns(new WebhookSubscriptionEntity
                    {
                        Id = "asubscriptionid"
                    });

                store.Delete("asubscriptionid");

                subscriptionStorage.Verify(ts => ts.Get("asubscriptionid"));
                subscriptionStorage.Verify(ts => ts.Delete(It.Is<WebhookSubscriptionEntity>(wse =>
                    wse.Id == "asubscriptionid")));
            }

            [Test, Category("Unit")]
            public void WhenAddDeliveryResultAndNullSubscriptionId_ThenThrows()
            {
                Assert.Throws<ArgumentNullException>(() =>
                    store.Add(null, new SubscriptionDeliveryResult()));
            }

            [Test, Category("Unit")]
            public void WhenAddDeliveryResultAndNullDeliveryResult_ThenThrows()
            {
                Assert.Throws<ArgumentNullException>(() =>
                    store.Add("asubscriptionid", null));
            }

            [Test, Category("Unit")]
            public void WhenAddDeliveryResultAndUnknownSubscriptionId_ThenDoesNotAddResults()
            {
                store.Add("anunknownsubscriptionid", new SubscriptionDeliveryResult());
            }

            [Test, Category("Unit")]
            public void WhenAddDeliveryResult_ThenAddResults()
            {
                var result = new SubscriptionDeliveryResult
                {
                    Id = "aresultid",
                    SubscriptionId = "asubscriptionid"
                };
                subscriptionStorage.Setup(ss => ss.Find(It.IsAny<TableStorageQuery>()))
                    .Returns(new List<WebhookSubscriptionEntity>
                    {
                        new WebhookSubscriptionEntity
                        {
                            Id = "asubscriptionid"
                        }
                    });

                store.Add("asubscriptionid", result);

                subscriptionStorage.Verify(ss => ss.Find(It.IsAny<TableStorageQuery>()));
                deliveryResultStorage.Verify(drs => drs.Add(It.Is<SubscriptionDeliveryResultEntity>(dre
                    => dre.Id == "aresultid"
                       && dre.SubscriptionId == "asubscriptionid")));
            }

            [Test, Category("Unit")]
            public void WhenSearchForDeliveryResultsWithNullSubscriptionId_ThenThrows()
            {
                Assert.Throws<ArgumentNullException>(() =>
                    store.Search(null, 1));
            }

            [Test, Category("Unit")]
            public void WhenSearchForDeliveryResultsWithZeroTop_ThenThrows()
            {
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    store.Search("asubscriptionid", 0));
            }

            [Test, Category("Unit")]
            public void WhenSearchForDeliveryResults_ThenReturnsMatchingResults()
            {
                var result = new SubscriptionDeliveryResultEntity
                {
                    Id = "aresultid"
                };
                deliveryResultStorage.Setup(drs => drs.Find(It.IsAny<TableStorageQuery>()))
                    .Returns(new List<SubscriptionDeliveryResultEntity>
                    {
                        result
                    });

                var results = store.Search("asubscriptionid", 1);

                Assert.That(results.Count, Is.EqualTo(1));
                Assert.That(results[0].Id, Is.EqualTo("aresultid"));
            }

            [Test, Category("Unit")]
            public void WhenSearchForDeliveryResults_ThenReturnsMatchingResultsInOrder()
            {
                var datum1 = DateTime.UtcNow.ToNearestSecond();
                var datum2 = datum1.AddDays(1);
                var result1 = new SubscriptionDeliveryResultEntity
                {
                    Id = "aresultid1",
                    AttemptedDateUtc = datum1
                };
                var result2 = new SubscriptionDeliveryResultEntity
                {
                    Id = "aresultid2",
                    AttemptedDateUtc = datum2
                };
                deliveryResultStorage.Setup(drs => drs.Find(It.IsAny<TableStorageQuery>()))
                    .Returns(new List<SubscriptionDeliveryResultEntity>
                    {
                        result1,
                        result2
                    });

                var results = store.Search("asubscriptionid", 1);

                Assert.That(results.Count, Is.EqualTo(1));
                Assert.That(results[0].Id, Is.EqualTo("aresultid2"));
            }

            [Test, Category("Unit")]
            public void WhenClear_ThenEmptiesStore()
            {
                store.Clear();

                subscriptionStorage.Verify(qs => qs.Empty());
            }
        }
    }

    public class TestEntity : TableEntity
    {
    }
}