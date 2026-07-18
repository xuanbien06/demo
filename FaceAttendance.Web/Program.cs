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

builder.Services.AddScoped<FaceAttendance.Web.Services.IAuthService, FaceAttendance.Web.Services.AuthService>();
// Đăng ký FaceCacheService như là một Singleton (Tồn tại duy nhất 1 bản sao trên RAM suốt vòng đời app)
builder.Services.AddSingleton<FaceAttendance.Web.Services.FaceCacheService>();

// 4. Cấu hình bảo mật JWT
var jwtKey = builder.Configuration["Jwt:Key"];
var keyBytes = Encoding.UTF8.GetBytes(jwtKey!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
        };

        options.Events = new JwtBearerEvents
        {
            // Lấy Token từ Cookie
            OnMessageReceived = context =>
            {
                var token = context.Request.Cookies["jwt_token"];
                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            },
            // BẢN VÁ QUAN TRỌNG: Xử lý khi chưa đăng nhập (401 Unauthorized)
            OnChallenge = context =>
            {
                // Bỏ qua phản hồi 401 mặc định của API
                context.HandleResponse();
                // Chuyển hướng người dùng về trang Login
                context.Response.Redirect("/Auth/Login");
                return Task.CompletedTask;
            }
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
//builder.Services.AddHostedService<AttendanceWarningJob>();

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

// [Nạp dữ liệu AI siêu tốc vào RAM khi bật Server]
using (var scope = app.Services.CreateScope())
{
    var cacheService = scope.ServiceProvider.GetRequiredService<FaceAttendance.Web.Services.FaceCacheService>();
    await cacheService.LoadFacesIntoMemoryAsync();
}

app.Run();