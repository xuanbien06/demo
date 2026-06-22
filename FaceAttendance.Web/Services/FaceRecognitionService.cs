using System.Text.Json;

namespace FaceAttendance.Web.Services
{
    public class FaceRecognitionService
    {
        private readonly HttpClient _httpClient;

        public FaceRecognitionService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Địa chỉ mặc định của Python FastAPI đang chạy
            _httpClient.BaseAddress = new Uri("http://localhost:8000");
        }

        // Hàm này nhận file ảnh từ giao diện, gửi sang Python, và nhận về Vector
        public async Task<List<float>> GetFaceEmbeddingAsync(IFormFile imageFile)
        {
            // 1. Chuyển file ảnh thành dạng MultipartFormData để gửi qua HTTP Request
            using var content = new MultipartFormDataContent();
            using var stream = imageFile.OpenReadStream();
            using var streamContent = new StreamContent(stream);

            content.Add(streamContent, "file", imageFile.FileName);

            // 2. Bắn HTTP POST Request sang Python
            var response = await _httpClient.PostAsync("/api/extract-face", content);

            if (!response.IsSuccessStatusCode)
            {
                // Nếu Python báo lỗi (không thấy mặt, ảnh hỏng...)
                var errorResult = await response.Content.ReadAsStringAsync();
                throw new Exception($"Lỗi từ AI: {errorResult}");
            }

            // 3. Đọc dữ liệu JSON (Response) Python trả về
            var jsonResponse = await response.Content.ReadAsStringAsync();

            // 4. Giải mã JSON (Parse JSON) để lấy ra mảng 512 con số
            using var document = JsonDocument.Parse(jsonResponse);
            var root = document.RootElement;

            if (root.GetProperty("status").GetString() == "success")
            {
                var vectorElement = root.GetProperty("vector");
                var vectorList = new List<float>();

                // Chuyển mảng JSON thành List<float> của C#
                foreach (var item in vectorElement.EnumerateArray())
                {
                    vectorList.Add((float)item.GetDouble());
                }
                return vectorList;
            }

            return null;
        }
    }
}