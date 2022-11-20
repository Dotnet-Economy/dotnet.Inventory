using System;
using System.Threading.Tasks;
using dotnet.Common;
using dotnet.Inventory.Service.Entities;
using dotnet.Inventory.Service.Exceptions;
using MassTransit;
using dotnet.Inventory.Contracts;
using Microsoft.Extensions.Logging;

namespace dotnet.Inventory.Service.Consumers
{
    public class GrantItemsConsumer : IConsumer<GrantItems>
    {
        private readonly IRepository<InventoryItem> inventoryItemsRepository;
        private readonly IRepository<CatalogItem> catalogItemsRepository;
        private readonly ILogger<GrantItemsConsumer> logger;
        public GrantItemsConsumer(IRepository<InventoryItem> inventoryItemsRepository, IRepository<CatalogItem> catalogItemsRepository, ILogger<GrantItemsConsumer> logger)
        {
            this.catalogItemsRepository = catalogItemsRepository;
            this.inventoryItemsRepository = inventoryItemsRepository;
            this.logger = logger;
        }

        public async Task Consume(ConsumeContext<GrantItems> context)
        {
            var message = context.Message;
            var item = await catalogItemsRepository.GetAsync(message.CatalogItemId);
            if (item == null) throw new UnknownItemException(message.CatalogItemId);
            var inventoryItem = await inventoryItemsRepository.GetAsync(item => item.UserId == message.UserId
                                                                        && item.CatalogItemId == message.CatalogItemId);

            logger.LogInformation(
                "Granting {Quantity} qty of Item:{CatalogItemId} to User:{UserId}. CorrelationId:{CorrelationId}",
                message.Quantity,
                message.CatalogItemId,
                message.UserId,
                context.Message.CorrelationId
            );
            if (inventoryItem == null)
            {
                inventoryItem = new InventoryItem
                {
                    CatalogItemId = message.CatalogItemId,
                    UserId = message.UserId,
                    Quantity = message.Quantity,
                    AcquireDate = DateTimeOffset.UtcNow
                };
                inventoryItem.MessageIds.Add(context.MessageId.Value);
                await inventoryItemsRepository.CreateAsync(inventoryItem);
            }
            else
            {
                if (inventoryItem.MessageIds.Contains(context.MessageId.Value))
                {
                    await context.Publish(new InventoryItemsGranted(message.CorrelationId));
                    return;
                }
                inventoryItem.Quantity += message.Quantity;
                inventoryItem.MessageIds.Add(context.MessageId.Value);

                await inventoryItemsRepository.UpdateAsync(inventoryItem);
            }

            var itemsGrantedTask = context.Publish(new InventoryItemsGranted(message.CorrelationId));
            var inventoryUpdatedTask = context.Publish(new InventoryItemUpdated(
                inventoryItem.UserId,
                inventoryItem.CatalogItemId,
                inventoryItem.Quantity
            ));

            await Task.WhenAll(inventoryUpdatedTask, itemsGrantedTask);
        }
    }
}