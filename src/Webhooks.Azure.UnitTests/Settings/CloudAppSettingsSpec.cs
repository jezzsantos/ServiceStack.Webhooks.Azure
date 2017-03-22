using System;
using Moq;
using NUnit.Framework;
using ServiceStack.Webhooks.Azure.Settings;

namespace ServiceStack.Webhooks.Azure.UnitTests.Settings
{
    public class CloudAppSettingsSpec
    {
        [TestFixture]
        public class GivenAContext
        {
            private Mock<ICloudConfigurationProvider> provider;
            private CloudAppSettings settings;

            [SetUp]
            public void Initialize()
            {
                provider = new Mock<ICloudConfigurationProvider>();
                provider.Setup(prov => prov.GetSetting("akey"))
                    .Returns("avalue");

                settings = new CloudAppSettings(new CloudConfigurationSettings(provider.Object));
            }

            [Test, Category("Unit")]
            public void WhenGetAll_ThenReturnsEmptyDictionary()
            {
                var result = settings.GetAll();

                Assert.That(result.Count, Is.EqualTo(0));
            }

            [Test, Category("Unit")]
            public void WhenGetAllKeys_ThenReturnsEmptyList()
            {
                var result = settings.GetAllKeys();

                Assert.That(result.Count, Is.EqualTo(0));
            }

            [Test, Category("Unit")]
            public void WhenExistsAndNotExists_ThenReturnsFalse()
            {
                provider.Setup(prov => prov.GetSetting("akey"))
                    .Returns((string) null);

                var result = settings.Exists("akey");

                Assert.That(result, Is.False);
            }

            [Test, Category("Unit")]
            public void WhenExistsAndExists_ThenReturnsTrue()
            {
                var result = settings.Exists("akey");

                Assert.That(result, Is.True);
            }

            [Test, Category("Unit")]
            public void WhenSet_ThenThrowsNotImplemented()
            {
                Assert.Throws<NotImplementedException>(() =>
                    settings.Set("akey", "avalue"));
            }

            [Test, Category("Unit")]
            public void WhenGetStringAndExists_ThenReturnsStringValue()
            {
                var result = settings.GetString("akey");

                Assert.That(result, Is.EqualTo("avalue"));
            }

            [Test, Category("Unit")]
            public void WhenGetStringAndNotExists_ThenReturnsNull()
            {
                provider.Setup(prov => prov.GetSetting("akey"))
                    .Returns((string) null);

                var result = settings.GetString("akey");

                Assert.That(result, Is.Null);
            }

            [Test, Category("Unit")]
            public void WhenGetList_ThenReturnsEmptyList()
            {
                var result = settings.GetList("akey");

                Assert.That(result.Count, Is.EqualTo(0));
            }

            [Test, Category("Unit")]
            public void WhenGetDictionary_ThenReturnsEmptyDictionary()
            {
                var result = settings.GetDictionary("akey");

                Assert.That(result.Count, Is.EqualTo(0));
            }

            [Test, Category("Unit")]
            public void WhenGetAsStringAndNotExists_ThenReturnsNull()
            {
                provider.Setup(prov => prov.GetSetting("akey"))
                    .Returns((string) null);

                var result = settings.Get<string>("akey");

                Assert.That(result, Is.Null);
            }

            [Test, Category("Unit")]
            public void WhenGetAsStringAndExists_ThenReturnsStringValue()
            {
                var result = settings.Get<string>("akey");

                Assert.That(result, Is.EqualTo("avalue"));
            }

            [Test, Category("Unit")]
            public void WhenGetAsIntAndNotConvertableToInt_ThenReturnsIntDefault()
            {
                provider.Setup(prov => prov.GetSetting("akey"))
                    .Returns("notaninteger");

                var result = settings.Get<int>("akey");

                Assert.That(result, Is.EqualTo(0));
            }

            [Test, Category("Unit")]
            public void WhenGetAsIntAndConvertableToInt_ThenReturnsInteger()
            {
                provider.Setup(prov => prov.GetSetting("akey"))
                    .Returns("1");

                var result = settings.Get<int>("akey");

                Assert.That(result, Is.EqualTo(1));
            }

            [Test, Category("Unit")]
            public void WhenGetWithDefaultAsIntAndNotConvertableToInt_ThenReturnsIntDefault()
            {
                provider.Setup(prov => prov.GetSetting("akey"))
                    .Returns("notaninteger");

                var result = settings.Get("akey", 9);

                Assert.That(result, Is.EqualTo(9));
            }

            [Test, Category("Unit")]
            public void WhenGetWithDefaultAsIntAndConvertableToInt_ThenReturnsInteger()
            {
                provider.Setup(prov => prov.GetSetting("akey"))
                    .Returns("1");

                var result = settings.Get("akey", 9);

                Assert.That(result, Is.EqualTo(1));
            }

            [Test, Category("Unit")]
            public void WhenGetWithDefaultAndNotExists_ThenReturnsDefault()
            {
                provider.Setup(prov => prov.GetSetting("akey"))
                    .Returns((string) null);

                var result = settings.Get("akey", "adefault");

                Assert.That(result, Is.EqualTo("adefault"));
            }

            [Test, Category("Unit")]
            public void WhenGetWithDefaultAndExists_ThenReturnsValue()
            {
                provider.Setup(prov => prov.GetSetting("akey"))
                    .Returns("avalue");

                var result = settings.Get("akey", "adefault");

                Assert.That(result, Is.EqualTo("avalue"));
            }
        }
    }
}