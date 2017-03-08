using Funq;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Validation;

namespace ServiceStack.Webhooks.Azure.IntTests.Services
{
    public class AppHostForAzureTesting : AppSelfHostBase
    {
        public AppHostForAzureTesting()
            : base("AppHostForAzureTesting", typeof(AppHostForAzureTesting).Assembly)
        {
        }

        public override void Configure(Container container)
        {
            LogManager.LogFactory = new ConsoleLogFactory();
            Config.DebugMode = true;
            Config.ReturnsInnerException = true;
            var settings = new AppSettings();
            container.Register<IAppSettings>(settings);
            container.Register<ISubscriptionStore>(new AzureTableSubscriptionStore(settings));
            container.Register<IEventSink>(new AzureQueueEventSink(settings));
            Plugins.Add(new ValidationFeature());
            Plugins.Add(new WebhookFeature());
        }
    }
}