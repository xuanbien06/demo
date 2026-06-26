using System.Text.Json;

namespace FaceAttendance.Web.Services
{
    public class FaceRecognitionService
    {
        private readonly HttpClient _httpClient;

        public FaceRecognitionService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Địa chỉ của API Python FastAPI (Bạn có thể điều chỉnh port nếu cần)
            _httpClient.BaseAddress = new Uri("http://127.0.0.1:8000");
        }

        // HÀM MỚI: Nhận diện nhiều khuôn mặt
        public async Task<List<List<float>>> GetMultipleFaceEmbeddingsAsync(IFormFile imageFile)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                using var stream = imageFile.OpenReadStream();
                using var streamContent = new StreamContent(stream);
                content.Add(streamContent, "file", imageFile.FileName);

                var response = await _httpClient.PostAsync("/api/extract-face", content);

                if (!response.IsSuccessStatusCode)
                {
                    return new List<List<float>>(); // Lỗi mạng hoặc Python sập -> trả về rỗng
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(jsonResponse);
                var root = document.RootElement;

                var resultList = new List<List<float>>();

                // Dùng TryGetProperty để tránh bị crash app nếu Python trả về file JSON bị thiếu field
                if (root.TryGetProperty("status", out var statusProp) && statusProp.GetString() == "success")
                {
                    // Lấy field "vectors" (bản mới) thay vì "vector" (bản cũ)
                    if (root.TryGetProperty("vectors", out var vectorsElement))
                    {
                        foreach (var personVector in vectorsElement.EnumerateArray())
                        {
                            var singleVector = new List<float>();
                            foreach (var item in personVector.EnumerateArray())
                            {
                                singleVector.Add((float)item.GetDouble());
                            }
                            resultList.Add(singleVector);
                        }
                    }
                }
                return resultList;
            }
            catch (Exception)
            {
                // Bắt mọi ngoại lệ để Web C# không bao giờ bị sập
                return new List<List<float>>();
            }
        }
    }
}