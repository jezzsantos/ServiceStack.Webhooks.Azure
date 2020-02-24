#if NETSTANDARD
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Fabric;
using ServiceStack.Configuration;

namespace ServiceStack.Webhooks.Azure.Settings
{
    public class FabricAppSettings : IAppSettings
    {
        private readonly FabricConfigurationSettings settings;

        public FabricAppSettings(StatelessServiceContext context)
            : this(new FabricConfigurationSettings(context))
        {
        }

        public FabricAppSettings(FabricConfigurationSettings settings)
        {
            Guard.AgainstNull(() => settings, settings);

            this.settings = settings;
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
            throw new NotImplementedException("FabricAppSettings.Set<T> is not implemented");
        }

        public string GetString(string name)
        {
            return settings.GetSetting(name);
        }

        public IList<string> GetList(string key)
        {
            return new List<string>();
        }

        public IDictionary<string, string> GetDictionary(string key)
        {
            return new Dictionary<string, string>();
        }

        public List<KeyValuePair<string, string>> GetKeyValuePairs(string key)
        {
            return new List<KeyValuePair<string, string>>();
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
#endif