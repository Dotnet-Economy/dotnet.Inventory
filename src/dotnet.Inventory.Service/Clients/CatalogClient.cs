using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using dotnet.Inventory.Service.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace dotnet.Inventory.Service.Clients
{
    public class CatalogClient
    {
        private readonly HttpClient httpClient;
        public CatalogClient(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        [HttpGet]
        public async Task<IReadOnlyCollection<CatalogItemDto>> GetCatalogItemsAsync()
        {
            var items = await httpClient.GetFromJsonAsync<IReadOnlyCollection<CatalogItemDto>>("/items");
            return items;
        }
    }
}