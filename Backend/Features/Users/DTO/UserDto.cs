namespace Backend.Features.Users.Dtos;

public class UserDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string UniversityName { get; set; } = string.Empty;
    public Guid id { get; set; }
    public List<string> Items { get; set; } = new();
}
