// Đường dẫn: FaceAttendance.Web/Services/AttendanceService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // <-- ĐẢM BẢO CÓ DÒNG NÀY ĐỂ KHÔNG BỊ LỖI FormFile
using FaceAttendance.Web.Repositories;

namespace FaceAttendance.Web.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly FaceCacheService _cacheService;
        private readonly FaceRecognitionService _faceService;

        public AttendanceService(FaceCacheService cacheService, FaceRecognitionService faceService)
        {
            _cacheService = cacheService;
            _faceService = faceService;
        }

        public async Task<List<FaceResultResponse>> ProcessAttendanceAsync(string base64Image)
        {
            var finalResults = new List<FaceResultResponse>();

            try
            {
                if (string.IsNullOrEmpty(base64Image) || !base64Image.Contains(","))
                {
                    return finalResults;
                }

                string base64Data = base64Image.Substring(base64Image.IndexOf(",") + 1);
                byte[] imageBytes = Convert.FromBase64String(base64Data);

                using var stream = new MemoryStream(imageBytes);
                var formFile = new FormFile(stream, 0, imageBytes.Length, "file", "webcam_frame.jpg");

                // 1. Lấy danh sách khuôn mặt từ AI Python (Tọa độ + Vector)
                var cameraFaces = await _faceService.GetFaceEmbeddingAsync(formFile);

                if (cameraFaces == null || cameraFaces.Count == 0)
                {
                    return finalResults;
                }

                // 2. Lấy dữ liệu khuôn mặt siêu tốc từ RAM Cache
                var savedEmbeddings = _cacheService.CachedFaces;

                // 3. Duyệt qua từng khuôn mặt xuất hiện trong camera
                foreach (var camFace in cameraFaces)
                {
                    string matchedStudentName = "Unknown";
                    double maxSimilarity = -1;
                    double threshold = 0.55;

                    // So sánh với từng khuôn mặt trong RAM Cache
                    foreach (var dbFace in savedEmbeddings)
                    {
                        double similarity = CalculateCosineSimilarity(camFace.Vector, dbFace.Vector);

                        if (similarity > maxSimilarity)
                        {
                            maxSimilarity = similarity;
                            if (maxSimilarity >= threshold)
                            {
                                matchedStudentName = dbFace.StudentName;
                            }
                        }
                    }

                    // 4. Đóng gói kết quả
                    bool isSuccess = maxSimilarity >= threshold;
                    finalResults.Add(new FaceResultResponse
                    {
                        Box = camFace.Box,
                        StudentName = matchedStudentName,
                        Percent = isSuccess ? Math.Round(maxSimilarity * 100, 2) : 0,
                        Success = isSuccess
                    });
                }

                return finalResults;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LỖI C#] {ex.Message}");
                return finalResults;
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