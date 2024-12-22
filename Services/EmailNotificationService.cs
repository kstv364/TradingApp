using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace TradingApp.Services
{
    public class EmailNotificationService
    {
        private readonly IConfiguration _configuration;
        public EmailNotificationService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendNotificationAsync(string subject, string body)
        {
            var smtpClient = new SmtpClient
            {
                Host = _configuration["Email:Host"],
                Port = int.Parse(_configuration["Email:Port"]),
                Credentials = new System.Net.NetworkCredential(_configuration["Email:Username"], _configuration["Email:Password"]),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_configuration["Email:From"]),
                Subject = subject,
                Body = body,
                IsBodyHtml = false,
            };

            mailMessage.To.Add(_configuration["Email:To"]);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}
