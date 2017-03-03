[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0) [![Build status](https://ci.appveyor.com/api/projects/status/d9bhik7l5vvblvka/branch/master?svg=true)](https://ci.appveyor.com/project/JezzSantos/servicestack-webhooks-azure/branch/master) [![NuGet](https://img.shields.io/nuget/v/ServiceStack.Webhooks.Azure.svg?label=ServiceStack.Webhooks.Azure)](https://www.nuget.org/packages/ServiceStack.Webhooks.Azure) 

# ServiceStack.Webhooks.Azure
Add Webhooks to your Azure ServiceStack services

# Overview

This project makes it very easy to plugin Azure components of the http://github.com/jezzsantos/ServiceStack.Webhooks architecture.


![](https://raw.githubusercontent.com/jezzsantos/ServiceStack.Webhooks.Azure/master/docs/images/Webhooks.Architecture.PNG)

If you cant find the component you want for your Azure architecture, it should be easy for you to build add your own and _just plug it in_.

### Contribute!

Want to get involved in this project? or want to help improve this capability for your services? just send us a message or pull-request!

# Getting Started

Install from NuGet:
```
Install-Package ServiceStack.Webhooks.Azure
```

If you deploy your web service to Microsoft Azure, you may want to use Azure storage Tables and Queues etc. to implement the various components of the webhooks.

For example, 'subscriptions' can be stored in Azure Table Storage, 'events' can be queued in a Azure Queue Storage, and then 'events' can be relayed by a WorkerRole to subscribers.

![](https://raw.githubusercontent.com/jezzsantos/ServiceStack.Webhooks/master/docs/images/Webhooks.Azure.PNG)

Add the `WebhookFeature` in your `AppHost.Configure()` method as usual, and register the Azure components:

```
public override void Configure(Container container)
{
    container.Register<IWebhookSubscriptionStore>(new AzureTableWebhookSubscriptionStore());
    container.Register<IWebhookEventSink>(new AzureQueueWebhookEventSink());

    Plugins.Add(new WebhookFeature();
}
```

If you are hosting your web service in an Azure WebRole, you may want to configure the 'subscription store' and the 'event sink' from your cloud configuration, instead of using the defaults, or specifying them in code, then register the services like this:

```
public override void Configure(Container container)
{
    var appSettings = new CloudAppSettings();
    container.Register<IAppSettings>(appSettings);

    container.Register<IWebhookSubscriptionStore>(new AzureTableWebhookSubscriptionStore(appSettings));
    container.Register<IWebhookEventSink>(new AzureQueueWebhookEventSink(appSettings));

    Plugins.Add(new WebhookFeature();
}
```

Then, in the 'Cloud' project that you have created for your service, edit the properties of the role you have for your web service.

Go to the 'Settings' tab and add the following settings:

* (ConnectionString) AzureTableWebhookSubscriptionStore.ConnectionString - The Azure Storage account connection string for your table. For example: UseDevelopmentStorage=true
* (string) AzureTableWebhookSubscriptionStore.Table.Name - The name of the table where subscriptions will be stored. For example: webhooksubscriptions
* (ConnectionString) AzureQueueWebhookEventSink.ConnectionString - The Azure Storage account connection string for your queue. For example: UseDevelopmentStorage=true
* (string) AzureQueueWebhookEventSink.Queue.Name - The name of the queue where events will be written. For example: webhookevents

### Configuring an Azure WorkerRole Relay

Now you can deploy an Azure WorkerRole that can query the events 'queue' and relay those events to all subscribers.
Since you are deploying this component to Azure, the configuration for it will exist in your Azure configuration files: `ServiceConfiguration.cscfg`

Create a new 'Azure Cloud Service' project in your solution, and add a 'WorkerRole' to it. (in this example we will name it "WebhookEventRelay")

In the new 'WebhookEventRelay' project, install the nuget package:

```
Install-Package Servicestack.Webhooks.Azure
```

In the 'WorkerRole.cs' file that was created for you, replace the 'WorkerRole' class with this code:

```
public class WorkerRole : AzureWorkerRoleEntryPoint
    {
        private List<WorkerEntryPoint> workers;

        protected override IEnumerable<WorkerEntryPoint> Workers
        {
            get { return workers; }
        }

        public override void Configure(Container container)
        {
            base.Configure(container);

            container.Register<IAppSettings>(new CloudAppSettings());
            container.Register(new EventRelayWorker(container));

            workers = new List<WorkerEntryPoint>
            {
                container.Resolve<EventRelayWorker>(),
                // (Add other types if you want to use this WorkerRole for multiple workloads)
            };
        }
    }
```

In the 'Cloud' project that you created, edit the properties of the 'WebhookEventRelay' role.

Go to the 'Settings' tab and add the following settings:

* (string) SubscriptionServiceClient.SubscriptionService.BaseUrl - The base URL of your webhook subscription service (where the `WebhookFeature` is installed ). For example: http://myserver:80/api
* (string) EventRelayQueueProcessor.Polling.Interval.Seconds - The interval (in seconds) that the worker role polls the Azure queue for new events. For example: 5
* (ConnectionString) EventRelayQueueProcessor.ConnectionString - The Azure Storage account connection string. For example: UseDevelopmentStorage=true
* (string) EventRelayQueueProcessor.TargetQueue.Name - The name of the queue where events will be polled. For example: webhookevents
* (string) EventRelayQueueProcessor.UnhandledQueue.Name - The name of the queue where failed events are dropped. For example: unhandledwebhookevents
* (string) EventRelayQueueProcessor.ServiceClient.Retries - The number of retry attempts the relay will make to notify a subscriber before giving up. For example: 3
* (string) EventRelayQueueProcessor.ServiceClient.Timeout.Seconds - The timeout (in seconds) the relay will wait for the subscriber endpoint before cancelling the notification. For example: 60

Note: the value of the setting `EventRelayQueueProcessor.TargetQueue.Name` must be the same as the `AzureQueueWebhookEventSink.QueueName` that you may have configured in the `WebhookFeature`.

### Configuring Azure Storage Credentials

By default, these services will connect to the local Azure Emulator (UseDevelopmentStorage=true) which might be fine for testing your service, but after you have deployed to your cloud, you will want to provide different storage connection strings.

If you use the overload constructors, and pass in the `IAppSettings`, like this, you can load settings from your Azure cloud configuration:

```
public override void Configure(Container container)
{
    var appSettings = new CloudAppSettings();
    container.Register<IAppSettings>(appSettings);

    container.Register<IWebhookSubscriptionStore>(new AzureTableWebhookSubscriptionStore(appSettings));
    container.Register<IWebhookEventSink>(new AzureQueueWebhookEventSink(appSettings));

    Plugins.Add(new WebhookFeature();
}
```
then from your current ServiceConfiguration.<Configuration>.cscfg file:

* `AzureTableWebhookSubscriptionStore` will try to use a setting called: 'AzureTableWebhookSubscriptionStore.ConnectionString' for its storage connection
* `AzureQueueWebhookEventSink` will try to use a setting called: 'AzureQueueWebhookEventSink.ConnectionString' for its storage connection

Otherwise, you can set those values directly in code when you register the services:

```
public override void Configure(Container container)
{
    container.Register<IWebhookSubscriptionStore>(new AzureTableWebhookSubscriptionStore
        {
            ConnectionString = "connectionstring",
        });
    container.Register<IWebhookEventSink>(new AzureQueueWebhookEventSink
        {
            ConnectionString = "connectionstring",
        });

    Plugins.Add(new WebhookFeature();
}
```

### Configuring Azure Storage Resources

By default, 

* `AzureTableWebhookSubscriptionStore` will create and use a storage table named: 'webhooksubscriptions'
* `AzureQueueWebhookEventSink` will create and use a storage queue named: 'webhookevents'

You can change those values when you register the services.

```
public override void Configure(Container container)
{
    container.Register<IWebhookSubscriptionStore>(new AzureTableWebhookSubscriptionStore
        {
            TableName = "mytablename",
        });
    container.Register<IWebhookEventSink>(new AzureQueueWebhookEventSink
        {
            QueueName = "myqueuename",
        });

    Plugins.Add(new WebhookFeature();
}
```



