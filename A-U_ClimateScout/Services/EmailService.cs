using A_U_ClimateScout.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace A_U_ClimateScout.Services
{
    // Sends plain-text emails (plan §8): invites, password resets, contact notifications.
    public interface IEmailService
    {
        bool IsConfigured { get; }

        Task SendAsync(string to, string subject, string text, CancellationToken cancellationToken = default);
    }

    // SMTP through MailKit (see EmailOptions). A failed send is logged and doesn't break the page that triggered it:
    // the contact message is already saved, and an invite's password is also shown on screen.
    public class SmtpEmailService(IOptions<EmailOptions> options, ILogger<SmtpEmailService> logger) : IEmailService
    {
        private readonly EmailOptions settings = options.Value;

        public bool IsConfigured => settings.Host.Length > 0 && settings.FromAddress.Length > 0;

        public async Task SendAsync(string to, string subject, string text, CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
            {
                logger.LogInformation("Email isn't configured; not sending \"{Subject}\" to {To}.", subject, to);
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = text };

            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(settings.Host, settings.Port,
                    settings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);
                if (settings.UserName.Length > 0)
                {
                    await client.AuthenticateAsync(settings.UserName, settings.Password, cancellationToken);
                }
                await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);
                logger.LogInformation("Sent \"{Subject}\" to {To}.", subject, to);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Couldn't send \"{Subject}\" to {To}.", subject, to);
            }
        }
    }

    // Lets ASP.NET Core Identity's own pages (forgot password, confirm email) send through the same service.
    public class IdentityEmailSender(IEmailService email) : Microsoft.AspNetCore.Identity.UI.Services.IEmailSender
    {
        public Task SendEmailAsync(string address, string subject, string htmlMessage) =>
            email.SendAsync(address, subject, HtmlToText(htmlMessage));

        // Identity's messages are short HTML with one link: keep the link's address in the text.
        private static string HtmlToText(string html) =>
            System.Net.WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(
                System.Text.RegularExpressions.Regex.Replace(html, "<a [^>]*href=['\"]([^'\"]+)['\"][^>]*>(.*?)</a>", "$2 ($1)"),
                "<[^>]+>", ""));
    }
}
