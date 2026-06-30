using FaceAttendance.Web.Data;
using FaceAttendance.Web.Repositories;
using FaceAttendance.Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký MVC và API Controllers
builder.Services.AddControllersWithViews(); // Cho MVC (Giao diện web)
builder.Services.AddControllers(); // Cho API (Để Python gọi sang)

// 2. Đăng ký kết nối Database (Lấy chuỗi kết nối từ appsettings.json)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Đăng ký Dependency Injection (Rất quan trọng - Hội đồng thường hỏi)
// "AddScoped" nghĩa là: Mỗi khi có request gửi lên web, nó sẽ tạo mới 1 đối tượng, xong request thì hủy.
builder.Services.AddScoped<FaceAttendance.Web.Repositories.IStudentRepository, FaceAttendance.Web.Repositories.StudentRepository>();
builder.Services.AddScoped<FaceAttendance.Web.Services.IStudentService, FaceAttendance.Web.Services.StudentService>();
builder.Services.AddScoped<FaceAttendance.Web.Repositories.IFaceEmbeddingRepository, FaceAttendance.Web.Repositories.FaceEmbeddingRepository>();
builder.Services.AddScoped<FaceAttendance.Web.Services.IAttendanceService, FaceAttendance.Web.Services.AttendanceService>();

// 4. Cấu hình bảo mật JWT
var jwtKey = builder.Configuration["Jwt:Key"];
var keyBytes = Encoding.UTF8.GetBytes(jwtKey!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, // Có kiểm tra người phát hành
            ValidateAudience = false, // Tắt kiểm tra người nhận (cho đơn giản)
            ValidateLifetime = true, // Token có hạn sử dụng không
            ValidateIssuerSigningKey = true, // Có kiểm tra chữ ký không
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
        };
    });

// Đăng ký HttpClient và đọc địa chỉ AI từ file cấu hình appsettings.json
builder.Services.AddHttpClient<FaceRecognitionService>(client =>
{
    var aiUrl = builder.Configuration["AiApiSettings:BaseUrl"];
    client.BaseAddress = new Uri(aiUrl!);
});

// Đăng ký Service gửi mail thông thường
builder.Services.AddScoped<EmailService>();

// Đăng ký con Bot chạy ngầm (AddHostedService)
builder.Services.AddHostedService<AttendanceWarningJob>();

var app = builder.Build();

// Cấu hình Middleware Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles(); // Đọc file CSS, JS, Ảnh
app.UseRouting();

// Thứ tự 2 hàm này BẮT BUỘC phải như sau:
app.UseAuthentication(); // 1. Mày là ai? (Xác thực JWT)
app.UseAuthorization();  // 2. Mày được quyền làm gì? (Phân quyền)

// Điều hướng mặc định của trang web
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();