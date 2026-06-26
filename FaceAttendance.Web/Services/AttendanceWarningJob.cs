// Đường dẫn: FaceAttendance.Web/Services/AttendanceWarningJob.cs
using FaceAttendance.Web.Data;

namespace FaceAttendance.Web.Services
{
    // Kế thừa BackgroundService để biến Class này thành Tác vụ chạy ngầm
    public class AttendanceWarningJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AttendanceWarningJob> _logger;

        // Vì Bot chạy ngầm là duy nhất (Singleton), nó không thể gọi trực tiếp AppDbContext.
        // Ta phải dùng IServiceProvider để tự "mở kết nối" mỗi khi cần quét CSDL.
        public AttendanceWarningJob(IServiceProvider serviceProvider, ILogger<AttendanceWarningJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        // Hàm này là "trái tim" của con Bot, nó sẽ bắt đầu chạy ngay khi Web bật lên
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Bot gửi Email cảnh báo đã khởi động!");

            // Vòng lặp vô hạn: Chạy liên tục cho đến khi em tắt Web
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"[Bot] Đang quét CSDL lúc: {DateTime.Now}");

                try
                {
                    // Xin hệ thống cấp cho 1 phạm vi làm việc (Scope) để truy cập Database
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        // Lấy công cụ Database và Email ra để dùng
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

                        // Ở đồ án thực tế, chỗ này em sẽ truy vấn: "Tìm tất cả sinh viên có số buổi vắng > 20%".
                        // Hôm nay để test, ta sẽ lấy tạm 1 sinh viên Đang hoạt động đầu tiên trong DB ra để gửi thử.
                        var studentToWarn = dbContext.Students.FirstOrDefault(s => s.IsActive == true);

                        if (studentToWarn != null)
                        {
                            string subject = "⚠️ [Khẩn] Cảnh báo vắng học quá quy định";
                            string htmlBody = $@"
                                <div style='font-family: Arial; padding: 20px; border: 1px solid #dc3545; border-radius: 10px;'>
                                    <h2 style='color: #dc3545;'>Cảnh Báo Chuyên Cần</h2>
                                    <p>Chào em <strong>{studentToWarn.FullName}</strong> (Mã SV: {studentToWarn.StudentID}),</p>
                                    <p>Hệ thống AI nhận diện khuôn mặt phát hiện em đã vắng mặt nhiều buổi trong học kỳ này.</p>
                                    <p>Vui lòng liên hệ ngay với Giảng viên bộ môn để giải trình, nếu không em sẽ bị <strong>CẤM THI</strong>.</p>
                                    <hr/>
                                    <p style='font-size: 12px; color: gray;'>Đây là email tự động từ Hệ thống Điểm danh AI - ĐH Công nghiệp Quảng Ninh.</p>
                                </div>
                            ";

                            // Gọi hàm gửi thư
                            await emailService.SendEmailAsync(studentToWarn.Email, subject, htmlBody);
                            _logger.LogInformation($"[Bot] Đã gửi thư thành công cho: {studentToWarn.Email}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[Bot Lỗi] {ex.Message}");
                }

                // CHU KỲ QUÉT: Lệnh này bảo con Bot đi ngủ. 
                // Ở đây thầy để 60 giây (1 phút) quét 1 lần để em dễ test. 
                // Sau này bảo vệ đồ án em có thể đổi thành 24 tiếng (TimeSpan.FromDays(1)).
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}