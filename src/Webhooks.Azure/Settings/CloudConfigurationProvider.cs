#if NETFRAMEWORK
using Microsoft.Azure;

namespace ServiceStack.Webhooks.Azure.Settings
{
    internal class CloudConfigurationProvider : IConfigurationProvider
    {
        internal const string CloudConfigurationCacheDurationSettingName =
            @"CloudConfigurationSettings.CacheDuration";
        private const int DefaultCacheDuration = 60;

        public string GetSetting(string settingName)
        {
            return CloudConfigurationManager.GetSetting(settingName);
        }

        public int CacheDuration
        {
            get
            {
                var durationValue = CloudConfigurationManager.GetSetting(CloudConfigurationCacheDurationSettingName);
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
    }
}
#endif