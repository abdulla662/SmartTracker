namespace DealTrack.Application.ServicesInterfaces
{
    public interface IEmailService
    {
        Task SendInviteEmailAsync(string toEmail, string companyName, string inviteCode);
    }
}
