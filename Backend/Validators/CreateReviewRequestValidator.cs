using Backend.Data;
using Backend.Features.Bookings.Enums;
using Backend.Features.Review.CreateReview;
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

        RuleFor(x => x.Review)
            .NotNull()
            .ChildRules(review =>
            {
                review.RuleFor(r => r.Rating)
                    .InclusiveBetween(1, 5).WithMessage("Rating needs to be between 1 and 5.");
                
                review.RuleFor(r => r.ReviewerId)
                    .NotEmpty().WithMessage("The reviewer's ID (ReviewerId) is required.");
                
                review.RuleFor(r => r.BookingId)
                    .NotEmpty().WithMessage("The booking ID (BookingId) associated with the review is required.");
            })
            .Must(x => x.TargetItemId.HasValue ^ x.TargetUserId.HasValue)
            .WithMessage("A review must target either an item (TargetItemId) or a user (TargetUserId), but not both.")
            .CustomAsync(ValidateReviewContextAsync);
    }

    private async Task ValidateReviewContextAsync(CreateReviewDto dto, ValidationContext<CreateReviewRequest> context, CancellationToken cancellationToken)
    {
        var booking = await GetBookingWithItemAsync(dto.BookingId, cancellationToken);
        
        if (!ValidateBookingExists(booking, context))
            return;

        if (!ValidateBookingIsCompleted(booking!, context))
            return;

        var (isReviewerTheBorrower, isReviewerTheOwner) = DetermineReviewerRole(booking!, dto.ReviewerId);
        
        if (!ValidateReviewerIsParticipant(isReviewerTheBorrower, isReviewerTheOwner, context))
            return;

        if (await HasDuplicateReviewAsync(dto.BookingId, dto.ReviewerId, cancellationToken))
        {
            context.AddFailure(nameof(dto.BookingId), "A review from this reviewer for the specified booking already exists.");
            return;
        }

        ValidateReviewTarget(dto, booking!, isReviewerTheBorrower, context);
    }

    private async Task<Booking?> GetBookingWithItemAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        return await _dbContext.Bookings
            .Include(b => b.Item)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
    }

    private static bool ValidateBookingExists(Booking? booking, ValidationContext<CreateReviewRequest> context)
    {
        if (booking == null)
        {
            context.AddFailure(nameof(CreateReviewDto.BookingId), "The specified booking does not exist.");
            return false;
        }
        return true;
    }

    private static bool ValidateBookingIsCompleted(Booking booking, ValidationContext<CreateReviewRequest> context)
    {
        if (booking.BookingStatus != BookingStatus.Completed)
        {
            context.AddFailure(nameof(CreateReviewDto.BookingId), 
                $"Reviews can be created only for the bookings with the status '{BookingStatus.Completed}'. The current status is'{booking.BookingStatus}'.");
            return false;
        }
        return true;
    }

    private static (bool IsReviewerTheBorrower, bool IsReviewerTheOwner) DetermineReviewerRole(Booking booking, Guid reviewerId)
    {
        var ownerId = booking.Item?.OwnerId;
        var isReviewerTheBorrower = booking.BorrowerId == reviewerId;
        var isReviewerTheOwner = ownerId.HasValue && ownerId.Value == reviewerId;
        
        return (isReviewerTheBorrower, isReviewerTheOwner);
    }

    private static bool ValidateReviewerIsParticipant(bool isReviewerTheBorrower, bool isReviewerTheOwner, ValidationContext<CreateReviewRequest> context)
    {
        if (!isReviewerTheBorrower && !isReviewerTheOwner)
        {
            context.AddFailure(nameof(CreateReviewDto.ReviewerId), 
                "The reviewer must be either the borrower or the owner associated with the specified booking.");
            return false;
        }
        return true;
    }

    private async Task<bool> HasDuplicateReviewAsync(Guid bookingId, Guid reviewerId, CancellationToken cancellationToken)
    {
        return await _dbContext.Reviews
            .AnyAsync(r => r.BookingId == bookingId && r.ReviewerId == reviewerId, cancellationToken);
    }

    private static void ValidateReviewTarget(CreateReviewDto dto, Booking booking, bool isReviewerTheBorrower, ValidationContext<CreateReviewRequest> context)
    {
        if (dto.TargetItemId.HasValue)
        {
            ValidateItemReview(dto, booking, isReviewerTheBorrower, context);
        }
        else if (dto.TargetUserId.HasValue)
        {
            ValidateUserReview(dto, booking, isReviewerTheBorrower, context);
        }
    }

    private static void ValidateItemReview(CreateReviewDto dto, Booking booking, bool isReviewerTheBorrower, ValidationContext<CreateReviewRequest> context)
    {
        if (!isReviewerTheBorrower)
        {
            context.AddFailure(nameof(CreateReviewDto.ReviewerId), "Only the borrower can create reviews targeting the item.");
            return;
        }

        if (dto.TargetItemId!.Value != booking.ItemId)
        {
            context.AddFailure(nameof(CreateReviewDto.TargetItemId), 
                "The TargetItemId must match the item associated with the specified booking.");
        }
    }

    private static void ValidateUserReview(CreateReviewDto dto, Booking booking, bool isReviewerTheBorrower, ValidationContext<CreateReviewRequest> context)
    {
        var ownerId = booking.Item?.OwnerId;
        var otherPartyId = isReviewerTheBorrower ? ownerId : booking.BorrowerId;

        if (dto.TargetUserId!.Value != otherPartyId)
        {
            context.AddFailure(nameof(CreateReviewDto.TargetUserId), 
                "The TargetUserId must match the other party (owner or borrower) associated with the specified booking.");
        }
    }
}