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
        private readonly string _frontendUrl;

        public EmailService(IConfiguration configuration)
        {
            _host        = configuration["Gmail:Host"]      ?? "smtp.gmail.com";
            _port        = int.Parse(configuration["Gmail:Port"] ?? "587");
            _username    = configuration["Gmail:Username"]!;
            _password    = configuration["Gmail:AppPassword"]!;
            _fromEmail   = configuration["Gmail:FromEmail"]!;
            _fromName    = configuration["Gmail:FromName"]  ?? "DealTrack";
            _frontendUrl = (configuration["FrontendUrl"] ?? "http://localhost").TrimEnd('/');
        }

        public async Task SendInviteEmailAsync(string toEmail, string companyName, string inviteCode)
        {
            var subject = $"You're invited to join {companyName} on DealTrack";
            var registerLink = $"{_frontendUrl}/accept-invite?code={inviteCode}";

            var html = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08);'>
                    <!-- Header -->
                    <div style='background: linear-gradient(135deg, #4f46e5, #7c3aed); padding: 36px 32px; text-align: center;'>
                        <h1 style='color: #ffffff; margin: 0; font-size: 28px; font-weight: 800; letter-spacing: -0.5px;'>FollowUp CRM</h1>
                        <p style='color: #c4b5fd; margin: 6px 0 0; font-size: 14px;'>You have been invited!</p>
                    </div>

                    <!-- Body -->
                    <div style='padding: 36px 32px;'>
                        <h2 style='color: #1e1b4b; font-size: 22px; margin: 0 0 12px;'>Welcome to <strong>{companyName}</strong> 🎉</h2>
                        <p style='color: #4b5563; font-size: 15px; line-height: 1.6; margin: 0 0 28px;'>
                            You have been invited to join <strong>{companyName}</strong> on DealTrack.<br/>
                            Click the button below to set up your account — it only takes a minute.
                        </p>

                        <!-- CTA Button -->
                        <div style='text-align: center; margin: 0 0 32px;'>
                            <a href='{registerLink}'
                               style='display: inline-block; background: linear-gradient(135deg, #4f46e5, #7c3aed);
                                      color: #ffffff; text-decoration: none; padding: 16px 40px;
                                      border-radius: 10px; font-size: 16px; font-weight: 700;
                                      letter-spacing: 0.3px; box-shadow: 0 4px 14px rgba(79,70,229,0.4);'>
                                ✅ Accept Invitation &amp; Register
                            </a>
                        </div>

                        <!-- Divider -->
                        <div style='border-top: 1px solid #e5e7eb; margin: 0 0 24px;'></div>

                        <!-- Code fallback -->
                        <p style='color: #6b7280; font-size: 13px; margin: 0 0 10px;'>
                            If the button doesn't work, open this link in your browser:
                        </p>
                        <div style='background: #f3f4f6; border-radius: 8px; padding: 12px 16px; word-break: break-all;'>
                            <a href='{registerLink}' style='color: #4f46e5; font-size: 13px; text-decoration: none;'>{registerLink}</a>
                        </div>

                        <p style='color: #9ca3af; font-size: 12px; margin: 20px 0 0; text-align: center;'>
                            This invitation expires in <strong>7 days</strong>. Do not share it with anyone.
                        </p>
                    </div>
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

        public async Task SendWithdrawalVerificationAsync(string toEmail, string code)
        {
            var subject = "Your DealTrack withdrawal verification code";

            var html = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #d97706;'>Join Request Withdrawal</h2>
                    <p>You requested to withdraw your pending join request on DealTrack.</p>
                    <p>Use the verification code below to confirm:</p>
                    <div style='background: #fffbeb; border: 2px solid #f59e0b; padding: 24px;
                                text-align: center; border-radius: 12px; margin: 24px 0;'>
                        <h1 style='color: #92400e; letter-spacing: 10px; font-size: 40px;
                                   margin: 0; font-family: monospace;'>{code}</h1>
                    </div>
                    <p style='color: #6b7280; font-size: 14px;'>
                        This code expires in <strong>15 minutes</strong>.<br/>
                        If you did not request this, please ignore this email — your request remains active.
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
