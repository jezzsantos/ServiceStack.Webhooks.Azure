namespace ServiceStack.Webhooks.Azure.IntTests
{
    public class AzureTableSubscriptionStoreSpec
    {
        public class GivenAzureTableSubscriptionStoreAndNoUser : GivenNoUserWithSubscriptionStoreBase
        {
            public override ISubscriptionStore GetSubscriptionStore()
            {
                var store = new AzureTableSubscriptionStore();
                store.Clear();
                return store;
            }
        }

        public class GivenAzureTableSubscriptionStoreAndAUser : GivenAUserWithSubscriptionStoreBase
        {
            public override ISubscriptionStore GetSubscriptionStore()
            {
                var store = new AzureTableSubscriptionStore();
                store.Clear();
                return store;
            }
        }
    }
}