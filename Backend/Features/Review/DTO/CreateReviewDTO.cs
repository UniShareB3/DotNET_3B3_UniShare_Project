namespace Backend.Features.Review.DTO;

public record CreateReviewDto(Guid BookingId, Guid ReviewerId, Guid? TargetUserId, Guid? TargetItemId, int Rating, string? Comment, DateTime CreatedAt);