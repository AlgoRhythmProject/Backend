using AlgoRhythm.Services.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace AlgoRhythm.Services;

public class SendGridEmailSender : IEmailSender
{
    private readonly string _apiKey;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public SendGridEmailSender(IConfiguration config)
    {
        _apiKey = config["SendGrid:ApiKey"] ?? throw new InvalidOperationException("SendGrid:ApiKey is missing");
        _fromEmail = config["SendGrid:FromEmail"] ?? throw new InvalidOperationException("SendGrid:FromEmail is missing");
        _fromName = config["SendGrid:FromName"] ?? "AlgoRhythm";
    }

    public async Task SendEmailAsync(string toEmail, string subject, string plainTextContent, string htmlContent)
    {
        var client = new SendGridClient(_apiKey);
        var from = new EmailAddress(_fromEmail, _fromName);
        var to = new EmailAddress(toEmail);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

        var response = await client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Body.ReadAsStringAsync();
            throw new InvalidOperationException($"SendGrid error: {response.StatusCode} - {body}");
        }
    }
}