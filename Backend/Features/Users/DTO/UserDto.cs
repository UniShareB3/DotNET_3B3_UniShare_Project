namespace Backend.Features.Users.DTO;

public class UserDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string UniversityName { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public List<string> Items { get; set; } = new();
}
