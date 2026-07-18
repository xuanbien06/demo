using FaceAttendance.Web.Repositories;
using System.Text.Json;

namespace FaceAttendance.Web.Services
{
    public class FaceCacheService
    {
        // ĐÃ SỬA: Bộ nhớ RAM lưu trữ cả Mã SV (StudentId) và Vector khuôn mặt
        public List<(string StudentId, List<float> Vector)> CachedFaces { get; private set; } = new();

        private readonly IServiceProvider _serviceProvider;

        public FaceCacheService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Hàm này sẽ được gọi 1 lần duy nhất khi khởi động Server Web
        public async Task LoadFacesIntoMemoryAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IFaceEmbeddingRepository>();

            var allFaces = await repo.GetAllWithStudentAsync();
            var newCache = new List<(string, List<float>)>();

            foreach (var face in allFaces)
            {
                if (string.IsNullOrEmpty(face.EmbeddingVector)) continue;

                var vector = JsonSerializer.Deserialize<List<float>>(face.EmbeddingVector);
                if (vector != null)
                {
                    // Lưu thẳng StudentID vào bộ nhớ RAM
                    newCache.Add((face.StudentID, vector));
                }
            }

            CachedFaces = newCache;
            Console.WriteLine($"[CACHE RAM] Đã nạp thành công {CachedFaces.Count} khuôn mặt từ Database vào bộ nhớ siêu tốc!");
        }

        // =====================================================================================
        // ĐÂY CHÍNH LÀ HÀM BỊ THIẾU GÂY LỖI ĐỎ DÒNG 50 - THUẬT TOÁN TÌM KHUÔN MẶT GIỐNG NHẤT
        // =====================================================================================
        public (string bestMatchId, double distance) FindBestMatch(List<float> targetVector)
        {
            string bestMatchId = null;
            double minDistance = double.MaxValue;

            // Quét toàn bộ khuôn mặt trong RAM
            foreach (var face in CachedFaces)
            {
                double dist = CalculateEuclideanDistance(targetVector, face.Vector);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestMatchId = face.StudentId; // Lấy đúng ID của sinh viên
                }
            }

            return (bestMatchId, minDistance);
        }

        // Hàm lõi AI: So khớp 2 vector bằng khoảng cách Euclid
        private double CalculateEuclideanDistance(List<float> v1, List<float> v2)
        {
            if (v1.Count != v2.Count) return double.MaxValue;

            double sum = 0;
            for (int i = 0; i < v1.Count; i++)
            {
                double diff = v1[i] - v2[i];
                sum += diff * diff;
            }
            return Math.Sqrt(sum);
        }
    }
}