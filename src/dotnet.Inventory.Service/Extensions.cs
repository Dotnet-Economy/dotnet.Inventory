using dotnet.Inventory.Service.Dtos;
using dotnet.Inventory.Service.Entities;

namespace dotnet.Inventory.Service
{
    public static class Extensions
    {
        public static InventoryItemDto AsDto(this InventoryItem item, string Name, string Description)
        {
            return new InventoryItemDto(item.CatalogItemId, Name, Description, item.Quantity, item.AcquireDate);
        }
    }
}