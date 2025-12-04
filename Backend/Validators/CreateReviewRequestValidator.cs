using Backend.Features.Bookings.Enums;
using Backend.Features.Review;
using Backend.Features.Review.DTO;
using Backend.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Backend.Validators;

public class CreateReviewRequestValidator : AbstractValidator<CreateReviewRequest>
{
    private readonly ApplicationContext _dbContext;

    public CreateReviewRequestValidator(ApplicationContext dbContext)
    {
        _dbContext = dbContext;

        RuleFor(x => x.Review).NotNull();

        RuleFor(x => x.Review.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating needs to be between 1 and 5.");
        
        RuleFor(x => x.Review.ReviewerId)
            .NotEmpty().WithMessage("The reviewer's ID (ReviewerId) is required.");
        
        RuleFor(x => x.Review.BookingId)
            .NotEmpty().WithMessage("The booking ID (BookingId) associated with the review is required.");

        RuleFor(x => x.Review)
            .Must(x => x.TargetItemId.HasValue ^ x.TargetUserId.HasValue)
            .WithMessage("A review must target either an item (TargetItemId) or a user (TargetUserId), but not both.");

        RuleFor(x => x.Review)
            .CustomAsync(ValidateReviewContextAsync);
    }

    private async Task ValidateReviewContextAsync(CreateReviewDTO dto, ValidationContext<CreateReviewRequest> context, CancellationToken cancellationToken)
    {
        var booking = await _dbContext.Bookings
            .Include(b => b.Item)
            .FirstOrDefaultAsync(b => b.Id == dto.BookingId, cancellationToken);
        
        if (booking == null)
        {
            context.AddFailure(nameof(dto.BookingId), "The specified booking does not exist.");
            return;
        }

        if (booking.BookingStatus != BookingStatus.Completed)
        {
            context.AddFailure(nameof(dto.BookingId), $"Reviews can be created only for the bookings with the status '{BookingStatus.Completed}'. The current status is'{booking.BookingStatus}'.");
            return;
        }

        var ownerId = booking.Item?.OwnerId;
        var isReviewerTheBorrower = booking.BorrowerId == dto.ReviewerId;
        var isReviewerTheOwner = ownerId.HasValue && ownerId.Value == dto.ReviewerId;
        
        if (!isReviewerTheBorrower && !isReviewerTheOwner)
        {
            context.AddFailure(nameof(dto.ReviewerId), "The reviewer must be either the borrower or the owner associated with the specified booking.");
            return;
        }

        var hasExistingReview = await _dbContext.Reviews
            .AnyAsync(r => r.BookingId == dto.BookingId && r.ReviewerId == dto.ReviewerId, cancellationToken);
        
        if (hasExistingReview)
        {
            context.AddFailure(nameof(dto.BookingId), "A review from this reviewer for the specified booking already exists.");
            return;
        }

        if (dto.TargetItemId.HasValue)
        {
            if (!isReviewerTheBorrower)
            {
                context.AddFailure(nameof(dto.ReviewerId), "Only the borrower can create reviews targeting the item.");
                return;
            }

            if (dto.TargetItemId.Value != booking.ItemId)
            {
                context.AddFailure(nameof(dto.TargetItemId), "The TargetItemId must match the item associated with the specified booking.");
            }
        }
        else if (dto.TargetUserId.HasValue)
        {
            var otherPartyId = isReviewerTheBorrower ? ownerId : booking.BorrowerId;

            if (dto.TargetUserId.Value != otherPartyId)
            {
                context.AddFailure(nameof(dto.TargetUserId), "The TargetUserId must match the other party (owner or borrower) associated with the specified booking.");
            }
        }
    }
}