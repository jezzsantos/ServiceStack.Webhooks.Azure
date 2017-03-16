namespace ServiceStack.Webhooks.Azure.IntTests.Services
{
    internal class TestService : Service
    {
        public IWebhooks Webhooks { get; set; }

        public void Any(RaiseEvent request)
        {
            Webhooks.Publish(request.EventName, request.Data);
        }
    }

    [Route("/raise")]
    public class RaiseEvent : IReturnVoid
    {
        public string EventName { get; set; }

        public TestEvent Data { get; set; }
    }

    public class TestEvent
    {
        public object A { get; set; }

        public object B { get; set; }

        public NestedTestEvent C { get; set; }
    }

    public class NestedTestEvent
    {
        public object D { get; set; }

        public object E { get; set; }

        public object F { get; set; }
    }
}