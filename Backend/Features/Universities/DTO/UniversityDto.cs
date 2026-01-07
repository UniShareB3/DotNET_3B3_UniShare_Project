namespace Backend.Features.Universities.DTO;

public class UniversityDto
{
    public required string Name { get; set; }
    public required string ShortCode { get; set; }
    public required string EmailDomain { get; set; }
    public Guid Id { get; set; }
}