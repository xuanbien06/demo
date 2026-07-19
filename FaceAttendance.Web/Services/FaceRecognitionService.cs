using System.Text.Json;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;

namespace FaceAttendance.Web.Services
{
    public class FaceRecognitionService
    {
        private readonly HttpClient _httpClient;

        public FaceRecognitionService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // ĐỔI KIẾN TRÚC: Trả về string Name thay vì List<float> Vector
        public async Task<List<(int[] Box, string Name)>> GetFaceEmbeddingAsync(IFormFile imageFile)
        {
            using var content = new MultipartFormDataContent();
            using var stream = imageFile.OpenReadStream();
            using var streamContent = new StreamContent(stream);

            content.Add(streamContent, "file", imageFile.FileName);

            var response = await _httpClient.PostAsync("/api/extract-face", content);

            if (!response.IsSuccessStatusCode)
            {
                return new List<(int[], string)>(); // Trả về list rỗng nếu lỗi
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(jsonResponse);
            var root = document.RootElement;

            var resultList = new List<(int[] Box, string Name)>();

            if (root.GetProperty("status").GetString() == "success")
            {
                // Parse mảng "faces" từ Python API
                var facesElement = root.GetProperty("faces");
                foreach (var face in facesElement.EnumerateArray())
                {
                    // Lấy Bounding Box [x, y, w, h]
                    var boxArray = face.GetProperty("box").EnumerateArray().Select(x => x.GetInt32()).ToArray();

                    // Lấy trực tiếp Tên (Mã SV) do AI Python quyết định
                    var name = face.GetProperty("name").GetString();

                    resultList.Add((boxArray, name));
                }
            }

            return resultList;
        }
    }
}