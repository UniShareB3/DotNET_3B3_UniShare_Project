namespace Backend.Services.ContentType;

/// <summary>
/// Utility class for resolving MIME content types from file extensions
/// </summary>
public static class ContentTypeResolver
{
    private static readonly Dictionary<string, string> ExtensionToContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        // Images
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".gif", "image/gif" },
        { ".webp", "image/webp" },
        { ".bmp", "image/bmp" },
        { ".svg", "image/svg+xml" },

        // Documents
        { ".pdf", "application/pdf" },
        { ".doc", "application/msword" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { ".xls", "application/vnd.ms-excel" },
        { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        { ".ppt", "application/vnd.ms-powerpoint" },
        { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },

        // Text
        { ".txt", "text/plain" },
        { ".csv", "text/csv" },
        { ".json", "application/json" },
        { ".xml", "application/xml" },

        // Archives
        { ".zip", "application/zip" },
        { ".rar", "application/x-rar-compressed" },
        { ".7z", "application/x-7z-compressed" },
        { ".tar", "application/x-tar" },
        { ".gz", "application/gzip" }
    };

    /// <summary>
    /// Default content type for unknown file extensions
    /// </summary>
    public const string DefaultContentType = "application/octet-stream";

    /// <summary>
    /// Default content type for text messages
    /// </summary>
    public const string TextPlain = "text/plain";

    /// <summary>
    /// Resolves the MIME content type from a file path or file name
    /// </summary>
    /// <param name="filePathOrName">File path or file name with extension</param>
    /// <returns>MIME content type string</returns>
    public static string FromFileName(string? filePathOrName)
    {
        if (string.IsNullOrEmpty(filePathOrName))
            return DefaultContentType;

        var extension = Path.GetExtension(filePathOrName);
        return FromExtension(extension);
    }

    /// <summary>
    /// Resolves the MIME content type from a file extension
    /// </summary>
    /// <param name="extension">File extension (with or without leading dot)</param>
    /// <returns>MIME content type string</returns>
    public static string FromExtension(string? extension)
    {
        if (string.IsNullOrEmpty(extension))
            return DefaultContentType;

        // Ensure extension starts with a dot
        if (!extension.StartsWith('.'))
            extension = "." + extension;

        return ExtensionToContentType.TryGetValue(extension, out var contentType)
            ? contentType
            : DefaultContentType;
    }

    /// <summary>
    /// Checks if the content type represents an image
    /// </summary>
    public static bool IsImage(string? contentType)
    {
        return !string.IsNullOrEmpty(contentType) && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the content type represents a document (non-image, non-text)
    /// </summary>
    public static bool IsDocument(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return false;

        return !IsImage(contentType) && contentType != TextPlain;
    }
}
