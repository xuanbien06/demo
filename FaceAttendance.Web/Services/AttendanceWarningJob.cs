// Đường dẫn: FaceAttendance.Web/Services/AttendanceWarningJob.cs
using FaceAttendance.Web.Data;

namespace FaceAttendance.Web.Services
{
    public class AttendanceWarningJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AttendanceWarningJob> _logger;

        public AttendanceWarningJob(IServiceProvider serviceProvider, ILogger<AttendanceWarningJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Bot gửi Email cảnh báo đã khởi động!");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"[Bot] Đang quét CSDL lúc: {DateTime.Now}");

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

                        var studentToWarn = dbContext.Students.FirstOrDefault(s => s.IsActive == true);

                        if (studentToWarn != null)
                        {
                            // Chủ đề không dùng từ nhạy cảm
                            string subject = "Thông báo tình hình chuyên cần học kỳ hiện tại";

                            // Giao diện tinh gọn, không dùng css phức tạp, dùng màu xanh dương (Tin cậy)
                            string htmlBody = $@"
                            <html>
                            <head>
                                <meta charset='UTF-8'>
                            </head>
                            <body style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;'>
                                <div style='max-width: 600px; background-color: #ffffff; padding: 20px; border-radius: 8px; border-top: 4px solid #0056b3; margin: 0 auto;'>
                                    <h2 style='color: #0056b3;'>Thông Báo Chuyên Cần</h2>
                                    <p>Chào em <strong>{studentToWarn.FullName}</strong> (Mã SV: {studentToWarn.StudentID}),</p>
                                    <p>Hệ thống ghi nhận em có số buổi vắng học ở mức cần lưu ý trong học kỳ này.</p>
                                    <p>Vui lòng kiểm tra lại quá trình tham gia lớp học và liên hệ với Giảng viên để được hỗ trợ, đảm bảo đủ điều kiện dự thi.</p>
                                    <br/>
                                    <p>Trân trọng,</p>
                                    <p><strong>Hệ thống Điểm danh AI - ĐH Công nghiệp Quảng Ninh</strong></p>
                                </div>
                            </body>
                            </html>";

                            await emailService.SendEmailAsync(studentToWarn.Email, subject, htmlBody);
                            _logger.LogInformation($"[Bot] Đã gửi thư thành công cho: {studentToWarn.Email}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[Bot Lỗi] {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}