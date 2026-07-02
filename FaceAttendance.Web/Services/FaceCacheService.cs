// Đường dẫn: FaceAttendance.Web/Services/FaceCacheService.cs
using FaceAttendance.Web.Repositories;
using System.Text.Json;

namespace FaceAttendance.Web.Services
{
    public class FaceCacheService
    {
        // Bộ nhớ RAM lưu trữ: Tên Học Sinh -> Vector khuôn mặt
        public List<(string StudentName, List<float> Vector)> CachedFaces { get; private set; } = new();

        private readonly IServiceProvider _serviceProvider;

        public FaceCacheService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Hàm này sẽ được gọi 1 lần duy nhất khi khởi động Server Web
        public async Task LoadFacesIntoMemoryAsync()
        {
            // Cần dùng Scope vì IFaceEmbeddingRepository là Scoped service
            using var scope = _serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IFaceEmbeddingRepository>();

            var allFaces = await repo.GetAllWithStudentAsync();
            var newCache = new List<(string, List<float>)>();

            foreach (var face in allFaces)
            {
                if (string.IsNullOrEmpty(face.VectorData)) continue;

                var vector = JsonSerializer.Deserialize<List<float>>(face.VectorData);
                if (vector != null)
                {
                    newCache.Add((face.Student.FullName, vector));
                }
            }

            CachedFaces = newCache;
            Console.WriteLine($"[CACHE RAM] Đã nạp thành công {CachedFaces.Count} khuôn mặt từ Database vào bộ nhớ siêu tốc!");
        }
    }
}