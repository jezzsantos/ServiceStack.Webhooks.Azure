using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Table;

namespace ServiceStack.Webhooks.Azure.Table
{
    internal interface IAzureTableStorage<TEntity> where TEntity : TableEntity
    {
        /// <summary>
        ///     Adds the specified entity to storage
        /// </summary>
        void Add(TEntity subscription);

        /// <summary>
        ///     Finds all entities that meet the specified query
        /// </summary>
        List<TEntity> Find(TableStorageQuery query);

        /// <summary>
        ///     Updates the specified entity in storage
        /// </summary>
        void Update(TEntity subscription);

        /// <summary>
        ///     Deletes the specified entity in storage
        /// </summary>
        void Delete(TEntity subscription);

        /// <summary>
        ///     Gets the specified subscription from storage
        /// </summary>
        TEntity Get(string id);

        /// <summary>
        ///     Empties all subscriptions from storage
        /// </summary>
        void Empty();
    }
}