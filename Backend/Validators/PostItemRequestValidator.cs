using Backend.Features.Items;
using FluentValidation;

namespace Backend.Validators;

public class PostItemRequestValidator:AbstractValidator<PostItemRequest>
{
    public PostItemRequestValidator(PostItemDtoValidator itemDtoValidator)
    {
        RuleFor(x => x.Item)
            .SetValidator(itemDtoValidator);
    }
}