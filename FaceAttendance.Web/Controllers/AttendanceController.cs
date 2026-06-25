using FaceAttendance.Web.Data;
using FaceAttendance.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaceAttendance.Web.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly AppDbContext _context;
        private readonly FaceRecognitionService _faceService;

        // Tiêm (Inject) Database và Service AI vào Controller
        public AttendanceController(AppDbContext context, FaceRecognitionService faceService)
        {
            _context = context;
            _faceService = faceService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Recognize([FromBody] ImageRequest request)
        {
            try
            {
                string base64Data = request.Base64Image.Substring(request.Base64Image.IndexOf(",") + 1);
                byte[] imageBytes = Convert.FromBase64String(base64Data);

                using var stream = new MemoryStream(imageBytes);
                var formFile = new FormFile(stream, 0, imageBytes.Length, "file", "webcam_frame.jpg");

                List<float> cameraVector = await _faceService.GetFaceEmbeddingAsync(formFile);
                if (cameraVector == null)
                {
                    Console.WriteLine("[AI] Không tìm thấy khuôn mặt trong khung hình.");
                    return Json(new { success = false, message = "Không thấy khuôn mặt" });
                }

                var savedEmbeddings = await _context.FaceEmbeddings.Include(f => f.Student).ToListAsync();

                if (savedEmbeddings.Count == 0)
                {
                    Console.WriteLine("[DB] CSDL trống! Chưa có ai đăng ký khuôn mặt.");
                    return Json(new { success = false, message = "DB chưa có dữ liệu" });
                }

                string matchedStudentName = "Người lạ";
                double maxSimilarity = -1;
                double threshold = 0.55; // Đã hạ ngưỡng xuống 55% để phù hợp thực tế

                foreach (var dbFace in savedEmbeddings)
                {
                    var dbVector = System.Text.Json.JsonSerializer.Deserialize<List<float>>(dbFace.VectorData);
                    double similarity = CalculateCosineSimilarity(cameraVector, dbVector);

                    // IN RA CONSOLE ĐỂ EM THẤY AI ĐANG TÍNH TOÁN
                    Console.WriteLine($"[AI] So sánh với {dbFace.Student.FullName} - Độ giống: {similarity * 100:0.00}%");

                    if (similarity > maxSimilarity)
                    {
                        maxSimilarity = similarity;
                        if (maxSimilarity >= threshold)
                        {
                            matchedStudentName = dbFace.Student.FullName;
                        }
                    }
                }

                if (maxSimilarity >= threshold)
                {
                    Console.WriteLine($"[AI] => NHẬN DIỆN THÀNH CÔNG: {matchedStudentName}");
                    return Json(new { success = true, studentName = matchedStudentName, percent = Math.Round(maxSimilarity * 100, 2) });
                }
                else
                {
                    Console.WriteLine($"[AI] => THẤT BẠI: Người lạ (Chỉ giống {maxSimilarity * 100:0.00}%)");
                    return Json(new { success = false, message = $"Giống {Math.Round(maxSimilarity * 100, 2)}%" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LỖI C#] {ex.Message}");
                return Json(new { success = false, message = "Lỗi C#: " + ex.Message });
            }
        }

        // ==========================================
        // HÀM TÍNH TOÁN TOÁN HỌC (COSINE SIMILARITY)
        // ==========================================
        private double CalculateCosineSimilarity(List<float> vectorA, List<float> vectorB)
        {
            double dotProduct = 0.0;
            double normA = 0.0;
            double normB = 0.0;

            for (int i = 0; i < vectorA.Count; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                normA += Math.Pow(vectorA[i], 2);
                normB += Math.Pow(vectorB[i], 2);
            }

            if (normA == 0 || normB == 0) return 0;
            return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
        }
    }

    public class ImageRequest
    {
        public string Base64Image { get; set; }
    }
}