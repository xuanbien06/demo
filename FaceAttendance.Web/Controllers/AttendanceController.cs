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
                // 1. Cắt bỏ tiền tố "data:image/jpeg;base64," của chuỗi ảnh Javascript gửi lên
                string base64Data = request.Base64Image.Substring(request.Base64Image.IndexOf(",") + 1);
                byte[] imageBytes = Convert.FromBase64String(base64Data);

                // Chuyển byte thành file ảnh ảo để gửi sang Python
                using var stream = new MemoryStream(imageBytes);
                var formFile = new FormFile(stream, 0, imageBytes.Length, "file", "webcam_frame.jpg");

                // 2. Gửi ảnh sang Python API và nhận về Vector 512 chiều
                List<float> cameraVector = await _faceService.GetFaceEmbeddingAsync(formFile);
                if (cameraVector == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy khuôn mặt" });
                }

                // 3. Lấy toàn bộ dữ liệu khuôn mặt đã đăng ký từ Database
                var savedEmbeddings = await _context.FaceEmbeddings.Include(f => f.Student).ToListAsync();

                string matchedStudentName = "Người lạ";
                double maxSimilarity = 0;
                double threshold = 0.80; // Ngưỡng giống nhau (80%)

                // 4. Vòng lặp so sánh Vector Camera với từng Vector trong Database
                foreach (var dbFace in savedEmbeddings)
                {
                    // Chuyển chuỗi JSON trong DB thành List<float>
                    var dbVector = System.Text.Json.JsonSerializer.Deserialize<List<float>>(dbFace.VectorData);

                    // Gọi hàm tính toán Cosine Similarity
                    double similarity = CalculateCosineSimilarity(cameraVector, dbVector);

                    // Tìm ra người có độ giống nhau cao nhất
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
                    return Json(new { success = true, studentName = matchedStudentName });
                }
                else
                {
                    return Json(new { success = false, message = "Không nhận diện được sinh viên này" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
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