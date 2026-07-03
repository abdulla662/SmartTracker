using DealTrack.Application.ServicesInterfaces;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace DealTrack.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _apiKey;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(IConfiguration configuration)
        {
            _apiKey = configuration["SendGrid:ApiKey"]!;
            _fromEmail = configuration["SendGrid:FromEmail"]!;
            _fromName = configuration["SendGrid:FromName"]!;
        }

        public async Task SendInviteEmailAsync(string toEmail, string companyName, string inviteCode)
        {
            var client = new SendGridClient(_apiKey);

            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail);
            var subject = $"You're invited to join {companyName} on DealTrack";

            var plainText = $"You have been invited to join {companyName} on DealTrack.\n\nYour invite code is: {inviteCode}\n\nThis code expires in 7 days.";

            var html = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #2563eb;'>You're Invited!</h2>
                    <p>You have been invited to join <strong>{companyName}</strong> on DealTrack.</p>
                    <p>Use the code below to register your account:</p>
                    <div style='background: #f3f4f6; padding: 20px; text-align: center; border-radius: 8px; margin: 20px 0;'>
                        <h1 style='color: #1f2937; letter-spacing: 8px; font-size: 36px;'>{inviteCode}</h1>
                    </div>
                    <p style='color: #6b7280; font-size: 14px;'>This code expires in 7 days.</p>
                </div>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainText, html);
            await client.SendEmailAsync(msg);
        }
        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            var client = new SendGridClient(_apiKey);

            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail);
            var subject = "Reset your DealTrack password";

            var plainText = $"Click the link below to reset your password:\n\n{resetLink}\n\nThis link expires in 1 hour.";

            var html = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
            <h2 style='color: #2563eb;'>Reset Your Password</h2>
            <p>We received a request to reset your DealTrack password.</p>
            <p>Click the button below to set a new password:</p>
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{resetLink}'
                   style='background: #2563eb; color: white; padding: 14px 28px;
                          border-radius: 8px; text-decoration: none; font-size: 16px;'>
                    Reset Password
                </a>
            </div>
            <p style='color: #6b7280; font-size: 14px;'>
                This link expires in 1 hour. If you didn't request this, ignore this email.
            </p>
        </div>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainText, html);
            await client.SendEmailAsync(msg);
        }
    }
}
