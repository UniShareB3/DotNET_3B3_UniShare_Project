using AutoMapper;
using Backend.Features.Items.Enums;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;
using ItemDto = Backend.Features.Items.DTO.ItemDto;

namespace Backend.Features.Items.PatchItem;

public class PatchItemHandler(ApplicationContext dbContext, IMapper mapper) : IRequestHandler<PatchItemRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<PatchItemHandler>();

    public async Task<IResult> Handle(PatchItemRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Attempting to update item {ItemId}", request.ItemId);

        var item = await dbContext.Items
            .Include(i => i.Owner)
            .FirstOrDefaultAsync(i => i.Id == request.ItemId, cancellationToken);

        if (item == null)
        {
            _logger.Warning("Update failed: Item {ItemId} not found.", request.ItemId);
            return Results.NotFound(new { error = "Item not found" });
        }

        var dto = request.PatchItemDto;

        // Update ItemName if provided
        if (!string.IsNullOrWhiteSpace(dto.ItemName))
        {
            item.Name = dto.ItemName;
            _logger.Information("Updated Name for item {ItemId}", request.ItemId);
        }

        // Update Description if provided
        if (!string.IsNullOrWhiteSpace(dto.Description))
        {
            item.Description = dto.Description;
            _logger.Information("Updated Description for item {ItemId}", request.ItemId);
        }

        // Update Category if provided
        if (!string.IsNullOrWhiteSpace(dto.Category))
        {
            if (Enum.TryParse<ItemCategory>(dto.Category, true, out var category))
            {
                item.Category = category;
                _logger.Information("Updated Category to {Category} for item {ItemId}", dto.Category, request.ItemId);
            }
            else
            {
                _logger.Warning("Invalid category {Category} for item {ItemId}", dto.Category, request.ItemId);
                return Results.BadRequest(new { error = $"Invalid category '{dto.Category}'" });
            }
        }

        // Update Condition if provided
        if (!string.IsNullOrWhiteSpace(dto.Condition))
        {
            if (Enum.TryParse<ItemCondition>(dto.Condition, true, out var condition))
            {
                item.Condition = condition;
                _logger.Information("Updated Condition to {Condition} for item {ItemId}", dto.Condition, request.ItemId);
            }
            else
            {
                _logger.Warning("Invalid condition {Condition} for item {ItemId}", dto.Condition, request.ItemId);
                return Results.BadRequest(new { error = $"Invalid condition '{dto.Condition}'" });
            }
        }

        // Update BlobName if provided
        if (dto.BlobName != null)
        {
            item.BlobName = dto.BlobName;
            _logger.Information("Updated BlobName for item {ItemId}", request.ItemId);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.Information("Successfully updated item {ItemId}", request.ItemId);

            var itemDto = mapper.Map<ItemDto>(item);
            return Results.Ok(itemDto);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error while updating item {ItemId}", request.ItemId);
            return Results.InternalServerError("An unexpected error occurred while updating the item.");
        }
    }
}
