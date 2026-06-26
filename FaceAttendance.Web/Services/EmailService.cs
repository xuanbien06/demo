// Đường dẫn: FaceAttendance.Web/Services/EmailService.cs
using System.Net;
using System.Net.Mail;

namespace FaceAttendance.Web.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        // Tiêm IConfiguration để lấy các thông số từ appsettings.json
        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            // 1. Đọc thông tin từ cấu hình
            var smtpServer = _config["EmailConfig:SmtpServer"];
            var port = int.Parse(_config["EmailConfig:Port"]);
            var senderEmail = _config["EmailConfig:SenderEmail"];
            var appPassword = _config["EmailConfig:AppPassword"];
            var senderName = _config["EmailConfig:SenderName"];

            // 2. Cấu hình bức thư
            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true // Cho phép dùng thẻ HTML (in đậm, tô màu) trong thư
            };
            mailMessage.To.Add(toEmail); // Thêm người nhận

            // 3. Cấu hình "Người đưa thư" (SmtpClient)
            using var smtpClient = new SmtpClient(smtpServer, port)
            {
                Credentials = new NetworkCredential(senderEmail, appPassword),
                EnableSsl = true // Bắt buộc phải bật SSL/TLS để bảo mật đường truyền
            };

            // 4. Bấm nút gửi
            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}