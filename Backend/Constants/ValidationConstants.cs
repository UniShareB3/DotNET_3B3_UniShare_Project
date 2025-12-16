namespace Backend.Constants;

/// <summary>
/// Constants for field length validations and constraints
/// </summary>
public static class ValidationConstants
{
    // String Length Constants
    public const int MaxDescriptionLength = 1000;
    public const int MaxReasonLength = 1000;
    public const int MaxNameLength = 255;
    public const int MaxCommentLength = 500;
    public const int MaxCategoryLength = 50;
    public const int MaxConditionLength = 50;
    public const int MaxUniversityNameLength = 100;
    public const int MaxShortCodeLength = 10;
    public const int MaxEmailDomainLength = 255;
    
    // Other Constants
    public const int MinRating = 1;
    public const int MaxRating = 5;
    public const int DefaultPageSize = 20;
    
    // Report Constants
    public const int MaxDeclinedReportsPerItem = 5;
    public const int MaxDaysForPendingReports = 7;
    
}

