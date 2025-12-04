namespace Backend.Features.Review.DTO;

public record UpdateReviewDto
(
    int Rating,
    string? Comment
);