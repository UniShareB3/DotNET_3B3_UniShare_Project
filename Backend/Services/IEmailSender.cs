namespace Backend.Services;

public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string body);
    Task SendEmailVerificationAsync(string toEmail, string verificationCode);
    Task SendPasswordResetEmailAsync(string toEmail, string resetToken, Guid userId);
}
