
namespace ServiceStack.Webhooks.Azure.Settings
{
    public interface IConfigurationProvider
    {
        /// <summary>
        ///     Gets the duration of the cache
        /// </summary>
        int CacheDuration { get; }

        /// <summary>
        ///     Gets the setting with the specified name
        /// </summary>
        string GetSetting(string settingName);
    }
}
