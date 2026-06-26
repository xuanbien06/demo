using FaceAttendance.Web.Data;
using FaceAttendance.Web.Models;
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
        // Frontend sẽ truyền thêm classId và sessionId xuống
        public async Task<IActionResult> Recognize([FromBody] ImageRequest request, [FromQuery] int classId, [FromQuery] int sessionId)
        {
            try
            {
                // 1. Kiểm tra an toàn dữ liệu Base64
                if (string.IsNullOrEmpty(request.Base64Image) || !request.Base64Image.Contains(","))
                    return Json(new { success = false, message = "Dữ liệu ảnh không hợp lệ" });

                string base64Data = request.Base64Image.Substring(request.Base64Image.IndexOf(",") + 1);
                byte[] imageBytes = Convert.FromBase64String(base64Data);

                using var stream = new MemoryStream(imageBytes);
                var formFile = new FormFile(stream, 0, imageBytes.Length, "file", "webcam.jpg");

                // 2. Lấy danh sách TOÀN BỘ khuôn mặt trong ảnh từ Python AI
                List<List<float>> cameraVectors = await _faceService.GetMultipleFaceEmbeddingsAsync(formFile);

                if (cameraVectors.Count == 0)
                    return Json(new { success = false, message = "Không thấy khuôn mặt" });

                // 3. THUẬT TOÁN TỐI ƯU CÓ CHỐNG LỖI (FAIL-SAFE)
                List<FaceEmbedding> dbEmbeddings;

                if (classId > 0)
                {
                    // Chế độ thực tế: Chỉ lấy sinh viên thuộc ClassId
                    var studentsInClass = await _context.StudentClasses
                        .Where(sc => sc.ClassId == classId)
                        .Select(sc => sc.StudentID)
                        .ToListAsync();

                    dbEmbeddings = await _context.FaceEmbeddings
                        .Include(f => f.Student)
                        .Where(f => studentsInClass.Contains(f.StudentID))
                        .ToListAsync();
                }
                else
                {
                    // CHẾ ĐỘ TEST: Quét toàn bộ khuôn mặt có trong Database 
                    // (Khắc phục lỗi "không nhận được ai" do bạn chưa thêm dữ liệu vào bảng Lớp)
                    dbEmbeddings = await _context.FaceEmbeddings
                        .Include(f => f.Student)
                        .ToListAsync();
                }

                if (dbEmbeddings.Count == 0)
                    return Json(new { success = false, message = "Chưa có dữ liệu khuôn mặt trong hệ thống!" });

                // 4. Lặp qua từng khuôn mặt bắt được trên Camera
                foreach (var camVector in cameraVectors)
                {
                    string bestMatchId = null;
                    double maxSimilarity = -1;

                    // 5. So sánh khuôn mặt đó với danh sách sinh viên trong lớp
                    foreach (var dbFace in dbEmbeddings)
                    {
                        var dbVector = System.Text.Json.JsonSerializer.Deserialize<List<float>>(dbFace.VectorData);
                        double similarity = CalculateCosineSimilarity(camVector, dbVector);

                        if (similarity > maxSimilarity)
                        {
                            maxSimilarity = similarity;
                            if (maxSimilarity >= threshold)
                            {
                                bestMatchId = dbFace.StudentID;
                            }
                        }
                    }

                    // 6. Ghi nhận điểm danh nếu vượt qua ngưỡng (Threshold)
                    if (bestMatchId != null)
                    {
                        var student = dbEmbeddings.First(f => f.StudentID == bestMatchId).Student;
                        recognizedNames.Add(student.FullName);

                        // Kiểm tra xem sinh viên này đã điểm danh trong phiên này chưa để tránh spam DB
                        bool alreadyCheckedIn = await _context.AttendanceRecords
                            .AnyAsync(r => r.SessionId == sessionId && r.StudentID == bestMatchId);

                        if (!alreadyCheckedIn)
                        {
                            var record = new AttendanceRecord
                            {
                                SessionId = sessionId,
                                StudentID = bestMatchId,
                                CheckInTime = DateTime.Now,
                                ConfidenceScore = Math.Round(maxSimilarity * 100, 2)
                            };
                            _context.AttendanceRecords.Add(record);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                if (recognizedNames.Count > 0)
                {
                    // Nối tên những người nhận diện được (VD: "Lại Xuân Biển, Nguyễn Thái")
                    return Json(new { success = true, studentName = string.Join(", ", recognizedNames) });
                }
                else
                {
                    return Json(new { success = false, message = "Người lạ hoặc không đủ độ tin cậy" });
                }
            }
            catch (Exception ex)
            {
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