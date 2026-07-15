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

        // ========================================================
        // HÀM 1: DÙNG ĐỂ CHAT VỚI NGƯỜI DÙNG BÊN FRONTEND (MỚI THÊM)
        // ========================================================
        public async Task<string> ChatAsync(string userMessage, string context = "")
        {
            if (string.IsNullOrEmpty(_apiKey) || _apiKey.Contains("HIDDEN"))
            {
                return "Xin lỗi, hệ thống AI chưa được cấu hình API Key. Vui lòng liên hệ Admin.";
            }

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

            // BƠM NHÂN CÁCH CHO AI
            string prompt = $@"Bạn là một trợ lý ảo thông minh, lịch sự và chuyên nghiệp của 'Khoa An ninh chính trị nội bộ' (Khoa NV4).
Nhiệm vụ của bạn là giải đáp các thắc mắc của người dùng, hoặc tóm tắt tài liệu.
Quy tắc bắt buộc:
1. Luôn trả lời bằng tiếng Việt.
2. Trình bày đẹp mắt bằng các thẻ HTML cơ bản (dùng <br/> để xuống dòng, <strong> để in đậm).
3. KHÔNG sử dụng Markdown như dấu ** hay *. Chỉ dùng HTML.

{(string.IsNullOrEmpty(context) ? "" : $"Người dùng đang đọc bài viết có nội dung sau:\n---\n{context}\n---\nNếu câu hỏi yêu cầu tóm tắt hoặc liên quan đến bài viết này, hãy dựa vào thông tin trên để trả lời.")}

Câu hỏi của người dùng: {userMessage}";

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
                    using JsonDocument doc = JsonDocument.Parse(responseString);
                    var answer = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text").GetString();

                    return answer ?? "Xin lỗi, tôi không thể tạo câu trả lời lúc này.";
                }

                return $"[Lỗi API]: {response.StatusCode} - Vui lòng kiểm tra lại cấu hình.";
            }
            catch (Exception ex)
            {
                return $"[Hệ thống quá tải]: {ex.Message}";
            }
        }

        // ========================================================
        // HÀM 2: DÙNG ĐỂ KIỂM DUYỆT BÀI CÀO BÊN ADMIN (CŨ CỦA CỤC CỨT)
        // ========================================================
        public async Task<bool> IsRelevantToSecurityAsync(string title, string summary)
        {
            if (string.IsNullOrEmpty(_apiKey) || _apiKey.Contains("HIDDEN"))
            {
                Console.WriteLine("[Gemini LỖI]: Chưa cấu hình API Key thật!");
                return true;
            }

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

            string prompt = $@"
Bạn là một Sĩ quan an ninh, chuyên gia phân tích dữ liệu chính trị nội bộ công an nhân dân.
Nhiệm vụ của bạn là đọc Tiêu đề và Tóm tắt dưới đây, đánh giá xem bài viết/video này CÓ THUỘC MỘT TRONG CÁC LĨNH VỰC TRỌNG ĐIỂM SAU KHÔNG: 
1. Hoạt động của các tổ chức phản động lưu vong, khủng bố (ví dụ: Việt Tân, Chính phủ quốc gia Việt Nam lâm thời, Triều đại Việt, Hội người Thượng vì công lý, BPSOS...).
2. Các vụ án, phóng sự liên quan đến khủng bố, bạo loạn, lật đổ, chống phá Nhà nước.
3. Đấu tranh bảo vệ nền tảng tư tưởng của Đảng, phản bác các quan điểm sai trái, luận điệu xuyên tạc, thù địch.

Tiêu đề: {title}
Tóm tắt: {summary}

Lưu ý: Loại bỏ các tin tức hình sự, đâm chém, cướp giật, tai nạn giao thông, hoặc tin quốc tế không liên quan đến an ninh quốc gia Việt Nam.
CHỈ TRẢ LỜI DUY NHẤT 1 TỪ: 'YES' (nếu đúng trọng tâm trên) hoặc 'NO' (nếu không liên quan hoặc là tin rác).";

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
                    Console.WriteLine($"[Gemini TRẢ LỜI cho bài '{title}']: {responseString}");
                    return responseString.Contains("YES", StringComparison.OrdinalIgnoreCase);
                }
                else
                {
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