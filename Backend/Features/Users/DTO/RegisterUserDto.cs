namespace Backend.Features.Users.DTO;


public record RegisterUserDto(string Email, string FirstName, string LastName, string Password, string UniversityName);