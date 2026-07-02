// Đường dẫn: FaceAttendance.Web/Services/FaceRecognitionService.cs
using System.Text.Json;
using System.Linq;             // <-- THÊM DÒNG NÀY ĐỂ SỬA LỖI ĐỎ SELECT/TOARRAY
using Microsoft.AspNetCore.Http; // <-- THÊM DÒNG NÀY ĐỂ NHẬN DIỆN IFormFile

namespace FaceAttendance.Web.Services
{
    public class FaceRecognitionService
    {
        private readonly HttpClient _httpClient;

        public FaceRecognitionService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Đổi kiểu trả về: Hỗ trợ nhiều khuôn mặt, mỗi khuôn mặt có [Tọa độ Box, Vector]
        public async Task<List<(int[] Box, List<float> Vector)>> GetFaceEmbeddingAsync(IFormFile imageFile)
        {
            using var content = new MultipartFormDataContent();
            using var stream = imageFile.OpenReadStream();
            using var streamContent = new StreamContent(stream);

            content.Add(streamContent, "file", imageFile.FileName);

            var response = await _httpClient.PostAsync("/api/extract-face", content);

            if (!response.IsSuccessStatusCode)
            {
                return new List<(int[], List<float>)>(); // Không tìm thấy mặt thì trả về list rỗng
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(jsonResponse);
            var root = document.RootElement;

            var resultList = new List<(int[] Box, List<float> Vector)>();

            if (root.GetProperty("status").GetString() == "success")
            {
                // Parse mảng "faces" từ Python API
                var facesElement = root.GetProperty("faces");
                foreach (var face in facesElement.EnumerateArray())
                {
                    // Lấy Bounding Box [x, y, w, h]
                    var boxArray = face.GetProperty("box").EnumerateArray().Select(x => x.GetInt32()).ToArray();

                    // Lấy Vector
                    var vectorArray = face.GetProperty("vector").EnumerateArray().Select(x => (float)x.GetDouble()).ToList();

                    resultList.Add((boxArray, vectorArray));
                }
            }

            return resultList;
        }
    }
}