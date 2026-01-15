using AutoMapper;
using Backend.Data;
using Backend.Features.Items.DTO;
using Backend.Services.AzureStorage;

namespace Backend;

public class ItemUrlResolver : IMappingAction<Item, ItemDto>
{
    private readonly IAzureStorageService _azureService;

    public ItemUrlResolver(IAzureStorageService azureService)
    {
        _azureService = azureService;
    }

    public void Process(Item source, ItemDto destination, ResolutionContext context)
    {
        destination.ImageUrl = !string.IsNullOrEmpty(source.BlobName)
            ? _azureService.GenerateReadSasUrl(source.BlobName, TimeSpan.FromHours(1))
            : source.ImageUrl;
    }
}