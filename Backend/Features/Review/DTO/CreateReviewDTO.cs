namespace Backend.Features.Review.DTO;

public record CreateReviewDTO(Guid BookingId, Guid ReviewerId, Guid? TargetUserId, Guid? TargetItemId, int Rating, string? Comment, DateTime CreatedAt);