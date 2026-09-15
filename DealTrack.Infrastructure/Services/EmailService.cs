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
        private readonly string _frontendUrl;

        public EmailService(IConfiguration configuration)
        {
            _apiKey      = configuration["SendGrid:ApiKey"]!;
            _fromEmail   = configuration["SendGrid:FromEmail"]
                        ?? configuration["Gmail:FromEmail"]!;
            _fromName    = configuration["SendGrid:FromName"]
                        ?? configuration["Gmail:FromName"]
                        ?? "FollowUp CRM";
            _frontendUrl = (configuration["FrontendUrl"] ?? "http://localhost").TrimEnd('/');
        }

        public async Task SendInviteEmailAsync(string toEmail, string companyName, string inviteCode)
        {
            var registerLink = $"{_frontendUrl}/accept-invite?code={inviteCode}";
            var subject = $"You're invited to join {companyName} on FollowUp CRM";

            var html = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08);'>
                    <div style='background: linear-gradient(135deg, #4f46e5, #7c3aed); padding: 36px 32px; text-align: center;'>
                        <h1 style='color: #ffffff; margin: 0; font-size: 28px; font-weight: 800;'>FollowUp CRM</h1>
                        <p style='color: #c4b5fd; margin: 6px 0 0; font-size: 14px;'>You have been invited!</p>
                    </div>
                    <div style='padding: 36px 32px;'>
                        <h2 style='color: #1e1b4b; font-size: 22px; margin: 0 0 12px;'>Welcome to <strong>{companyName}</strong> 🎉</h2>
                        <p style='color: #4b5563; font-size: 15px; line-height: 1.6; margin: 0 0 28px;'>
                            You have been invited to join <strong>{companyName}</strong> on FollowUp CRM.<br/>
                            Click the button below to set up your account.
                        </p>
                        <div style='text-align: center; margin: 0 0 32px;'>
                            <a href='{registerLink}'
                               style='display: inline-block; background: linear-gradient(135deg, #4f46e5, #7c3aed);
                                      color: #ffffff; text-decoration: none; padding: 16px 40px;
                                      border-radius: 10px; font-size: 16px; font-weight: 700;
                                      box-shadow: 0 4px 14px rgba(79,70,229,0.4);'>
                                ✅ Accept Invitation &amp; Register
                            </a>
                        </div>
                        <div style='border-top: 1px solid #e5e7eb; margin: 0 0 24px;'></div>
                        <p style='color: #6b7280; font-size: 13px; margin: 0 0 10px;'>Or copy this link:</p>
                        <div style='background: #f3f4f6; border-radius: 8px; padding: 12px 16px; word-break: break-all;'>
                            <a href='{registerLink}' style='color: #4f46e5; font-size: 13px;'>{registerLink}</a>
                        </div>
                        <p style='color: #9ca3af; font-size: 12px; margin: 20px 0 0; text-align: center;'>
                            This invitation expires in <strong>7 days</strong>.
                        </p>
                    </div>
                </div>";

            await SendAsync(toEmail, subject, html);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            var subject = "Reset your FollowUp CRM password";

            var html = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08);'>
                    <div style='background: linear-gradient(135deg, #2563eb, #1d4ed8); padding: 36px 32px; text-align: center;'>
                        <h1 style='color: #ffffff; margin: 0; font-size: 28px; font-weight: 800;'>FollowUp CRM</h1>
                        <p style='color: #bfdbfe; margin: 6px 0 0; font-size: 14px;'>Password Reset Request</p>
                    </div>
                    <div style='padding: 36px 32px;'>
                        <h2 style='color: #1e3a8a; font-size: 20px; margin: 0 0 12px;'>Reset Your Password 🔐</h2>
                        <p style='color: #4b5563; font-size: 15px; line-height: 1.6; margin: 0 0 28px;'>
                            We received a request to reset your password. Click the button below to choose a new one.
                        </p>
                        <div style='text-align: center; margin: 0 0 32px;'>
                            <a href='{resetLink}'
                               style='display: inline-block; background: linear-gradient(135deg, #2563eb, #1d4ed8);
                                      color: #ffffff; text-decoration: none; padding: 16px 40px;
                                      border-radius: 10px; font-size: 16px; font-weight: 700;
                                      box-shadow: 0 4px 14px rgba(37,99,235,0.4);'>
                                🔑 Reset My Password
                            </a>
                        </div>
                        <div style='border-top: 1px solid #e5e7eb; margin: 0 0 24px;'></div>
                        <p style='color: #6b7280; font-size: 13px; margin: 0 0 10px;'>Or copy this link:</p>
                        <div style='background: #f3f4f6; border-radius: 8px; padding: 12px 16px; word-break: break-all;'>
                            <a href='{resetLink}' style='color: #2563eb; font-size: 13px;'>{resetLink}</a>
                        </div>
                        <p style='color: #9ca3af; font-size: 12px; margin: 20px 0 0; text-align: center;'>
                            This link expires in <strong>1 hour</strong>. If you didn't request this, ignore this email.
                        </p>
                    </div>
                </div>";

            await SendAsync(toEmail, subject, html);
        }

        public async Task SendWithdrawalVerificationAsync(string toEmail, string code)
        {
            var subject = "Your FollowUp CRM verification code";

            var html = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #d97706;'>Join Request Withdrawal</h2>
                    <p>Use the verification code below to confirm your withdrawal:</p>
                    <div style='background: #fffbeb; border: 2px solid #f59e0b; padding: 24px;
                                text-align: center; border-radius: 12px; margin: 24px 0;'>
                        <h1 style='color: #92400e; letter-spacing: 10px; font-size: 40px;
                                   margin: 0; font-family: monospace;'>{code}</h1>
                    </div>
                    <p style='color: #6b7280; font-size: 14px;'>
                        This code expires in <strong>15 minutes</strong>.
                    </p>
                </div>";

            await SendAsync(toEmail, subject, html);
        }

        private async Task SendAsync(string toEmail, string subject, string html)
        {
            var client = new SendGridClient(_apiKey);
            var from   = new EmailAddress(_fromEmail, _fromName);
            var to     = new EmailAddress(toEmail);
            var msg    = MailHelper.CreateSingleEmail(from, to, subject, null, html);
            var res    = await client.SendEmailAsync(msg);
            if ((int)res.StatusCode >= 400)
                throw new Exception($"SendGrid error {res.StatusCode}");
        }
    }
}
