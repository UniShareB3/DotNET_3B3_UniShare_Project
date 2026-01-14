using Backend.Features.Conversations.DTO;
using Backend.Features.Conversations.GenerateUploadUrl;
using Backend.Validators;
using FluentAssertions;

namespace Backend.Tests.Validators;

public class GenerateUploadUrlRequestValidatorTests
{
    #region Valid Cases

    [Fact]
    public async Task Given_ValidJpegFile_When_Validate_Then_ReturnsValid()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "test.jpg",
            ContentType = "image/jpeg"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Given_ValidPngFile_When_Validate_Then_ReturnsValid()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "image.png",
            ContentType = "image/png"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Given_ValidPdfFile_When_Validate_Then_ReturnsValid()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "document.pdf",
            ContentType = "application/pdf"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Given_ValidDocxFile_When_Validate_Then_ReturnsValid()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "document.docx",
            ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Given_ValidTxtFile_When_Validate_Then_ReturnsValid()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "notes.txt",
            ContentType = "text/plain"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Given_ValidZipFile_When_Validate_Then_ReturnsValid()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "archive.zip",
            ContentType = "application/zip"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Given_ValidGifFile_When_Validate_Then_ReturnsValid()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "animation.gif",
            ContentType = "image/gif"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Given_ValidWebpFile_When_Validate_Then_ReturnsValid()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "image.webp",
            ContentType = "image/webp"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Given_ValidDocFile_When_Validate_Then_ReturnsValid()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "document.doc",
            ContentType = "application/msword"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Given_ValidXlsxFile_When_Validate_Then_ReturnsValid()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "spreadsheet.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Given_ValidXlsFile_When_Validate_Then_ReturnsValid()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "spreadsheet.xls",
            ContentType = "application/vnd.ms-excel"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Given_JpegExtension_WithAlternativeContentType_When_Validate_Then_ReturnsValid()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "photo.jpeg",
            ContentType = "image/pjpeg"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Given_PngExtension_WithAlternativeContentType_When_Validate_Then_ReturnsValid()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "image.png",
            ContentType = "image/x-png"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Given_ZipExtension_WithAlternativeContentType_When_Validate_Then_ReturnsValid()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "archive.zip",
            ContentType = "application/x-zip-compressed"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region FileName Validation Tests

    [Fact]
    public async Task Given_EmptyFileName_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "",
            ContentType = "image/jpeg"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "File name is required.");
    }

    [Fact]
    public async Task Given_NullFileName_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = null!,
            ContentType = "image/jpeg"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "File name is required.");
    }

    [Fact]
    public async Task Given_WhitespaceFileName_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "   ",
            ContentType = "image/jpeg"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Dto.FileName");
    }

    [Fact]
    public async Task Given_FileNameTooLong_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = new string('a', 256) + ".jpg",
            ContentType = "image/jpeg"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "File name must not exceed 255 characters.");
    }

    [Fact]
    public async Task Given_InvalidFileExtension_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "file.exe",
            ContentType = "application/x-msdownload"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Dto.FileName");
    }

    [Fact]
    public async Task Given_FileNameWithoutExtension_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "filenoextension",
            ContentType = "image/jpeg"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Dto.FileName");
    }

    [Fact]
    public async Task Given_FileNameWithUpperCaseExtension_When_Validate_Then_ReturnsValid()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "file.JPG",
            ContentType = "image/jpeg"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region ContentType Validation Tests

    [Fact]
    public async Task Given_EmptyContentType_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "test.jpg",
            ContentType = ""
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Content type is required.");
    }

    [Fact]
    public async Task Given_NullContentType_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "test.jpg",
            ContentType = null!
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Content type is required.");
    }

    [Fact]
    public async Task Given_WhitespaceContentType_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "test.jpg",
            ContentType = "   "
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Dto.ContentType");
    }

    [Fact]
    public async Task Given_InvalidContentType_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "test.jpg",
            ContentType = "application/x-msdownload"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Dto.ContentType");
    }

    [Fact]
    public async Task Given_ContentTypeWithUpperCase_When_Validate_Then_ReturnsValid()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "test.jpg",
            ContentType = "IMAGE/JPEG"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region ContentType and Extension Matching Tests

    [Fact]
    public async Task Given_MismatchedContentTypeAndExtension_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "document.pdf",
            ContentType = "image/jpeg"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Content type does not match file extension.");
    }

    [Fact]
    public async Task Given_JpgExtensionWithPngContentType_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "image.jpg",
            ContentType = "image/png"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Content type does not match file extension.");
    }

    [Fact]
    public async Task Given_PdfExtensionWithWordContentType_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "document.pdf",
            ContentType = "application/msword"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Content type does not match file extension.");
    }

    [Fact]
    public async Task Given_JpgExtension_WithImageJpgContentType_When_Validate_Then_ReturnsValid()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "photo.jpg",
            ContentType = "image/jpg"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Given_DocxExtensionWithDocContentType_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "document.docx",
            ContentType = "application/msword"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Content type does not match file extension.");
    }

    [Fact]
    public async Task Given_XlsxExtensionWithXlsContentType_When_Validate_Then_ReturnsValidationError()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "spreadsheet.xlsx",
            ContentType = "application/vnd.ms-excel"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Content type does not match file extension.");
    }

    #endregion

    #region Multiple Validation Errors

    [Fact]
    public async Task Given_EmptyFileNameAndContentType_When_Validate_Then_ReturnsMultipleValidationErrors()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "",
            ContentType = ""
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task Given_InvalidExtensionAndContentType_When_Validate_Then_ReturnsMultipleValidationErrors()
    {
        // Arrange
        var dto = new GenerateUploadUrlDto
        {
            FileName = "file.exe",
            ContentType = "application/x-msdownload"
        };
        var request = new GenerateUploadUrlRequest(dto);
        var validator = new GenerateUploadUrlRequestValidator();

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Count.Should().BeGreaterThan(0);
    }

    #endregion
}

