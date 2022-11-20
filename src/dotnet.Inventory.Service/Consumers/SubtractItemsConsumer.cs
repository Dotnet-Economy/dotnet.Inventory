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
    public class SubtractItemsConsumer : IConsumer<SubtractItems>
    {
        private readonly IRepository<InventoryItem> inventoryItemsRepository;
        private readonly IRepository<CatalogItem> catalogItemsRepository;
        private readonly ILogger<SubtractItems> logger;
        public SubtractItemsConsumer(IRepository<InventoryItem> inventoryItemsRepository, IRepository<CatalogItem> catalogItemsRepository, ILogger<SubtractItems> logger)
        {
            this.catalogItemsRepository = catalogItemsRepository;
            this.inventoryItemsRepository = inventoryItemsRepository;
            this.logger = logger;
        }

        public async Task Consume(ConsumeContext<SubtractItems> context)
        {
            var message = context.Message;
            var item = await catalogItemsRepository.GetAsync(message.CatalogItemId);
            if (item == null) throw new UnknownItemException(message.CatalogItemId);
            var inventoryItem = await inventoryItemsRepository.GetAsync(item => item.UserId == message.UserId
                                                                        && item.CatalogItemId == message.CatalogItemId);
            
            logger.LogInformation(
                "Subtracting {Quantity} qty of Item:{CatalogItemId} from User:{UserId}. CorrelationId:{CorrelationId}",
                message.Quantity,
                message.CatalogItemId,
                message.UserId,
                context.Message.CorrelationId
            );
            
            if (inventoryItem != null)
            {
                if (inventoryItem.MessageIds.Contains(context.MessageId.Value))
                {
                    await context.Publish(new InventoryItemsSubtracted(message.CorrelationId));
                    return;
                }
                inventoryItem.Quantity -= message.Quantity;
                inventoryItem.MessageIds.Add(context.MessageId.Value);
                await inventoryItemsRepository.UpdateAsync(inventoryItem);
            }

            var itemsSubtractedTask = context.Publish(new InventoryItemsSubtracted(message.CorrelationId));
            var inventoryUpdatedTask = context.Publish(new InventoryItemUpdated(
                inventoryItem.UserId,
                inventoryItem.CatalogItemId,
                inventoryItem.Quantity
            ));

            await Task.WhenAll(inventoryUpdatedTask, itemsSubtractedTask);
        }
    }
}