using Backend.Features.Conversations.GenerateUploadUrl;
using FluentValidation;

namespace Backend.Validators;

public class GenerateUploadUrlRequestValidator : AbstractValidator<GenerateUploadUrlRequest>
{
    private static readonly string[] AllowedContentTypes = 
    {
        "image/jpeg", "image/jpg", "image/pjpeg", "image/png", "image/x-png", "image/gif", "image/webp",
        "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/plain", "application/zip", "application/x-zip-compressed"
    };

    private static readonly string[] AllowedExtensions = 
    { 
        ".jpg", ".jpeg", ".png", ".gif", ".webp", 
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", 
        ".txt", ".zip" 
    };

    public GenerateUploadUrlRequestValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("File name is required.")
            .Must(HaveValidExtension)
            .WithMessage($"Invalid file extension. Allowed extensions: {string.Join(", ", AllowedExtensions)}")
            .MaximumLength(255)
            .WithMessage("File name must not exceed 255 characters.");

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .WithMessage("Content type is required.")
            .Must(BeValidContentType)
            .WithMessage($"Invalid content type. Allowed types: images (JPEG, PNG, GIF, WebP), documents (PDF, Word, Excel), text files, and ZIP archives.");

        RuleFor(x => x)
            .Must(HaveMatchingContentTypeAndExtension)
            .WithMessage("Content type does not match file extension.");
    }

    private bool HaveValidExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        return !string.IsNullOrEmpty(extension) && AllowedExtensions.Contains(extension);
    }

    private bool BeValidContentType(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        return AllowedContentTypes.Contains(contentType.ToLowerInvariant());
    }

    private bool HaveMatchingContentTypeAndExtension(GenerateUploadUrlRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FileName) || string.IsNullOrWhiteSpace(request.ContentType))
            return true; // Let other validators handle null/empty checks

        var extension = Path.GetExtension(request.FileName)?.ToLowerInvariant();
        var contentType = request.ContentType.ToLowerInvariant();

        // Map extensions to expected content types
        var expectedContentTypes = extension switch
        {
            ".jpg" or ".jpeg" => new[] { "image/jpeg", "image/jpg", "image/pjpeg" },
            ".png" => new[] { "image/png", "image/x-png" },
            ".gif" => new[] { "image/gif" },
            ".webp" => new[] { "image/webp" },
            ".pdf" => new[] { "application/pdf" },
            ".doc" => new[] { "application/msword" },
            ".docx" => new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            ".xls" => new[] { "application/vnd.ms-excel" },
            ".xlsx" => new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            ".txt" => new[] { "text/plain" },
            ".zip" => new[] { "application/zip", "application/x-zip-compressed" },
            _ => Array.Empty<string>()
        };

        return expectedContentTypes.Contains(contentType);
    }
}

