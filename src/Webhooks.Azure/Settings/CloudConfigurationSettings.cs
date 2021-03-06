﻿#if NETFRAMEWORK
using System;
using System.Collections.Generic;

namespace ServiceStack.Webhooks.Azure.Settings
{
    /// <summary>
    ///     Defines configurations settings stored in an cloud role configuration file.
    /// </summary>
    public class CloudConfigurationSettings
    {
        private readonly IConfigurationProvider configurationProvider;
        private DateTime cacheRefreshedNext = DateTime.MinValue;

        public CloudConfigurationSettings()
            : this(new CloudConfigurationProvider())
        {
        }

        public CloudConfigurationSettings(IConfigurationProvider provider)
        {
            Guard.AgainstNull(() => provider, provider);

            configurationProvider = provider;
            CachedSettings = new Dictionary<string, string>();
            ClearCache();
        }

        public IDictionary<string, string> CachedSettings { get; }

        /// <summary>
        ///     Returns the setting with the specified name
        /// </summary>
        /// <param name="settingName"> The name of the setting </param>
        public virtual string GetSetting(string settingName)
        {
            RefreshCacheIfOutOfDate();

            lock (CachedSettings)
            {
                if (!CachedSettings.ContainsKey(settingName))
                {
                    var settingValue = configurationProvider.GetSetting(settingName);
                    CachedSettings.Add(settingName, settingValue);

                    return settingValue;
                }
            }

            return CachedSettings[settingName];
        }

        public void ClearCache()
        {
            CachedSettings.Clear();
            cacheRefreshedNext = DateTime.UtcNow + TimeSpan.FromSeconds(configurationProvider.CacheDuration);
        }

        private void RefreshCacheIfOutOfDate()
        {
            var now = DateTime.UtcNow;
            if (now >= cacheRefreshedNext)
            {
                ClearCache();
            }
        }
    }
}
#endif