using DealTrack.Application.ServicesInterfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace DealTrack.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(IConfiguration configuration)
        {
            _host      = configuration["Gmail:Host"]      ?? "smtp.gmail.com";
            _port      = int.Parse(configuration["Gmail:Port"] ?? "587");
            _username  = configuration["Gmail:Username"]!;
            _password  = configuration["Gmail:AppPassword"]!;
            _fromEmail = configuration["Gmail:FromEmail"]!;
            _fromName  = configuration["Gmail:FromName"]  ?? "DealTrack";
        }

        public async Task SendInviteEmailAsync(string toEmail, string companyName, string inviteCode)
        {
            var subject = $"You're invited to join {companyName} on DealTrack";

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

            await SendAsync(toEmail, subject, html);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            var subject = "Reset your DealTrack password";

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

            await SendAsync(toEmail, subject, html);
        }

        private async Task SendAsync(string toEmail, string subject, string html)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_fromName, _fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = html };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_host, _port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_username, _password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
    }
}
