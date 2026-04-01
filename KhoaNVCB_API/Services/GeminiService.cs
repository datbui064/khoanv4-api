using System.Text;
using System.Text.Json;

namespace KhoaNVCB_API.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["GeminiAI:ApiKey"] ?? "";
        }

        public async Task<bool> IsRelevantToSecurityAsync(string title, string summary)
        {
            // Kiểm tra xem đã dán API Key thật chưa
            if (string.IsNullOrEmpty(_apiKey) || _apiKey.Contains("DÁN_CÁI_API_KEY"))
            {
                Console.WriteLine("[Gemini LỖI]: Chưa cấu hình API Key thật!");
                return true; // Tạm cho qua hết nếu chưa có Key để không bị vứt oan
            }

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

            string prompt = $@"
Bạn là một chuyên gia kiểm duyệt nội dung của Khoa An ninh nội địa.
Nhiệm vụ của bạn là đọc Tiêu đề và Tóm tắt dưới đây, sau đó đánh giá xem bài viết này CÓ THUỘC MỘT TRONG CÁC LĨNH VỰC SAU KHÔNG: 
An ninh nội địa, phòng chống tội phạm, an ninh mạng, an ninh kinh tế, tôn giáo, dân tộc, chống khủng bố, phản động, hoặc bảo vệ Tổ quốc.

Tiêu đề: {title}
Tóm tắt: {summary}

CHỈ TRẢ LỜI DUY NHẤT 1 TỪ: 'YES' (nếu có liên quan) hoặc 'NO' (nếu không liên quan).";

            var requestBody = new
            {
                contents = new[] {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // In câu trả lời thật của AI ra để xem nó có "trả treo" không
                    Console.WriteLine($"[Gemini TRẢ LỜI cho bài '{title}']: {responseString}");
                    return responseString.Contains("YES", StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    // Ép nó khai ra lỗi HTTP
                    Console.WriteLine($"[Gemini TỪ CHỐI API]: {response.StatusCode} - Chi tiết: {responseString}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Lỗi Hệ Thống Gemini API]: {ex.Message}");
            }

            return false;
        }
    }
}