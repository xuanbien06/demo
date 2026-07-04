// Đường dẫn: FaceAttendance.Web/Services/EmailService.cs
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;

namespace FaceAttendance.Web.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var smtpServer = _config["EmailConfig:SmtpServer"];
            var port = int.Parse(_config["EmailConfig:Port"]);
            var senderEmail = _config["EmailConfig:SenderEmail"];
            var appPassword = _config["EmailConfig:AppPassword"];
            var senderName = _config["EmailConfig:SenderName"];

            var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(senderEmail, senderName);
            mailMessage.To.Add(toEmail);
            mailMessage.Subject = subject;
            mailMessage.SubjectEncoding = Encoding.UTF8;

            // 1. Thêm các Header bảo mật và thông tin phản hồi
            string domain = senderEmail.Split('@').LastOrDefault() ?? "domain.com";
            mailMessage.Headers.Add("Message-Id", $"<{Guid.NewGuid()}@{domain}>");
            mailMessage.ReplyToList.Add(new MailAddress(senderEmail, senderName));

            // 2. Tạo nội dung Plain Text từ HTML để tránh lỗi HTML-Only Spam
            string plainText = Regex.Replace(htmlMessage, "<.*?>", string.Empty);
            plainText = WebUtility.HtmlDecode(plainText).Trim();

            // 3. Khởi tạo ContentType ép chuẩn UTF-8
            ContentType mimeTypeHtml = new ContentType("text/html; charset=UTF-8");
            ContentType mimeTypeText = new ContentType("text/plain; charset=UTF-8");

            // 4. Tạo AlternateViews
            AlternateView htmlView = AlternateView.CreateAlternateViewFromString(htmlMessage, mimeTypeHtml);
            AlternateView plainView = AlternateView.CreateAlternateViewFromString(plainText, mimeTypeText);

            // CỰC KỲ QUAN TRỌNG: Theo chuẩn RFC 2046, Plain text phải được add TRƯỚC HTML
            mailMessage.AlternateViews.Add(plainView);
            mailMessage.AlternateViews.Add(htmlView);

            using var smtpClient = new SmtpClient(smtpServer, port);
            smtpClient.EnableSsl = true;
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = new NetworkCredential(senderEmail, appPassword);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}