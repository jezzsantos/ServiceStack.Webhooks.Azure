#if NETSTANDARD
using System.Fabric;

namespace ServiceStack.Webhooks.Azure.Settings
{
    /// <summary>
    ///     Defines a configuration provider for Fabric services, reading configuration from PackageRoot/Config/Settings.xml
    /// </summary>
    internal class FabricConfigurationProvider : IConfigurationProvider
    {
        internal const string FabricConfigurationCacheDurationSettingName =
            @"FabricConfigurationSettings.CacheDuration";
        private const int DefaultCacheDuration = 60;
        private readonly StatelessServiceContext context;

        public FabricConfigurationProvider(StatelessServiceContext context)
        {
            this.context = context;
        }

        public int CacheDuration
        {
            get
            {
                var durationValue = GetSettingInternal(FabricConfigurationCacheDurationSettingName);
                if (durationValue.HasValue())
                {
                    int result;
                    if (int.TryParse(durationValue, out result))
                    {
                        return result;
                    }
                }

                return DefaultCacheDuration;
            }
        }

        public string GetSetting(string settingName)
        {
            return GetSettingInternal(settingName);
        }

        private string GetSettingInternal(string settingName)
        {
            var configPackage = context.CodePackageActivationContext.GetConfigurationPackageObject("Config");

            var parameters = configPackage.Settings.Sections["AppSettings"].Parameters;
            return parameters[settingName]?.Value;
        }
    }
}
#endif