﻿using System;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using EventStore.ClientAPI;

using Newtonsoft.Json;

namespace SimpleCQRS.Views
{
    public class SubcribeAndProjector
    {
        const string CategoryStreamName = "$ce-inventory";

        readonly InventoryListView inventoryListView;
        readonly InventoryItemDetailView inventoryView;

        IHostApplicationLifetime appLifeTime;
        EventStoreStreamCatchUpSubscription subscriber;
        Microsoft.Extensions.Logging.ILogger logger;

        public SubcribeAndProjector(InventoryListView inventoryListView, InventoryItemDetailView inventoryView, Microsoft.Extensions.Logging.ILogger logger)
        {
            this.inventoryListView = inventoryListView;
            this.inventoryView = inventoryView;
            this.logger = logger;
        }

        public void ConfigureAndStart(IEventStoreConnection connection, IHostApplicationLifetime applicationLifeTime)
        {
            this.subscriber = connection.SubscribeToStreamFrom(CategoryStreamName, StreamPosition.Start, CatchUpSubscriptionSettings.Default, Project, null, SubscriptionDropped);
            this.appLifeTime = applicationLifeTime;
        }

        void SubscriptionDropped(EventStoreCatchUpSubscription arg1, SubscriptionDropReason reason, Exception ex)
        {
            logger.LogError(ex, $"subscription died restarting reason {reason}");
            appLifeTime.StopApplication();
        }

        Task Project(EventStoreCatchUpSubscription subscription, ResolvedEvent resolvedEvent)
        {
            dynamic evnt = ToEvent(resolvedEvent);

            //if you have many or do IO can do in parallel or asyc
            inventoryListView.Handle(evnt); 
            inventoryView.Handle(evnt);
            return Task.CompletedTask;
        }

        static Event ToEvent(ResolvedEvent storeEvent)
        {
            var type = Type.GetType(storeEvent.Event.EventType);
            var json = Encoding.UTF8.GetString(storeEvent.Event.Data);
            return (Event)JsonConvert.DeserializeObject(json, type);
        }
    }
}