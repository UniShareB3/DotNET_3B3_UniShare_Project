using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Backend.Services;

public class MailKitEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public MailKitEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_configuration["Smtp:From"]));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        
        var host = _configuration["Smtp:Host"];
        var port = int.Parse(_configuration["Smtp:Port"] ?? "587");
        var useSsl = _configuration.GetValue<bool>("Smtp:UseSsl", true);
        
        await client.ConnectAsync(host, port, useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
        
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        
        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password)) {
            await client.AuthenticateAsync(username, password);
        }
        
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    public async Task SendEmailVerificationAsync(string toEmail, string verificationCode)
    {
        var subject = "Email Verification Code - UniShare";
        var body = $@"Hello,

Your email verification code is: {verificationCode}

This code will expire in 5 minutes.

If you didn't request this code, please ignore this email.

Best regards,
UniShare Team";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken, Guid userId)
    {
        // Get frontend URL from configuration or use default
        var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";
        
        // URL encode the token to ensure it's safe for URLs
        var encodedToken = Uri.EscapeDataString(resetToken);
        
        // Build the reset password URL with token and userId as query parameters
        var resetUrl = $"{frontendUrl}/reset-password?token={encodedToken}&userId={userId}";
        
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_configuration["Smtp:From"] ?? "noreply@unishare.com"));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Password Reset - UniShare";

        var builder = new BodyBuilder
        {
            HtmlBody = $@"
                <!DOCTYPE html>
                <html lang=""en"">
                <head>
                    <meta charset=""UTF-8"">
                    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                    <title>Password Reset</title>
                </head>
                <body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;"">
                    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
                        <tr>
                            <td style=""padding: 40px 0; text-align: center;"">
                                <table role=""presentation"" style=""width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);"">
                                    <tr>
                                        <td style=""padding: 40px 30px; text-align: center;"">
                                            <h1 style=""color: #333333; margin: 0 0 20px 0; font-size: 28px;"">Password Reset Request</h1>
                                            <p style=""color: #666666; font-size: 16px; line-height: 1.5; margin: 0 0 30px 0;"">
                                                We received a request to reset your password for your UniShare account. Click the button below to create a new password.
                                            </p>
                                            <p style=""margin: 30px 0;"">
                                                <a href=""{resetUrl}"" style=""display: inline-block; padding: 14px 40px; background-color: #007bff; color: #ffffff; text-decoration: none; border-radius: 5px; font-size: 16px; font-weight: bold;"">
                                                    Reset Password
                                                </a>
                                            </p>
                                            <p style=""color: #999999; font-size: 14px; line-height: 1.5; margin: 30px 0 0 0;"">
                                                This link will expire in 15 minutes for security reasons.
                                            </p>
                                            <p style=""color: #999999; font-size: 14px; line-height: 1.5; margin: 20px 0 0 0;"">
                                                If you didn't request a password reset, please ignore this email or contact support if you have concerns about your account security.
                                            </p>
                                            <hr style=""border: none; border-top: 1px solid #e9ecef; margin: 30px 0;"">
                                            <p style=""color: #999999; font-size: 12px; line-height: 1.5; margin: 10px 0 0 0;"">
                                                If the button doesn't work, copy and paste this link into your browser:<br>
                                                <span style=""color: #007bff; word-break: break-all;"">{resetUrl}</span>
                                            </p>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style=""padding: 20px 30px; background-color: #f8f9fa; border-top: 1px solid #e9ecef; text-align: center; border-radius: 0 0 8px 8px;"">
                                            <p style=""color: #999999; font-size: 12px; margin: 0;"">
                                                © 2025 UniShare. All rights reserved.
                                            </p>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>
            "
        };
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        
        var host = _configuration["Smtp:Host"];
        var port = int.Parse(_configuration["Smtp:Port"] ?? "587");
        var useSsl = _configuration.GetValue<bool>("Smtp:UseSsl", true);
        
        await client.ConnectAsync(host, port, useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
        
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        
        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password)) {
            await client.AuthenticateAsync(username, password);
        }
        
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
