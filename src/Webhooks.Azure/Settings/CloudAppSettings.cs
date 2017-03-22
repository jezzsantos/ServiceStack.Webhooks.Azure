using System;
using System.Collections.Generic;
using System.ComponentModel;
using ServiceStack.Configuration;

namespace ServiceStack.Webhooks.Azure.Settings
{
    public class CloudAppSettings : IAppSettings
    {
        private readonly CloudConfigurationSettings cloudSettings;

        public CloudAppSettings()
            : this(new CloudConfigurationSettings())
        {
        }

        internal CloudAppSettings(CloudConfigurationSettings cloudSettings)
        {
            Guard.AgainstNull(() => cloudSettings, cloudSettings);

            this.cloudSettings = cloudSettings;
        }

        public Dictionary<string, string> GetAll()
        {
            return new Dictionary<string, string>();
        }

        public List<string> GetAllKeys()
        {
            return new List<string>();
        }

        public bool Exists(string key)
        {
            return GetString(key) != null;
        }

        public void Set<T>(string key, T value)
        {
            throw new NotImplementedException("CloudAppSettings.Set<T> is not implemented");
        }

        public string GetString(string name)
        {
            return cloudSettings.GetSetting(name);
        }

        public IList<string> GetList(string key)
        {
            return new List<string>();
        }

        public IDictionary<string, string> GetDictionary(string key)
        {
            return new Dictionary<string, string>();
        }

        public T Get<T>(string name)
        {
            return Get(name, default(T));
        }

        public T Get<T>(string name, T defaultValue)
        {
            var setting = GetString(name);
            if (setting == null)
            {
                return defaultValue;
            }

            var converter = TypeDescriptor.GetConverter(typeof(T));
            if (converter.IsValid(setting))
            {
                return (T) converter.ConvertFromString(setting);
            }

            return defaultValue;
        }
    }
}