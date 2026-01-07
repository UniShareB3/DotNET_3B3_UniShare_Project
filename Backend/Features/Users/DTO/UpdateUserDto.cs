namespace Backend.Features.Users.DTO;

public record UpdateUserDto(
    string? FirstName,
    string? LastName,
    string? Email,
    string? Password,
    string? UniversityName
);

