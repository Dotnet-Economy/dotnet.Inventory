using System;

namespace dotnet.Inventory.Service.Dtos
{
    public record GrantItemsDto(Guid UserId, Guid CatalogItemId, int Quantity);
    public record InventoryItemDto(Guid CatalogItemId, int Quantity, DateTimeOffset AcquireDate);

}