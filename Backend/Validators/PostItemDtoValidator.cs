using Backend.Features.Items.DTO;
using Backend.Features.Items.Enums;
using FluentValidation;

namespace Backend.Validators;

public class PostItemDtoValidator:AbstractValidator<PostItemDto>
{
    public PostItemDtoValidator()
    {
        RuleFor(x => x.OwnerId)
            .NotEmpty().WithMessage("OwnerId is required.");
        
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .Must(BeAValidEnum<ItemCategory>).WithMessage(GetValidEnumMessage<ItemCategory>("Category"));

        RuleFor(x => x.Condition)
            .NotEmpty().WithMessage("Condition is required.")
            .Must(BeAValidEnum<ItemCondition>).WithMessage(GetValidEnumMessage<ItemCondition>("Condition"));
    }
    
    private static bool BeAValidEnum<TEnum>(string value) where TEnum : struct, Enum
    {
        return Enum.TryParse(typeof(TEnum), value, true, out _);
    }
    private static string GetValidEnumMessage<TEnum>(string propertyName) where TEnum : struct, Enum
    {
        var validValues = Enum.GetNames<TEnum>().ToList();
        return $"{propertyName} must be one of: {string.Join(", ", validValues)}.";
    }
}
