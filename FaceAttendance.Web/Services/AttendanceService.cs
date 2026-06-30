using FaceAttendance.Web.Repositories;

namespace FaceAttendance.Web.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly IFaceEmbeddingRepository _faceRepository;
        private readonly FaceRecognitionService _faceService;

        public AttendanceService(IFaceEmbeddingRepository faceRepository, FaceRecognitionService faceService)
        {
            _faceRepository = faceRepository;
            _faceService = faceService;
        }

        public async Task<(bool Success, string Message, string StudentName, double Percent)> ProcessAttendanceAsync(string base64Image)
        {
            try
            {
                string base64Data = base64Image.Substring(base64Image.IndexOf(",") + 1);
                byte[] imageBytes = Convert.FromBase64String(base64Data);

                using var stream = new MemoryStream(imageBytes);
                var formFile = new FormFile(stream, 0, imageBytes.Length, "file", "webcam_frame.jpg");

                List<float> cameraVector = await _faceService.GetFaceEmbeddingAsync(formFile);
                if (cameraVector == null)
                {
                    return (false, "Không thấy khuôn mặt", string.Empty, 0);
                }

                var savedEmbeddings = await _faceRepository.GetAllWithStudentAsync();

                if (savedEmbeddings.Count == 0)
                {
                    return (false, "DB chưa có dữ liệu", string.Empty, 0);
                }

                string matchedStudentName = "Người lạ";
                double maxSimilarity = -1;
                double threshold = 0.55;

                foreach (var dbFace in savedEmbeddings)
                {
                    var dbVector = System.Text.Json.JsonSerializer.Deserialize<List<float>>(dbFace.VectorData);
                    double similarity = CalculateCosineSimilarity(cameraVector, dbVector);

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
                    return (true, "Thành công", matchedStudentName, Math.Round(maxSimilarity * 100, 2));
                }
                else
                {
                    Console.WriteLine($"[AI] => THẤT BẠI: Người lạ (Chỉ giống {maxSimilarity * 100:0.00}%)");
                    return (false, $"Giống {Math.Round(maxSimilarity * 100, 2)}%", string.Empty, 0);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LỖI C#] {ex.Message}");
                return (false, "Lỗi C#: " + ex.Message, string.Empty, 0);
            }
        }

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
}