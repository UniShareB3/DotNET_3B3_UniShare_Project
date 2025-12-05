namespace Backend.Features.Users.Dtos;


public record RegisterUserDto(string Email, string FirstName, string LastName, string Password, string UniversityName);