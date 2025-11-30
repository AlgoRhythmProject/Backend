namespace AlgoRhythm.Services.Users.Interfaces;

public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string plainTextContent, string htmlContent);
}