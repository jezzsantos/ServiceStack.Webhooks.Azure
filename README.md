[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0) [![Build status](https://ci.appveyor.com/api/projects/status/d9bhik7l5vvblvka/branch/master?svg=true)](https://ci.appveyor.com/project/JezzSantos/servicestack-webhooks-azure/branch/master) [![NuGet](https://img.shields.io/nuget/v/ServiceStack.Webhooks.Azure.svg?label=ServiceStack.Webhooks.Azure)](https://www.nuget.org/packages/ServiceStack.Webhooks.Azure) 

# ServiceStack.Webhooks.Azure
Add Webhooks to your Azure ServiceStack services

# [Release Notes](https://github.com/jezzsantos/ServiceStack.Webhooks.Azure/wiki/Release-Notes)

# Overview

This project makes it very easy to plugin various Azure components to the [ServiceStack.Webhooks architecture](http://github.com/jezzsantos/ServiceStack.Webhooks), which looks like this:

![](https://raw.githubusercontent.com/jezzsantos/ServiceStack.Webhooks/master/docs/images/Webhooks.Architecture.PNG)

If you cant find the exact component you want for your Azure architecture in this `ServiceStack.Webhooks.Azure` package, it is pretty easy for you to build add your own and _just plug it in_ to your `WebhookFeature`.

### Contribute!

Want to get involved in this project? or there is something missing and you want to help improve this capability for your services? just send us a message or pull-request!

# Getting Started

Install from NuGet:
```
Install-Package ServiceStack.Webhooks.Azure
```

If you deploy your ServiceStack web service to Microsoft Azure, you may want to use Azure storage Tables, Queues, Buses etc. to allow your app to scale and perform at scale when using webhooks.

For example, 'subscriptions' can be stored in Azure Table Storage, or Azure SQL, 'events' can be queued in Azure Queue Storage,  Function, or Service Bus, and then 'events' can be relayed by a Function, ServiceBus or WorkerRole to subscribers.

![](https://raw.githubusercontent.com/jezzsantos/ServiceStack.Webhooks.Azure/master/docs/images/Webhooks.Azure.PNG)

Add the `WebhookFeature` in your `AppHost.Configure()` method as usual, and register the Azure components:

```
public override void Configure(Container container)
{
    container.Register<ISubscriptionStore>(new AzureTableSubscriptionStore());
    container.Register<IEventSink>(new AzureQueueEventSink());

    Plugins.Add(new WebhookFeature();
}
```

If you are hosting your web service in an Azure WebRole, you may want to configure the 'subscription store' and the 'event sink' from your cloud configuration (`ServiceConfiguration.cscfg`), instead of using the defaults, or specifying them in code. You would do that by  registering your services like this:

```
public override void Configure(Container container)
{
    var appSettings = new CloudAppSettings();
    container.Register<IAppSettings>(appSettings);

    container.Register<ISubscriptionStore>(new AzureTableSubscriptionStore(appSettings));
    container.Register<IEventSink>(new AzureQueueEventSink(appSettings));

    Plugins.Add(new WebhookFeature();
}
```

Then, in the 'Cloud' project that you have created for your service, edit the properties of the role you have for your web service.

Go to the 'Settings' tab and add the following settings:

* (ConnectionString) AzureTableSubscriptionStore.ConnectionString - The Azure Storage account connection string for your table. For example: UseDevelopmentStorage=true
* (string) AzureTableSubscriptionStore.SubscriptionsTable.Name - The name of the storage table where subscriptions are stored. For example: webhooksubscriptions
* (string) AzureTableSubscriptionStore.DeliveryResultsTable.Name - The name of the storage table where delivery results are stored. For example: webhookdeliveryresults
* (ConnectionString) AzureQueueEventSink.ConnectionString - The Azure Storage account connection string for your queue. For example: UseDevelopmentStorage=true
* (string) AzureQueueEventSink.Queue.Name - The name of the queue where events will be written. For example: webhookevents

## Configuring an Azure WorkerRole Relay

To relay events from an Azure queue, you can deploy an Azure WorkerRole that picks up the events from the 'queue' and relays them to all subscribers.
Since you are deploying this component to Azure, the configuration for it will likely exist in your Azure configuration (`ServiceConfiguration.cscfg`) file:

This is how to do it:

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

Note: the value of the setting `EventRelayQueueProcessor.TargetQueue.Name` must be the same as the `AzureQueueEventSink.QueueName` that you may have configured in the `WebhookFeature`.

## Configuring Azure Storage Resources

By default, these services will use default names for storage table and event queue, and will connect to the local Azure Emulator (UseDevelopmentStorage=true) storage account, which might be fine for you, but you may want to provide different names or connection strings for different deployments.

* `AzureTableSubscriptionStore` will create and use a storage table named: 'webhooksubscriptions'
* `AzureQueueEventSink` will create and use a storage queue named: 'webhookevents'

If you use the overload constructors, and pass in the `IAppSettings`, like this, you can load settings from your Azure cloud configuration (`ServiceConfiguration.cscfg`):

```
public override void Configure(Container container)
{
    var appSettings = new CloudAppSettings();
    container.Register<IAppSettings>(appSettings);

    container.Register<ISubscriptionStore>(new AzureTableSubscriptionStore(appSettings));
    container.Register<IEventSink>(new AzureQueueEventSink(appSettings));

    Plugins.Add(new WebhookFeature();
}
```
Then for your selected configuration, add these settings:

* (ConnectionString) AzureTableSubscriptionStore.ConnectionString - The storage account for storing subscriptions.
* (string) AzureTableSubscriptionStore.SubscriptionsTable.Name - The name of the storage table where subscriptions are stored. For example: webhooksubscriptions
* (string) AzureTableSubscriptionStore.DeliveryResultsTable.Name - The name of the storage table where delivery results are stored. For example: webhookdeliveryresults
* (ConnectionString) AzureQueueEventSink.ConnectionString - The storage account for storing events.
* (string) AzureQueueEventSink.Queue.Name - The name of the queue where events are stored before being relayed: For example: webhookevents

Otherwise, you can set those values directly in code when you register the services:

```
public override void Configure(Container container)
{
    container.Register<ISubscriptionStore>(new AzureTableSubscriptionStore
        {
            SubscriptionTableName = "mytablename",
            DeliveryResultsTableName = "mytablename",
            ConnectionString = "mystorageaccount"
        });
    container.Register<IEventSink>(new AzureQueueEventSink
        {
            QueueName = "myqueuename",
            ConnectionString = "mystorageaccount"
        });

    Plugins.Add(new WebhookFeature();
}
```



