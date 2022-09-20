using dotnet.Inventory.Service.Dtos;
using dotnet.Inventory.Service.Entities;

namespace dotnet.Inventory.Service
{
    public static class Extensions
    {
        public static InventoryItemDto AsDto(this InventoryItem item)
        {
            return new InventoryItemDto(item.CatalogItemId, item.Quantity, item.AcquireDate);
        }
    }
}