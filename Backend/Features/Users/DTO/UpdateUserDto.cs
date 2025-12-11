namespace Backend.Features.Users.Dtos;

public record UpdateUserDto(
    string? FirstName,
    string? LastName,
    string? Email,
    string? Password,
    string? UniversityName
);

